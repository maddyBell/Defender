using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class TerrainDecoration
{
    public GameObject[] trees, grass, rocks;
    private Vector3[] possiblePositions, filledPositions;

    public int numberOfTrees, numberOfGrass, numberOfRocks;

    public TerrainDecoration(GameObject[] trees, GameObject[] grass, GameObject[] rocks, int numberOfTrees, int numberOfGrass, int numberOfRocks)
    {
        this.trees = trees;
        this.grass = grass;
        this.rocks = rocks;
        this.numberOfTrees = numberOfTrees;
        this.numberOfGrass = numberOfGrass;
        this.numberOfRocks = numberOfRocks;
    }

    //a method to place the decorations on the terrain at ranom positions based on the numbber of decorations specified
    public void PlaceDecoration(Vector3[] positions)
    {
        possiblePositions = positions;
        filledPositions = new Vector3[numberOfTrees + numberOfGrass + numberOfRocks];
        int decorPlaced = 0;
        for (int i = 0; i < numberOfTrees; i++)
        {
            int randomIndex = Random.Range(0, possiblePositions.Length);
            int randomTree = Random.Range(0, trees.Length);
            GameObject.Instantiate(trees[randomTree], possiblePositions[randomIndex], Quaternion.Euler(0, Random.Range(0, 360), 0));
            filledPositions[decorPlaced] = possiblePositions[randomIndex];
            decorPlaced++;
        }
        for (int i = 0; i < numberOfGrass; i++)
        {
            int randomIndex = Random.Range(0, possiblePositions.Length);
            int randomGrass = Random.Range(0, grass.Length);
            GameObject.Instantiate(grass[randomGrass], possiblePositions[randomIndex], Quaternion.Euler(0, Random.Range(0, 360), 0));
            filledPositions[decorPlaced] = possiblePositions[randomIndex];
            decorPlaced++;
        }
        for (int i = 0; i < numberOfRocks; i++)
        {
            int randomIndex = Random.Range(0, possiblePositions.Length);
            int randomRock = Random.Range(0, rocks.Length);
            GameObject.Instantiate(rocks[randomRock], possiblePositions[randomIndex], Quaternion.Euler(0, Random.Range(0, 360), 0));
            filledPositions[decorPlaced] = possiblePositions[randomIndex];
            decorPlaced++;
        }
    }
   
}
