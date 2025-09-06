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
    private bool spawning = false;
    private Vector3 castlePosition;
    private List<Vector3> spawnPoints;

    void Start()
    {
        if (!terrainGen)
        {
            Debug.LogError("EnemySpawner: TerrainGeneration reference not assigned!");
            return;
        }

        spawnPoints = terrainGen.GetPathStartWorldPositions(terrainGen.HeightMap);
        castlePosition = terrainGen.GetCastleWorldPosition();

        StartCoroutine(SpawnWaveRoutine());
    }

    private IEnumerator SpawnWaveRoutine()
    {
        yield return new WaitForSeconds(spawnDelay);

        while (currentWave < waveCount)
        {
            spawning = true;
            int enemiesThisWave = baseEnemiesPerWave + (currentWave * 3);
            aliveEnemies = enemiesThisWave;

            Debug.Log($"Spawning Wave {currentWave + 1} with {enemiesThisWave} enemies.");

            for (int i = 0; i < enemiesThisWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(0.5f);
            }

            spawning = false;

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
        if (enemyData == null || enemyData.enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: EnemyData not assigned!");
            return;
        }

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

    // 🔹 Still here for compatibility
    public void OnEnemyKilled()
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
    }
}