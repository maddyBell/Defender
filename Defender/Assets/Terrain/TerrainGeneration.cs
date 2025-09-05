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
        //Security check to ensure grass prefabs are loaded 

        if (!grassPrefab)
        {
            Debug.LogError("Grass Prefab not assigned");
            return;
        }

        //Security check to make sure no pre-existing terrain is present
        if (grassTransform)
        {
            DestroyImmediate(grassTransform.gameObject);
        }
        if (castleTransform)
        {
            DestroyImmediate(castleTransform.gameObject);
        }

        //Setting up the transforms for the grass and castles
        grassTransform = new GameObject("GrassTiles").transform;
        grassTransform.parent = transform;
        castleTransform = new GameObject("CastleTile").transform;
        castleTransform.parent = transform;

        //Making the heightmap

        float[,] heightMap = new float[width + 1, height + 1]; // adding 1 to both for proper tiling
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= height; y++)
            {
                float xCoord = (float)x / width * noiseScale;
                float yCoord = (float)y / height * noiseScale;
                //using Perlin noise to generate the heightmap
                heightMap[x, y] = Mathf.PerlinNoise(xCoord, yCoord) * heightMultiplier;
            }
        }

        //Generating paths 

        bool[,] pathMask = new bool[width + 1, height + 1];

        foreach (var start in PathStarts())
        {
            List<Vector2> path = GeneratePath(start);
            foreach (var point in path)
            {
                MarkPathArea(pathMask, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
            }
        }

        //Spawning in the grass tiles

        defenderAreas = new List<GameObject>();
        openSpaces = new Vector3[(width * height) - (defenderAreas.Count + (castleSize.x * castleSize.y))]; // setting up the open spaces array to be used for enviro decor spawning
        int openSpaceIndex = 0;
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < height; y++)
            {
                if (CastleInterior(x, y))
                {
                    continue; // skipping the castle interior
                }
                if (pathMask[x, y])
                {
                    continue; // skipping the castle interior
                }
                Vector3 position = new Vector3(x, heightMap[x, y], y);
                GameObject grassTile = Instantiate(grassPrefab, position, Quaternion.identity);

                //Checking if the tile is a path or next to a path for defender placement
                if (DefenderArea(x, y, pathMask))
                {
                    defenderAreas.Add(grassTile);
                }
                Debug.Log(openSpaces.Length);
                openSpaces[openSpaceIndex] = position; // adding the position of open space to the array
                openSpaceIndex++; // keeping track of the index to stop overwriting
                
            }
        }

        //Spawning in the castle
        if (castlePrefabs.Length > 0)
        {
            GameObject castlePrefab = castlePrefabs[UnityEngine.Random.Range(0, castlePrefabs.Length)]; // randomly choosing which caslt prefab to spawn
            Vector3 castlePosition = new Vector3(mapCentre.x, heightMap[mapCentre.x, mapCentre.y], mapCentre.y);
            GameObject castle = Instantiate(castlePrefab, castlePosition, Quaternion.identity);

            //stating the castle as an obstacle so enemies cant walk
            var obstacle = castle.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
        }
        else
        {
            Debug.LogError("No Castle Prefabs assigned");
        }

        //Making Path Mesh 

        pathMeshObject = new GameObject("PathMesh");
        pathMeshObject.transform.parent = transform;
        int walkableLayer = LayerMask.NameToLayer("Walkable");
        if (walkableLayer == -1)
        {
             Debug.LogError("Layer 'Walkable' does not exist!");
             walkableLayer = 0; // fallback to Default
        }
        pathMeshObject.layer = walkableLayer;

        MeshFilter meshFilter = pathMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = pathMeshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = pathMaterial;

        //making the mesh based on the mask 

        meshFilter.mesh = PathNav(heightMap, pathMask);

        //making the navmesh on runtime 
        pathNavMesh = pathMeshObject.AddComponent<NavMeshSurface>();
        pathNavMesh.collectObjects = CollectObjects.All;
        pathNavMesh.BuildNavMesh();
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
        int halfWidth = castleSize.x/2;
        int halfHeight = castleSize.y/2;

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


}
