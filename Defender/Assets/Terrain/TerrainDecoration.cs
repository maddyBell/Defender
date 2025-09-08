using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDecoration
{
    public GameObject[] trees, grass, rocks;
    private Vector3[] possiblePositions, filledPositions;

    public int numberOfTrees, numberOfGrass, numberOfRocks;

    public TerrainDecoration(GameObject[] trees, GameObject[] grass, GameObject[] rocks, int numberOfTrees, int numberOfGrass, int numberOfRocks)
    { // getting all the tree, grass and rock prefabs, and the numbers for each 
        this.trees = trees;
        this.grass = grass;
        this.rocks = rocks;
        this.numberOfTrees = numberOfTrees;
        this.numberOfGrass = numberOfGrass;
        this.numberOfRocks = numberOfRocks;
    }

    //placing different decor at random open spots around the map
    public void PlaceDecoration(Vector3[] positions)
    {
        possiblePositions = positions;
        filledPositions = new Vector3[numberOfTrees + numberOfGrass + numberOfRocks];
        int decorPlaced = 0;

        //spawning the number of tress wanted in random positions in the map, uses possible positions to avoid spawning on the tower, defender areas or paths 
        for (int i = 0; i < numberOfTrees; i++)
        {
            int randomIndex = Random.Range(0, possiblePositions.Length);
            int randomTree = Random.Range(0, trees.Length);
            GameObject.Instantiate(trees[randomTree], possiblePositions[randomIndex], Quaternion.Euler(0, Random.Range(0, 360), 0));
            filledPositions[decorPlaced] = possiblePositions[randomIndex];
            decorPlaced++;
        } 
        //same
        for (int i = 0; i < numberOfGrass; i++)
        {
            int randomIndex = Random.Range(0, possiblePositions.Length);
            int randomGrass = Random.Range(0, grass.Length);
            GameObject.Instantiate(grass[randomGrass], possiblePositions[randomIndex], Quaternion.Euler(0, Random.Range(0, 360), 0));
            filledPositions[decorPlaced] = possiblePositions[randomIndex];
            decorPlaced++;
        }
        //same
        for (int i = 0; i < numberOfRocks; i++)
        {
            int randomIndex = Random.Range(0, possiblePositions.Length);
            int randomRock = Random.Range(0, rocks.Length);
            GameObject.Instantiate(rocks[randomRock], possiblePositions[randomIndex], Quaternion.Euler(0, Random.Range(0, 360), 0));
            filledPositions[decorPlaced] = possiblePositions[randomIndex];
            decorPlaced++;
        }
    }

//spawning a border around the edge of the map to hide the edge of the map and the enemy spawn positions, no functional need but trying to improve user friendliness and visual impact 
public void SpawnBorderForest(float[,] heightMap, GameObject[] treePrefabs, int layers = 2, float density = 0.85f, float offsetRange = 0.5f)
{
    TerrainGeneration tg = GameObject.FindObjectOfType<TerrainGeneration>();
    if (tg == null) return;

    for (int inset = 0; inset < layers; inset++)
    {
        List<Vector3> edgePositions = tg.EdgePositions(heightMap, inset);

        foreach (Vector3 pos in edgePositions)
        {
            if (UnityEngine.Random.value <= density)
            {
                Vector3 randomOffset = new Vector3(
                    UnityEngine.Random.Range(-offsetRange, offsetRange),
                    0,
                    UnityEngine.Random.Range(-offsetRange, offsetRange)
                );

                Vector3 spawnPos = pos + randomOffset;

                GameObject treePrefab = treePrefabs[UnityEngine.Random.Range(0, treePrefabs.Length)];
                Quaternion rot = Quaternion.Euler(0, UnityEngine.Random.Range(0f, 360f), 0);

                GameObject.Instantiate(treePrefab, spawnPos, rot);
            }
        }
    }
}
    
   
}
