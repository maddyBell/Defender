using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Wave Settings")]
    public int initialEnemyCount = 5;   // base number for wave 1
    public int wavesTotal = 3;          // total waves
    public float spawnDelay = 0.5f;     // delay between each enemy spawn
    public float waveStartDelay = 2f;   // delay before a new wave starts

    [Header("References")]
    public TerrainGeneration terrain;   // drag your TerrainGeneration here
    public EnemyDetails enemyType;         // single ScriptableObject enemy type

    private int currentWave = 0;
    private int enemiesAlive = 0;
    private int enemiesToSpawn;
    private bool waveInProgress = false;

    private List<Vector3> spawnPositions;

    private void Start()
    {
        StartCoroutine(InitAndRun());
    }

    // Wait for terrain to finish generating (HeightMap + path starts) before spawning
    private IEnumerator InitAndRun()
    {
        if (terrain == null)
        {
            Debug.LogError("WaveSpawner: Terrain reference is null.");
            yield break;
        }

        // wait until terrain has a HeightMap and path starts
        while (terrain.HeightMap == null || terrain.pathStartPositions == null || terrain.pathStartPositions.Length == 0)
        {
            Debug.Log("WaveSpawner: waiting for terrain to generate path start positions...");
            yield return null;
        }

        spawnPositions = terrain.GetPathStartWorldPositions(terrain.HeightMap);
        if (spawnPositions == null || spawnPositions.Count == 0)
        {
            Debug.LogError("WaveSpawner: no spawn positions found on terrain.");
            yield break;
        }

        // small delay to stabilize
        yield return new WaitForSeconds(0.2f);

        // Kick off first wave
        StartCoroutine(StartNextWave());
    }

    private IEnumerator StartNextWave()
    {
        currentWave++;
        enemiesToSpawn = initialEnemyCount * currentWave; // scale: wave 1 -> initial*1, wave2 -> initial*2, etc.
        Debug.Log($"WaveSpawner: Starting wave {currentWave} with {enemiesToSpawn} enemies.");

        yield return new WaitForSeconds(waveStartDelay);

        enemiesAlive = enemiesToSpawn;
        waveInProgress = true;

        for (int i = 0; i < enemiesToSpawn; i++)
        {
            Vector3 spawnPos = spawnPositions[Random.Range(0, spawnPositions.Count)];
            Vector3 castlePos = terrain.GetCastleWorldPosition();

            // spawn enemy (factory takes care of initialization & rotation)
            EnemyFactory.CreateEnemy(enemyType, spawnPos, castlePos, this);

            yield return new WaitForSeconds(spawnDelay);
        }
    }

    // Called by enemies when they die
    public void OnEnemyKilled()
    {
        enemiesAlive--;
        if (enemiesAlive < 0) enemiesAlive = 0;

        // If enough are dead (<= 25% remain), queue next wave
        if (waveInProgress && enemiesAlive <= Mathf.CeilToInt(enemiesToSpawn * 0.25f))
        {
            waveInProgress = false; // prevent multiple triggers

            if (currentWave < wavesTotal)
            {
                Debug.Log($"WaveSpawner: Threshold reached — scheduling wave {currentWave + 1}.");
                StartCoroutine(StartNextWave());
            }
            else
            {
                Debug.Log("WaveSpawner: All waves finished.");
            }
        }
    }
}