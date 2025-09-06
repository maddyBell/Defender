using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    public int initialEnemyCount = 5;   // base number of enemies in wave 1
    public int wavesTotal = 3;          // total waves
    public float spawnDelay = 0.5f;     // delay between enemy spawns
    public float waveStartDelay = 2f;   // delay before a new wave starts

    [Header("References")]
    public TerrainGeneration terrain;
    public EnemyDetails[] enemyTypes;      // ScriptableObjects for enemies

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private int enemiesToSpawn;
    private bool waveInProgress = false;

    private List<Vector3> spawnPositions;

    private void Start()
    {
        spawnPositions = terrain.GetPathStartWorldPositions(terrain.HeightMap);
        StartCoroutine(StartNextWave()); // kick off wave 1
    }

    private void Update()
    {
        // If wave is running and enough enemies are dead → start next wave
        if (waveInProgress && enemiesAlive <= enemiesToSpawn * 0.25f)
        {
            waveInProgress = false;

            if (currentWave < wavesTotal)
            {
                StartCoroutine(StartNextWave());
            }
            else
            {
                Debug.Log("All waves complete! Level finished.");
            }
        }
    }

    private IEnumerator StartNextWave()
    {
        currentWave++;
        enemiesToSpawn = initialEnemyCount * currentWave; // scale enemies per wave
        Debug.Log($"Spawning wave {currentWave} with {enemiesToSpawn} enemies.");

        yield return new WaitForSeconds(waveStartDelay); // delay before wave starts

        enemiesAlive = enemiesToSpawn;
        waveInProgress = true;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            // pick spawn + random enemy type
            Vector3 spawnPos = spawnPositions[Random.Range(0, spawnPositions.Count)];
            EnemyDetails enemyData = enemyTypes[Random.Range(0, enemyTypes.Length)];

            // spawn facing castle
            EnemyFactory.CreateEnemy(enemyData, spawnPos, terrain.GetCastleWorldPosition());

            yield return new WaitForSeconds(spawnDelay); // delay between spawns
        }
    }

    // Called by enemies when they die
    public void OnEnemyKilled()
    {
        enemiesAlive--;
        if (enemiesAlive < 0) enemiesAlive = 0;
    }
}