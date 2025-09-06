using UnityEngine;

public static class EnemyFactory
{
    public static Enemy CreateEnemy(EnemyDetails data, Vector3 spawnPosition, Vector3 targetPosition, EnemySpawner spawner)
    {
        if (data == null || data.enemyPrefab == null)
        {
            Debug.LogError("EnemyFactory: enemy data or prefab is null.");
            return null;
        }

        GameObject obj = Object.Instantiate(data.enemyPrefab, spawnPosition, Quaternion.identity);

        // Rotate toward target (castle)
        Vector3 dir = targetPosition - spawnPosition;
        dir.y = 0f;
        if (dir.sqrMagnitude > 0.0001f)
            obj.transform.rotation = Quaternion.LookRotation(dir);

        Enemy enemy = obj.GetComponent<Enemy>();
        if (enemy == null)
            enemy = obj.AddComponent<Enemy>();

        enemy.Initialize(data, spawner);
        obj.GetComponent<EnemyMovement>()?.Initialize(targetPosition, data.movementSpeed);

        return enemy;
    }
}