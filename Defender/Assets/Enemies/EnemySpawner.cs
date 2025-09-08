using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawner Settings")]
    public EnemyDetails enemyData;
    public TerrainGeneration terrainGen;
    public float spawnDelay = 2f;
    public int baseEnemiesPerWave = 5;
    public int waveCount = 3;

    private int currentWave = 0;
    private int aliveEnemies = 0;
    private List<Vector3> spawnPoints;
    private Vector3 castlePosition;

    private bool terrainReady = false;

    void Start()
    {
        if (terrainGen == null)
        {
            Debug.LogError("EnemySpawner: TerrainGeneration reference not assigned!");
            return;
        }

        // Subscribe to terrain ready callback
        StartCoroutine(WaitForTerrainReadyAndSpawn());
    }

    private IEnumerator WaitForTerrainReadyAndSpawn()
    {
        // Wait until TerrainGeneration has baked the NavMesh and exposed path positions
        yield return new WaitUntil(() => terrainGen.PathStartWorldPositions != null && terrainGen.PathStartWorldPositions.Count > 0);

        spawnPoints = terrainGen.PathStartWorldPositions;
        castlePosition = terrainGen.CastleWorldPosition;
        terrainReady = true;

        Debug.Log("Terrain ready, starting enemy waves.");
        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);

        while (currentWave < waveCount)
        {
            int enemiesThisWave = baseEnemiesPerWave + (currentWave * 3);
            aliveEnemies = enemiesThisWave;

            Debug.Log($"Spawning Wave {currentWave + 1} with {enemiesThisWave} enemies.");

            for (int i = 0; i < enemiesThisWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(0.5f);
            }

            // wait until 3/4 enemies are dead
            yield return new WaitUntil(() => aliveEnemies <= enemiesThisWave / 4);

            currentWave++;
            if (currentWave < waveCount)
                yield return new WaitForSeconds(spawnDelay);
        }

        Debug.Log("All waves completed!");
    }

    private void SpawnEnemy()
    {
        if (!terrainReady || enemyData == null || enemyData.enemyPrefab == null) return;

        Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Count)];
        Enemy enemy = EnemyFactory.CreateEnemy(enemyData, spawnPos, castlePosition, this);

        if (enemy != null)
            enemy.OnDeath += HandleEnemyDeath;
    }

    private void HandleEnemyDeath(Enemy enemy)
    {
        aliveEnemies--;
        enemy.OnDeath -= HandleEnemyDeath;
    }

    public void OnEnemyKilled()
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
    }
}