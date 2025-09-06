using System;
using System.Collections;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using Unity.AI.Navigation;
using UnityEngine;
using UnityEngine.AI;

public class TerrainGeneration : MonoBehaviour
{
    //Terrain dimensions

    public int width = 128, height = 128;
    public float noiseScale = 15f, heightMultiplier = 2f;

    //Path Dimensions 
    public int minPath = 3, maxPath = 6;
    public float pathWidth = 2f;
    private GameObject pathMeshObject;
    private NavMeshSurface pathNavMesh;

    public Material pathMaterial;

    //Grass and Castle Prefabs 
    public GameObject grassPrefab;
    public GameObject[] castlePrefabs;

    //Castle sizing 
    public Vector2Int castleSize = new Vector2Int(3, 3);

    //Defender Placement Areas
    public List<GameObject> defenderAreas { get; private set; }

    //Keeping Path starts 

    public Vector2[] pathStartPositions { get; private set; }

    //Getting the transforms of the grass and castles
    private Transform grassTransform, castleTransform;

    //Getting the position of the map centre for castle spawns
    private Vector2Int mapCentre;

    //Open spaces sed to spawn evironmental decor
    private Vector3[] openSpaces;
    public GameObject[] trees, grass, rocks;
    public int numberOfTrees, numberOfGrass, numberOfRocks;
    private TerrainDecoration terrainDecoration;

    public int forestInset = 3;

    //exposing the height map for enemies spawn 
    public float[,] HeightMap { get; private set; }




    void Start()
    {
        //Setting up the map centre and generating the terrain
        mapCentre = new Vector2Int(width / 2, height / 2);
        terrainDecoration = new TerrainDecoration(trees, grass, rocks, numberOfTrees, numberOfGrass, numberOfRocks);
        GenerateTerrainMap();
        terrainDecoration.PlaceDecoration(openSpaces);


    }

    public void GenerateTerrainMap()
{
    // Security checks
    if (!grassPrefab)
    {
        Debug.LogError("Grass Prefab not assigned");
        return;
    }

    if (grassTransform) DestroyImmediate(grassTransform.gameObject);
    if (castleTransform) DestroyImmediate(castleTransform.gameObject);

    grassTransform = new GameObject("GrassTiles").transform;
    grassTransform.parent = transform;
    castleTransform = new GameObject("CastleTile").transform;
    castleTransform.parent = transform;

    float[,] heightMap = new float[width + 1, height + 1];
    for (int x = 0; x <= width; x++)
    {
        for (int y = 0; y <= height; y++)
        {
            float xCoord = (float)x / width * noiseScale;
            float yCoord = (float)y / height * noiseScale;
            heightMap[x, y] = Mathf.PerlinNoise(xCoord, yCoord) * heightMultiplier;
        }
    }

    // Generate paths
    bool[,] pathMask = new bool[width + 1, height + 1];
    var starts = PathStarts();
    pathStartPositions = starts.ToArray();

    foreach (var start in starts)
    {
        List<Vector2> path = GeneratePath(start);
        foreach (var point in path)
        {
            MarkPathArea(pathMask, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
        }
    }

    // Spawn grass tiles (non-walkable)
    defenderAreas = new List<GameObject>();
    openSpaces = new Vector3[(width * height) - (defenderAreas.Count + (castleSize.x * castleSize.y))];
    int openSpaceIndex = 0;
    int grassLayer = LayerMask.NameToLayer("NonWalkable"); // make sure this layer exists
    for (int x = 0; x < width; x++)
    {
        for (int y = 0; y < height; y++)
        {
            if (CastleInterior(x, y) || pathMask[x, y]) continue;

            Vector3 position = new Vector3(x, heightMap[x, y], y);
            GameObject grassTile = Instantiate(grassPrefab, position, Quaternion.identity, grassTransform);
            grassTile.layer = grassLayer;

            // Add NavMeshModifier to ignore this object
            var modifier = grassTile.GetComponent<NavMeshModifier>();
            if (modifier == null) modifier = grassTile.AddComponent<NavMeshModifier>();
            modifier.ignoreFromBuild = true;

            if (DefenderArea(x, y, pathMask))
                defenderAreas.Add(grassTile);

            openSpaces[openSpaceIndex] = position;
            openSpaceIndex++;
        }
    }

    // Spawn castle
    if (castlePrefabs.Length > 0)
    {
        GameObject castlePrefab = castlePrefabs[UnityEngine.Random.Range(0, castlePrefabs.Length)];
        Vector3 castlePosition = new Vector3(mapCentre.x, heightMap[mapCentre.x, mapCentre.y], mapCentre.y);
        GameObject castle = Instantiate(castlePrefab, castlePosition, Quaternion.identity, castleTransform);
        var obstacle = castle.AddComponent<NavMeshObstacle>();
        obstacle.carving = true;
    }
    else Debug.LogError("No Castle Prefabs assigned");

    // Create path mesh
    pathMeshObject = new GameObject("PathMesh");
    pathMeshObject.transform.parent = transform;
    pathMeshObject.layer = LayerMask.NameToLayer("Path"); // make sure this layer exists

    MeshFilter meshFilter = pathMeshObject.AddComponent<MeshFilter>();
    MeshRenderer meshRenderer = pathMeshObject.AddComponent<MeshRenderer>();
    meshRenderer.material = pathMaterial;

    meshFilter.mesh = PathNav(heightMap, pathMask);

    // NavMeshSurface setup (only bake path)
    pathNavMesh = pathMeshObject.AddComponent<NavMeshSurface>();
    pathNavMesh.collectObjects = CollectObjects.Children; // only pathMeshObject
    pathNavMesh.useGeometry = NavMeshCollectGeometry.RenderMeshes;
    pathNavMesh.layerMask = 1 << LayerMask.NameToLayer("Path"); // bake only Path layer
    pathNavMesh.BuildNavMesh();

    // Spawn forest
    terrainDecoration.SpawnBorderForest(EdgePositions(heightMap, forestInset), trees, forestInset, 0.9f, 0.4f);

    HeightMap = heightMap;
}
    private List<Vector2> PathStarts()
    {
        int numPaths = UnityEngine.Random.Range(minPath, maxPath + 1);
        List<Vector2> starts = new List<Vector2>();

        //Setting up a dictionary to keep track of how many paths start on each side to enforce placement rules
        Dictionary<string, int> sideCounts = new Dictionary<string, int>
        {
            { "Top", 0 },
            { "Bottom", 0 },
            { "Left", 0 },
            { "Right", 0 }
        };

        for (int i = 0; i < numPaths; i++)
        {
            string side;


            //Ensuring that no more than teo paths start on the same side
            do
            {
                int sideIndex = UnityEngine.Random.Range(0, 4);
                side = sideIndex == 0 ? "Top" :
                sideIndex == 1 ? "Bottom" :
                sideIndex == 2 ? "Left" : "Right";

            } while (sideCounts[side] >= 2);

            //Incrementing the count for the chosen side
            sideCounts[side]++;

            Vector2 position = Vector2.zero;

            //using a switch to generate random paths starts within rules, Top and Bottom contain minuses due to increment of width and height at the stat

            switch (side)
            {
                case "Top":
                    position = new Vector2(UnityEngine.Random.Range(0, width), height - 1);
                    break;
                case "Bottom":
                    position = new Vector2(UnityEngine.Random.Range(0, width), 0);
                    break;
                case "Left":
                    position = new Vector2(0, UnityEngine.Random.Range(0, height));
                    break;
                case "Right":
                    position = new Vector2(width - 1, UnityEngine.Random.Range(0, height));
                    break;
            }

            starts.Add(position);
        }
        return starts;
    }

    private List<Vector2> GeneratePath(Vector2 start)
    {
        List<Vector2> path = new List<Vector2>();

        Vector2 currentPos = start;
        int safetyCounter = 0; // an int used to try prevent broken loops

        while (Vector2.Distance(currentPos, mapCentre) > pathWidth && safetyCounter < 1000)
        {
            path.Add(currentPos);
            Vector2 directionToCentre = (mapCentre - currentPos).normalized;
            Vector2 randomStep = directionToCentre + new Vector2(UnityEngine.Random.Range(-0.25f, 0.25f), UnityEngine.Random.Range(-0.25f, 0.25f)); // creating a random step to improve the change in path heights
            currentPos += randomStep.normalized * pathWidth; // updating the current position to accomodate the changes and create seemless joints

            safetyCounter++;
        }
        return path;
    }

    private void MarkPathArea(bool[,] mask, int x, int y)
    {
        int halfWidth = Mathf.CeilToInt(pathWidth / 2f);

        //making the square around the point as a path 
        for (int i = -halfWidth; i <= halfWidth; i++)
        {
            for (int j = -halfWidth; j <= halfWidth; j++)
            {
                int newX = x + i;
                int newY = y + j;
                if (newX >= 0 && newX < width && newY >= 0 && newY < height)
                {
                    mask[newX, newY] = true;
                }
            }
        }
    }

    private bool CastleInterior(int x, int y)
    {
        // getting the coords that fall inside the castle space to help with PAthing
        int halfWidth = castleSize.x / 2;
        int halfHeight = castleSize.y / 2;

        return (x >= mapCentre.x - halfWidth && x < mapCentre.x + halfWidth &&
                y >= mapCentre.y - halfHeight && y < mapCentre.y + halfHeight);
    }

    bool DefenderArea(int x, int y, bool[,] pathMask)
    {
        //checking if next to a path from all sides and diags

        int[,] directions = new int[,] //Manual settings for 8 cards
        {
            {1, 0}, {-1, 0}, {0, 1}, {0, -1},
            {1, 1}, {1, -1}, {-1, 1}, {-1, -1}
        };

        for (int i = 0; i < 7; i++)
        {
            int newX = x + directions[i, 0];
            int newY = y + directions[i, 1];

            if (newX >= 0 && newX < width && newY >= 0 && newY < height)
            {
                if (pathMask[newX, newY])
                {
                    return true;
                }
            }
        }

        return false;

    }

    private Mesh PathNav(float[,] heightMap, bool[,] pathMask)
    {
        //Setting up the mesh data
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();
        List<Vector2> uvs = new List<Vector2>();

        int vert = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (!pathMask[x, y] || CastleInterior(x, y))
                {
                    continue; // skipping non-path areas and castle interior
                }

                Vector3 quad0 = new Vector3(x, heightMap[x, y], y);
                Vector3 quad1 = new Vector3(x + 1, heightMap[x + 1, y], y);
                Vector3 quad2 = new Vector3(x, heightMap[x, y + 1], y + 1);
                Vector3 quad3 = new Vector3(x + 1, heightMap[x + 1, y + 1], y + 1);

                vertices.AddRange(new Vector3[] { quad0, quad1, quad2, quad3 });
                uvs.AddRange(new Vector2[]
                {
                    new Vector2((float)x / width, (float)y / height),
                        new Vector2((float)(x + 1) / width, (float)y / height),
                    new Vector2((float)x / width, (float)(y + 1) / height),
                        new Vector2((float)(x + 1) / width, (float)(y + 1) / height)
                });

                triangles.AddRange(new int[]
                {
                    vert, vert + 2, vert + 1,
                    vert + 2, vert + 3, vert + 1
                });

                vert += 4;

            }
        }

        Mesh pMesh = new Mesh();
        pMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // letting the mesh have lots of verts
        pMesh.SetVertices(vertices);
        pMesh.SetTriangles(triangles, 0);
        pMesh.SetUVs(0, uvs);
        pMesh.RecalculateNormals();

        return pMesh;
    }

    private List<Vector3> EdgePositions(float[,] heightMap, int inset)
    {
        List<Vector3> edges = new List<Vector3>();

        // Bottom and Top rows with inset
        for (int x = inset; x < width - inset; x++)
        {
            edges.Add(new Vector3(x, heightMap[x, inset], inset)); //getting bottom
            edges.Add(new Vector3(x, heightMap[x, height - 1 - inset], height - 1 - inset)); //getting top
        }

        // Left and Right columns with inset 
        for (int y = inset + 1; y < height - 1 - inset; y++)
        {
            edges.Add(new Vector3(inset, heightMap[inset, y], y));//getting left
            edges.Add(new Vector3(width - 1 - inset, heightMap[width - 1 - inset, y], y)); //getting right
        }
        //adding insets to pull it in a bit more and flesh the forest out a bit so its not a single lined wall
        return edges;
    }
    public List<Vector3> GetPathStartWorldPositions(float[,] heightMap)
    {
        List<Vector3> worldPositions = new List<Vector3>();

        foreach (var pos in pathStartPositions)
        {
            int x = Mathf.RoundToInt(pos.x);
            int y = Mathf.RoundToInt(pos.y);

            worldPositions.Add(new Vector3(x, heightMap[x, y], y));
        }

        return worldPositions;
    }

    public Vector3 GetCastleWorldPosition()
    {
        return new Vector3(mapCentre.x, HeightMap[mapCentre.x, mapCentre.y], mapCentre.y);
    }

}
