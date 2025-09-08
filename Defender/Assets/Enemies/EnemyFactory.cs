using UnityEngine;
using UnityEngine.AI;

public static class EnemyFactory
{
    public static Enemy CreateEnemy(EnemyDetails data, Vector3 spawnPosition, Vector3 targetPosition, EnemySpawner spawner)
    {
        if (data == null || data.enemyPrefab == null)
        {
            Debug.LogError("EnemyFactory: Enemy data or prefab is null.");
            return null;
        }

        NavMeshHit hit;
        if (NavMesh.SamplePosition(spawnPosition, out hit, 2f, NavMesh.AllAreas))
        {
            spawnPosition = hit.position;
        }

        GameObject obj = Object.Instantiate(data.enemyPrefab, spawnPosition, Quaternion.identity);

        Vector3 dir = targetPosition - spawnPosition;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            obj.transform.rotation = Quaternion.LookRotation(dir);


        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy == null)
            enemy = obj.AddComponent<Enemy>();

        enemy.Initialize(data, spawner);


        EnemyMovement move = obj.GetComponent<EnemyMovement>();
        if (move != null)
        {
            move.Initialize(targetPosition, data.movementSpeed);


            NavMeshAgent agent = obj.GetComponent<NavMeshAgent>();
            if (agent != null && !agent.isOnNavMesh)
            {
                agent.Warp(spawnPosition);
                agent.enabled = true;
                agent.SetDestination(targetPosition);
            }
        }

        return enemy;
    }
}