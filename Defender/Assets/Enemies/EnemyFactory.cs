using UnityEngine;

public static class EnemyFactory
{
    public static GameObject CreateEnemy(EnemyDetails data, Vector3 spawnPosition, Vector3 targetPosition)
    {
        GameObject enemy = Object.Instantiate(data.enemyPrefab, spawnPosition, Quaternion.identity);

        // rotate enemy to face the target (castle)
        Vector3 direction = (targetPosition - spawnPosition).normalized;
        if (direction != Vector3.zero)
        {
            enemy.transform.rotation = Quaternion.LookRotation(direction);
        }

        // attach Enemy script with data
        Enemy enemyComponent = enemy.GetComponent<Enemy>();
        if (enemyComponent == null)
        {
            enemyComponent = enemy.AddComponent<Enemy>();
        }
        enemyComponent.Initialize(data);

        return enemy;
    }
}
