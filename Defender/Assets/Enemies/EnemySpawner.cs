using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
   
    public EnemyDetails easyEnemy;
    public EnemyDetails mediumEnemy;
    public EnemyDetails hardEnemy;

   
    public TerrainGeneration terrainGen;
    public float spawnDelay = 2f;
    public int baseEnemiesPerWave = 5;
    public int waveCount = 5;

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

        StartCoroutine(WaitForTerrainReadyAndSpawn());
    }

    private IEnumerator WaitForTerrainReadyAndSpawn()
    {
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
            // Enemy count scaling formula
            int enemiesThisWave = Mathf.RoundToInt(baseEnemiesPerWave * Mathf.Pow(1.2f, currentWave));
            aliveEnemies = enemiesThisWave;

            Debug.Log($"[Wave {currentWave + 1}] Spawning {enemiesThisWave} enemies.");

            for (int i = 0; i < enemiesThisWave; i++)
            {
                SpawnEnemy();
                yield return new WaitForSeconds(0.5f);
            }

            // Wait until most enemies are dead before next wave
            yield return new WaitUntil(() => aliveEnemies <= enemiesThisWave / 4);

            currentWave++;
            if (currentWave < waveCount)
                yield return new WaitForSeconds(spawnDelay);
        }

        Debug.Log("All waves completed!");
    }

    private void SpawnEnemy()
    {
        if (!terrainReady) return;

        Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Count)];
        EnemyDetails selectedEnemy = SelectEnemyType();

        if (selectedEnemy == null || selectedEnemy.enemyPrefab == null)
        {
            Debug.LogWarning("EnemySpawner: Missing enemy prefab data!");
            return;
        }

        Enemy enemy = EnemyFactory.CreateEnemy(selectedEnemy, spawnPos, castlePosition, this);

        if (enemy != null)
            enemy.OnDeath += HandleEnemyDeath;
    }

    private EnemyDetails SelectEnemyType()
    {
        // Difficulty weighting increases with wave progression
        float easyWeight = Mathf.Clamp01(1f - (currentWave * 0.2f));   // starts high, drops fast
        float mediumWeight = Mathf.Clamp01(0.4f + (currentWave * 0.1f)); // mid curve
        float hardWeight = Mathf.Clamp01(currentWave * 0.2f);            // starts low, increases steadily

        // Normalize weights
        float total = easyWeight + mediumWeight + hardWeight;
        easyWeight /= total;
        mediumWeight /= total;
        hardWeight /= total;

        // Randomized selection
        float rand = Random.value;

        if (rand < easyWeight)
            return easyEnemy;
        else if (rand < easyWeight + mediumWeight)
            return mediumEnemy;
        else
            return hardEnemy;
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