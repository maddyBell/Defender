using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
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
    public GameManager gameManager;

   /* void Start()
    {
        currentWave = 0;
        Debug.Log(gameManager);
        //getting the spawn points and the castles position
        if (!terrainGen)
        {
            Debug.LogError("EnemySpawner: TerrainGeneration reference not assigned!");
            return;
        }
        Debug.Log("stuff");

        spawnPoints = terrainGen.GetPathStartWorldPositions(terrainGen.HeightMap);

        castlePosition = terrainGen.GetCastleWorldPosition();

        //start spawneave routine 

    }*/

    //using a wave based system to spawn the enemies, i think its more fun, and looks better 
    private IEnumerator SpawnWaveRoutine()
    {
        Debug.Log("Wave Coroutine");
        // yield return new WaitForSeconds(spawnDelay); // delays the spawn so so everything can spawn in the map and the player can look at the map before being bombarded by enemies

        while (currentWave < waveCount)
        {
            Debug.Log("currentWave< waveCount");
            spawning = true;
            int enemiesThisWave = baseEnemiesPerWave + (currentWave * 3); // increases the number of enemies spawned per wave so each wave is harder 
            aliveEnemies = enemiesThisWave;



            for (int i = 0; i < enemiesThisWave; i++)
            {
                Debug.Log("running");
                SpawnEnemy();
                //yield return new WaitForSeconds(0.5f);
            }

            spawning = false;

            // waits until 3/4 of the enemies in the wave are dead so there not too many enemies on the map at once 
            yield return new WaitUntil(() => aliveEnemies <= enemiesThisWave / 4);

            currentWave++;
            if (currentWave < waveCount)
                yield return new WaitForSeconds(spawnDelay);
        }
    }

    private void SpawnEnemy()
    {
        if (enemyData == null || enemyData.enemyPrefab == null)
        {
            Debug.LogError("EnemySpawner: EnemyData not assigned!");
            return;
        }

        // randomising which path the enemy will spawn on 
        Vector3 spawnPos = spawnPoints[Random.Range(0, spawnPoints.Count)];

        // using the factory to create the actual enemies 
        Enemy enemy = EnemyFactory.CreateEnemy(enemyData, spawnPos, castlePosition, this, gameManager);
        Debug.Log("Spawning enemy at " + spawnPos + " | Prefab = " + enemyData.enemyPrefab.name);
        Debug.Log("spawn");
        if (enemy != null)
            enemy.OnDeath += HandleEnemyDeath;
    }

    // updating the number of alive enemies so new wave can trigger 
    private void HandleEnemyDeath(Enemy enemy)
    {
        aliveEnemies--;
        enemy.OnDeath -= HandleEnemyDeath;
    }

    //links to the death in the enemy script 
    public void OnEnemyKilled()
    {
        aliveEnemies = Mathf.Max(0, aliveEnemies - 1);
    }

    public void StartSpawning()
    {
        StartCoroutine(SpawnWaveRoutine());
        Debug.Log("Called by terrainGen");
    }

    public void SetupSpawner(TerrainGeneration terrain)
{
    terrainGen = terrain;
    spawnPoints = terrainGen.GetPathStartWorldPositions(terrainGen.HeightMap);
    castlePosition = terrainGen.GetCastleWorldPosition();
}
}