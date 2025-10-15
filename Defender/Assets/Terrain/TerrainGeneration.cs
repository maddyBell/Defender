using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using Unity.AI.Navigation;

public class TerrainGeneration : MonoBehaviour
{
 
    public int width = 90;
    public int length = 64;
    public float noiseScale = 15f;
    public float heightMultiplier = 2f;


    public int minPath = 4;
    public int maxPath = 6;
    public float pathWidth = 2f;
    public Material pathMaterial;
    public int forestInset = 3;

  
    public GameObject grassPrefab;
    public GameObject[] castlePrefabs;
    public Vector2Int castleSize = new Vector2Int(3, 3);

  
    public GameObject[] trees;
    public GameObject[] grass;
    public GameObject[] rocks;
    public int numberOfTrees = 50;
    public int numberOfGrass = 100;
    public int numberOfRocks = 30;

   
    public List<GameObject> defenderAreas { get; private set; }
    public List<Vector3> PathStartWorldPositions { get; private set; }
    public Vector3 CastleWorldPosition { get; private set; }
    public float[,] HeightMap { get; private set; }
    public Vector3[] openSpaces { get; private set; }

    private Transform grassTransform;
    private Transform castleTransform;
    private TerrainDecoration terrainDecoration; 
    private Vector2Int mapCentre;
    private GameObject pathMeshObject;
    private NavMeshSurface pathNavMesh;

    void Start()
    {
        mapCentre = new Vector2Int(width / 2, length / 2);
        terrainDecoration = new TerrainDecoration(trees, grass, rocks, numberOfTrees, numberOfGrass, numberOfRocks);

        GenerateTerrainMap();

        terrainDecoration?.PlaceDecoration(openSpaces);
    }

    public void GenerateTerrainMap()
    {
        // --- basic checks ---
        if (grassPrefab == null)
        {
            Debug.LogError("TerrainGeneration: grassPrefab not assigned.");
            return;
        }

        // clear previous
        if (grassTransform) DestroyImmediate(grassTransform.gameObject);
        if (castleTransform) DestroyImmediate(castleTransform.gameObject);
        if (pathMeshObject) DestroyImmediate(pathMeshObject);

        grassTransform = new GameObject("GrassTiles").transform;
        grassTransform.parent = transform;

        castleTransform = new GameObject("CastleTile").transform;
        castleTransform.parent = transform;

        // --- height map ---
        float[,] heightMap = new float[width + 1, length + 1];
        for (int x = 0; x <= width; x++)
        {
            for (int y = 0; y <= length; y++)
            {
                float xCoord = (float)x / Mathf.Max(1, width) * noiseScale;
                float yCoord = (float)y / Mathf.Max(1, length) * noiseScale;
                heightMap[x, y] = Mathf.PerlinNoise(xCoord, yCoord) * heightMultiplier;
            }
        }

        // --- path generation ---
        bool[,] pathMask = new bool[width + 1, length + 1];
        List<Vector2> starts = PathStarts();

        List<Vector2> pathStartPositions = new List<Vector2>(starts);

        foreach (var start in starts)
        {
            List<Vector2> path = GeneratePath(start);
            foreach (var point in path)
            {
                MarkPathArea(pathMask, Mathf.RoundToInt(point.x), Mathf.RoundToInt(point.y));
            }
        }


        defenderAreas = new List<GameObject>();
        List<Vector3> tempOpenSpaces = new List<Vector3>();

        int grassLayer = LayerMask.NameToLayer("NonWalkable");
        int defenderLayer = LayerMask.NameToLayer("Defender");
  

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {

                if (CastleInterior(x, y) || pathMask[x, y]) continue;

                Vector3 pos = new Vector3(x, heightMap[x, y], y);
                GameObject grassTile = Instantiate(grassPrefab, pos, Quaternion.identity, grassTransform);

                if (grassLayer >= 0) grassTile.layer = grassLayer;


                if (grassTile.GetComponent<Collider>() == null)
                {
                    BoxCollider bc = grassTile.AddComponent<BoxCollider>();
   
                    bc.size = new Vector3(1f, 0.2f, 1f);
                    bc.center = Vector3.zero;
                }


                NavMeshModifier modifier = grassTile.GetComponent<NavMeshModifier>();
                if (modifier == null)
                    modifier = grassTile.AddComponent<NavMeshModifier>();
                modifier.ignoreFromBuild = true;


                if (DefenderArea(x, y, pathMask))
                {
                    defenderAreas.Add(grassTile);
                    if (defenderLayer >= 0) grassTile.layer = defenderLayer;
                }
                else
                {
                    tempOpenSpaces.Add(pos);
                }
            }
        }


        openSpaces = tempOpenSpaces.ToArray();


        if (castlePrefabs != null && castlePrefabs.Length > 0)
        {
            GameObject chosen = castlePrefabs[Random.Range(0, castlePrefabs.Length)];
            Vector3 castlePos = new Vector3(mapCentre.x, heightMap[mapCentre.x, mapCentre.y], mapCentre.y);
            GameObject castle = Instantiate(chosen, castlePos, Quaternion.identity, castleTransform);

            NavMeshObstacle obstacle = castle.GetComponent<NavMeshObstacle>();
            if (obstacle == null) obstacle = castle.AddComponent<NavMeshObstacle>();
            obstacle.carving = true;

            CastleWorldPosition = castlePos;
        }
        else
        {
            Debug.LogError("TerrainGeneration: No castlePrefabs assigned.");
            CastleWorldPosition = new Vector3(mapCentre.x, heightMap[mapCentre.x, mapCentre.y], mapCentre.y);
        }

        // --- build path mesh (rendered) for NavMesh bake ---
        pathMeshObject = new GameObject("PathMesh");
        pathMeshObject.transform.parent = transform;
        int pathLayer = LayerMask.NameToLayer("Path");
        if (pathLayer >= 0) pathMeshObject.layer = pathLayer;

        MeshFilter mf = pathMeshObject.AddComponent<MeshFilter>();
        MeshRenderer mr = pathMeshObject.AddComponent<MeshRenderer>();
        if (pathMaterial != null) mr.material = pathMaterial;
        mf.mesh = BuildPathMesh(heightMap, pathMask);

    
        pathNavMesh = pathMeshObject.GetComponent<NavMeshSurface>();
        if (pathNavMesh == null) pathNavMesh = pathMeshObject.AddComponent<NavMeshSurface>();

        pathNavMesh.collectObjects = CollectObjects.Children;
        pathNavMesh.useGeometry = NavMeshCollectGeometry.RenderMeshes;

        if (pathLayer >= 0)
            pathNavMesh.layerMask = 1 << pathLayer;
        else
            pathNavMesh.layerMask = ~0; 

     
        pathNavMesh.BuildNavMesh();

     
PathStartWorldPositions = new List<Vector3>();
foreach (var p in pathStartPositions)
{
    int sx = Mathf.Clamp(Mathf.RoundToInt(p.x), 0, width);
    int sy = Mathf.Clamp(Mathf.RoundToInt(p.y), 0, length);
    float h = heightMap[sx, sy];
    Vector3 worldPos = new Vector3(sx, h, sy);

    Vector3 dirToCenter = (CastleWorldPosition - worldPos).normalized;
    float spawnOffset = 3.5f; 
    worldPos += dirToCenter * spawnOffset;

    NavMeshHit hit;
    if (NavMesh.SamplePosition(worldPos, out hit, 3f, NavMesh.AllAreas))
        PathStartWorldPositions.Add(hit.position);
    else
        PathStartWorldPositions.Add(worldPos);
}


        HeightMap = heightMap;
         terrainDecoration.SpawnBorderForest(HeightMap, trees, 3, 0.9f, 0.4f);
        
    }



    private List<Vector2> PathStarts()
    {
        int numPaths = Random.Range(minPath, maxPath + 1);
        List<Vector2> starts = new List<Vector2>();
        Dictionary<string, int> sideCounts = new Dictionary<string, int>()
        {
            { "Top", 0 }, { "Bottom", 0 }, { "Left", 0 }, { "Right", 0 }
        };

        for (int i = 0; i < numPaths; i++)
        {
            string side;
            do
            {
                int idx = Random.Range(0, 4);
                side = idx == 0 ? "Top" : idx == 1 ? "Bottom" : idx == 2 ? "Left" : "Right";
            } while (sideCounts[side] >= 2);

            sideCounts[side]++;

            Vector2 pos = Vector2.zero;
            switch (side)
            {
                case "Top": pos = new Vector2(Random.Range(0, width), length - 1); break;
                case "Bottom": pos = new Vector2(Random.Range(0, width), 0); break;
                case "Left": pos = new Vector2(0, Random.Range(0, length)); break;
                case "Right": pos = new Vector2(width - 1, Random.Range(0, length)); break;
            }
            starts.Add(pos);
        }

        return starts;
    }

    private List<Vector2> GeneratePath(Vector2 start)
    {
        List<Vector2> path = new List<Vector2>();
        Vector2 currentPos = start;
        int safety = 0;
        while (Vector2.Distance(currentPos, mapCentre) > pathWidth && safety < 1000)
        {
            path.Add(currentPos);
            Vector2 dir = (mapCentre - currentPos).normalized;
            Vector2 step = dir + new Vector2(Random.Range(-0.25f, 0.25f), Random.Range(-0.25f, 0.25f));
            currentPos += step.normalized * pathWidth;
            safety++;
        }
        return path;
    }

    private void MarkPathArea(bool[,] mask, int x, int y)
    {
        int half = Mathf.CeilToInt(pathWidth / 2f);
        for (int i = -half; i <= half; i++)
        {
            for (int j = -half; j <= half; j++)
            {
                int nx = x + i;
                int ny = y + j;
                if (nx >= 0 && nx < width && ny >= 0 && ny < length)
                    mask[nx, ny] = true;
            }
        }
    }

    private Mesh BuildPathMesh(float[,] heightMap, bool[,] pathMask)
    {
        List<Vector3> verts = new List<Vector3>();
        List<int> tris = new List<int>();
        List<Vector2> uvs = new List<Vector2>();
        int vert = 0;

        for (int x = 0; x < width; x++)
        {
            for (int y = 0; y < length; y++)
            {
                if (!pathMask[x, y] || CastleInterior(x, y)) continue;

                Vector3 q0 = new Vector3(x, heightMap[x, y], y);
                Vector3 q1 = new Vector3(x + 1, heightMap[x + 1, y], y);
                Vector3 q2 = new Vector3(x, heightMap[x, y + 1], y + 1);
                Vector3 q3 = new Vector3(x + 1, heightMap[x + 1, y + 1], y + 1);

                verts.Add(q0); verts.Add(q1); verts.Add(q2); verts.Add(q3);

                uvs.Add(new Vector2((float)x / width, (float)y / length));
                uvs.Add(new Vector2((float)(x + 1) / width, (float)y / length));
                uvs.Add(new Vector2((float)x / width, (float)(y + 1) / length));
                uvs.Add(new Vector2((float)(x + 1) / width, (float)(y + 1) / length));

                tris.Add(vert); tris.Add(vert + 2); tris.Add(vert + 1);
                tris.Add(vert + 2); tris.Add(vert + 3); tris.Add(vert + 1);

                vert += 4;
            }
        }

        Mesh m = new Mesh();
        m.indexFormat = UnityEngine.Rendering.IndexFormat.UInt32;
        m.SetVertices(verts);
        m.SetTriangles(tris, 0);
        m.SetUVs(0, uvs);
        m.RecalculateNormals();
        return m;
    }

    private bool CastleInterior(int x, int y)
    {
        int halfX = castleSize.x / 2;
        int halfY = castleSize.y / 2;
        return x >= mapCentre.x - halfX && x < mapCentre.x + halfX &&
               y >= mapCentre.y - halfY && y < mapCentre.y + halfY;
    }

    private bool DefenderArea(int x, int y, bool[,] pathMask)
    {
        int[,] dirs = new int[,] { { 1, 0 }, { -1, 0 }, { 0, 1 }, { 0, -1 }, { 1, 1 }, { 1, -1 }, { -1, 1 }, { -1, -1 } };
        for (int i = 0; i < 8; i++)
        {
            int nx = x + dirs[i, 0];
            int ny = y + dirs[i, 1];
            if (nx >= 0 && nx < width && ny >= 0 && ny < length)
            {
                if (pathMask[nx, ny]) return true;
            }
        }
        return false;
    }

    // edge positions for decoration system
    public List<Vector3> EdgePositions(float[,] heightMap, int inset)
    {
        List<Vector3> edges = new List<Vector3>();
        for (int x = inset; x < width - inset; x++)
        {
            edges.Add(new Vector3(x, heightMap[x, inset], inset));
            edges.Add(new Vector3(x, heightMap[x, length - 1 - inset], length - 1 - inset));
        }
        for (int y = inset + 1; y < length - 1 - inset; y++)
        {
            edges.Add(new Vector3(inset, heightMap[inset, y], y));
            edges.Add(new Vector3(width - 1 - inset, heightMap[width - 1 - inset, y], y));
        }
        return edges;
    }
    



}