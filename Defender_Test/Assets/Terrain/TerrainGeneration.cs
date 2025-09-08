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

    public int width = 128, length = 128;
    public float noiseScale = 15f, heightMultiplier = 2f;

    //Path Dimensions 
    public int minPath = 4, maxPath = 6;
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

    //Keeping track of Path starts 
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
    public EnemySpawner spawner;

    public int forestInset = 3;

    //exposing the height map for enemies spawn 
    public float[,] HeightMap { get; private set; }


    void Start()
    {
        //Setting up the map centre and generating the terrain
        mapCentre = new Vector2Int(width / 2, length / 2);
        terrainDecoration = new TerrainDecoration(trees, grass, rocks, numberOfTrees, numberOfGrass, numberOfRocks);
        GenerateTerrainMap();
        terrainDecoration.PlaceDecoration(openSpaces);


    }

    public void GenerateTerrainMap()
    {

        if (!grassPrefab)
        {
            Debug.LogError("Grass Prefab not assigned");
            return;
        }

        //making sure no prefabs are still in the map, mainly there to make sure theres nothing left behind when reloading the map
        if (grassTransform) DestroyImmediate(grassTransform.gameObject);
        if (castleTransform) DestroyImmediate(castleTransform.gameObject);

        grassTransform = new GameObject("GrassTiles").transform;
        grassTransform.parent = transform;
        castleTransform = new GameObject("CastleTile").transform;
        castleTransform.parent = transform;

        //using Perlin Noise to procedurally generate the height map to create variance in height for the grass/ terriain tiles 
        float[,] heightMap = new float[width + 1, length + 1];
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= length; y++)
            {
                float xCoord = (float)x / width * noiseScale;
                float yCoord = (float)y / length * noiseScale;
                heightMap[x, y] = Mathf.PerlinNoise(xCoord, yCoord) * heightMultiplier;
            }
        }

        // creating a path mask based on the height map to keep track of the paths that are generated 
        bool[,] pathMask = new bool[width + 1, length + 1];
        var starts = PathStarts();
        pathStartPositions = starts.ToArray();

        foreach (var start in starts)
        {
            List<Vector2> path = GeneratePath(start);
            foreach (var point in path)
            {
                MarkPathArea(pathMask, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y)); // marking the path areas to use for nav mesh stuff 
            }
        }

        // spawning the grass tiles and keeping track of the open spaces and defender areas for terrain decor and defender placement 
        defenderAreas = new List<GameObject>();
        openSpaces = new Vector3[(width * length) - (defenderAreas.Count + (castleSize.x * castleSize.y))];
        int openSpaceIndex = 0;
        int grassLayer = LayerMask.NameToLayer("NonWalkable"); // marking the grass area as non walkable so the nav mesh bloacks it off from walkable area
        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                if (CastleInterior(x, y) || pathMask[x, y]) continue; // skipping over the path areas and the castle interior space to avoid clipping and overlapping issues 

                Vector3 position = new Vector3(x, heightMap[x, y], y);
                GameObject grassTile = Instantiate(grassPrefab, position, Quaternion.identity, grassTransform);
                grassTile.layer = grassLayer;

                // using a nav mesh modifier to directly ignore the grass areas from the surface build 
                var modifier = grassTile.GetComponent<NavMeshModifier>();
                if (modifier == null) modifier = grassTile.AddComponent<NavMeshModifier>();
                modifier.ignoreFromBuild = true;

                // creating the defender areas, using the heightmap and pathmask and adding them to the defender area list and applying a "defender" layer 
                if (DefenderArea(x, y, pathMask))
                {
                    defenderAreas.Add(grassTile);
                    grassTile.layer = LayerMask.NameToLayer("Defender");
                }

                // creating the open spaces so i can add the decorations in theses areas 
                openSpaces[openSpaceIndex] = position;
                openSpaceIndex++;
            }
        }

        // Spawning the tower in the centre of the map, added randomness for the castles so they different, making them a nav mesh obstacle
        if (castlePrefabs.Length > 0)
        {
            GameObject castlePrefab = castlePrefabs[UnityEngine.Random.Range(0, castlePrefabs.Length)];
            Vector3 castlePosition = new Vector3(mapCentre.x, heightMap[mapCentre.x, mapCentre.y], mapCentre.y);
            GameObject castle = Instantiate(castlePrefab, castlePosition, Quaternion.identity, castleTransform);
            var obstacle = castle.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;
        }
        else Debug.LogError("No Castle Prefabs assigned");

        // Creating the mesh for the path becasue it doesnt use a tile prefab, placing it into the world and assinging it to the Path layer 
        pathMeshObject = new GameObject("PathMesh");
        pathMeshObject.transform.parent = transform;
        pathMeshObject.layer = LayerMask.NameToLayer("Path"); // make sure this layer exists
        MeshFilter meshFilter = pathMeshObject.AddComponent<MeshFilter>();
        MeshRenderer meshRenderer = pathMeshObject.AddComponent<MeshRenderer>();
        meshRenderer.material = pathMaterial;

        meshFilter.mesh = PathNav(heightMap, pathMask);

        // setting up the nav mesh surface to bake on runtime so the paths are the onlhy ones baked and can be used for the enemy movement 
        pathNavMesh = pathMeshObject.AddComponent<NavMeshSurface>();
        pathNavMesh.collectObjects = CollectObjects.Children; // only using the path mesh objects 
        pathNavMesh.useGeometry = NavMeshCollectGeometry.RenderMeshes;
        pathNavMesh.layerMask = 1 << LayerMask.NameToLayer("Path"); //double checking that the area is marked as a path layer 
        pathNavMesh.BuildNavMesh(); // building and baking the nav mesh 



        // Spawn forest
        terrainDecoration.SpawnBorderForest(EdgePositions(heightMap, forestInset), trees, forestInset, 0.9f, 0.4f);

        HeightMap = heightMap; // exposing the height map 
               if (spawner != null)
{
    spawner.SetupSpawner(this);
    spawner.StartSpawning();
    Debug.Log("Spawning enemies");
}

    }

    // creating the areas that the paths will start from, randomising the number of paths and what side of the map the paths are spawned on 
    private List<Vector2> PathStarts()
    {
        int numPaths = UnityEngine.Random.Range(minPath, maxPath + 1);
        List<Vector2> starts = new List<Vector2>();

        //Setting up a dictionary to store the sides of the map
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
            //making sure that not more than two of the paths start on the same side of the map (visual appeal)
            do
            {
                int sideIndex = UnityEngine.Random.Range(0, 4);
                side = sideIndex == 0 ? "Top" :
                sideIndex == 1 ? "Bottom" :
                sideIndex == 2 ? "Left" : "Right";

            } while (sideCounts[side] >= 2);

            sideCounts[side]++;

            Vector2 position = Vector2.zero;

            //switch is used to randomise with the correct values cos of the changes made when generating the height maps 
            switch (side)
            {
                case "Top":
                    position = new Vector2(UnityEngine.Random.Range(0, width), length - 1);
                    break;
                case "Bottom":
                    position = new Vector2(UnityEngine.Random.Range(0, width), 0);
                    break;
                case "Left":
                    position = new Vector2(0, UnityEngine.Random.Range(0, length));
                    break;
                case "Right":
                    position = new Vector2(width - 1, UnityEngine.Random.Range(0, length));
                    break;
            }

            starts.Add(position); // adding the position to the list 
        }
        return starts;
    }

    //generating the paths using a random walk system including as much random data to generate as possible to try improve the uniqueness of each map generation 
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

//marking all the areas that are paths so i can detect what areas are and are not for decoration, defender and nav functions
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
                if (newX >= 0 && newX < width && newY >= 0 && newY < length)
                {
                    mask[newX, newY] = true;
                }
            }
        }
    }

//getting the coordinates of the center point of the castle 
    private bool CastleInterior(int x, int y)
    {
        // getting the coords that fall inside the castle space to help with PAthing
        int halfWidth = castleSize.x / 2;
        int halfHeight = castleSize.y / 2;

        return (x >= mapCentre.x - halfWidth && x < mapCentre.x + halfWidth &&
                y >= mapCentre.y - halfHeight && y < mapCentre.y + halfHeight);
    }

//creating the defender areas and making sure that is is adjacent to the path by all sides including the diagonals 
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

            if (newX >= 0 && newX < width && newY >= 0 && newY < length)
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
            for (int y = 0; y < length; y++)
            {
                if (!pathMask[x, y] || CastleInterior(x, y))
                {
                    continue; // skipping grass and castl areas 
                }
                    // creating the 4 corner set up so that the path fits the tile system set up for the grass 
                Vector3 quad0 = new Vector3(x, heightMap[x, y], y);
                Vector3 quad1 = new Vector3(x + 1, heightMap[x + 1, y], y);
                Vector3 quad2 = new Vector3(x, heightMap[x, y + 1], y + 1);
                Vector3 quad3 = new Vector3(x + 1, heightMap[x + 1, y + 1], y + 1);

                vertices.AddRange(new Vector3[] { quad0, quad1, quad2, quad3 });
                uvs.AddRange(new Vector2[]
                {
                    new Vector2((float)x / width, (float)y / length),
                        new Vector2((float)(x + 1) / width, (float)y / length),
                    new Vector2((float)x / width, (float)(y + 1) / length),
                        new Vector2((float)(x + 1) / width, (float)(y + 1) / length)
                });

                triangles.AddRange(new int[]
                {
                    vert, vert + 2, vert + 1,
                    vert + 2, vert + 3, vert + 1
                });

                vert += 4;

            }
        }
            //generating the physical mesh spawned into the world using the calculated tris and quads 
        Mesh pMesh = new Mesh();
        pMesh.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32; // letting the mesh have lots of verts
        pMesh.SetVertices(vertices);
        pMesh.SetTriangles(triangles, 0);
        pMesh.SetUVs(0, uvs);
        pMesh.RecalculateNormals();

        return pMesh;
    }

//getting all the edge positions to spawn the forest set up on the borders of the map 
    private List<Vector3> EdgePositions(float[,] heightMap, int inset)
    {
        List<Vector3> edges = new List<Vector3>();

        // Bottom and Top rows with inset
        for (int x = inset; x < width - inset; x++)
        {
            edges.Add(new Vector3(x, heightMap[x, inset], inset)); //getting bottom
            edges.Add(new Vector3(x, heightMap[x, length - 1 - inset], length - 1 - inset)); //getting top
        }

        // Left and Right columns with inset 
        for (int y = inset + 1; y < length - 1 - inset; y++)
        {
            edges.Add(new Vector3(inset, heightMap[inset, y], y));//getting left
            edges.Add(new Vector3(width - 1 - inset, heightMap[width - 1 - inset, y], y)); //getting right
        }
        //adding insets to pull it in a bit more and flesh the forest out a bit so its not a single lined wall
        return edges;
    }

    //converting the original path starts to vector 3 so they can be used directly to spawn the enemies 
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

//converting the castle position to a vector 3 so it can be used for the enemies target seamlessly
    public Vector3 GetCastleWorldPosition()
    {
        return new Vector3(mapCentre.x, HeightMap[mapCentre.x, mapCentre.y], mapCentre.y);
    }

}
