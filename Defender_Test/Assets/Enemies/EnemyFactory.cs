using UnityEngine;
using UnityEngine.AI;

public static class EnemyFactory
{
    public static Enemy CreateEnemy(EnemyDetails data, Vector3 spawnPosition, Vector3 targetPosition, EnemySpawner spawner, GameManager gameManager)
    {
        if (data == null || data.enemyPrefab == null)
        {
            Debug.LogError("EnemyFactory: enemy data or prefab is null.");
            return null;
        }

        // Snap spawnPosition to NavMesh
        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPosition, out hit, 5f, NavMesh.AllAreas))
        {
            spawnPosition = hit.position;
        }
        else
        {
            Debug.LogWarning("EnemyFactory: Could not find valid NavMesh near spawn position. Using original position.");
        }

        // Instantiate enemy
        GameObject obj = Object.Instantiate(data.enemyPrefab, spawnPosition, Quaternion.identity);

        // Make enemy face the target
        Vector3 dir = targetPosition - spawnPosition;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
        {
            obj.transform.rotation = Quaternion.LookRotation(dir);
        }

        // Ensure Enemy component exists
        Enemy enemy = obj.GetComponent<Enemy>() ?? obj.AddComponent<Enemy>();
        enemy.Initialize(data, spawner, gameManager);

        // Ensure EnemyMovement exists and initialize it
        EnemyMovement movement = obj.GetComponent<EnemyMovement>();
        if (movement == null)
        {
            Debug.LogError("Enemy prefab missing EnemyMovement script!");
            return null;
        }

        // Snap target to NavMesh as well (optional but safe)
        if (NavMesh.SamplePosition(targetPosition, out hit, 5f, NavMesh.AllAreas))
        {
            targetPosition = hit.position;
        }

        obj.SetActive(true); // ensure Update runs
        movement.Initialize(targetPosition, data.movementSpeed);

        // Force the NavMeshAgent to update its path immediately
        NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();
        if (agent != null)
        {
            agent.Warp(spawnPosition); // guarantees agent starts on navmesh
            agent.SetDestination(targetPosition); // starts movement immediately
        }

        Debug.Log($"EnemyFactory: Enemy '{enemy.name}' spawned and moving to {targetPosition}");
        return enemy;
    }
}