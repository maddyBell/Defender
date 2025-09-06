using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Tower : MonoBehaviour
{
    [Header("Tower Stats")]
    public int maxHealth = 100;
    private int health;
    public int attackDamage = 10;

    [Header("Projectile")]
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireInterval = 10f;

    [Header("Close Range")]
    public float closeDamageInterval = 5f;
    public int closeDamageAmount = 2;

    [Header("Visuals")]
    public Mesh midMesh;
    public Mesh lowMesh;

    // Colliders
    public SphereCollider attackRadiusCollider;
    public SphereCollider closeRadiusCollider;

    // Tracking enemies
    private List<Transform> enemiesInRange = new List<Transform>();
    private List<Enemy> closeRangeEnemies = new List<Enemy>();

    private Coroutine firingCoroutine;
    private Coroutine closeRangeCoroutine;

    void Start()
    {
        health = maxHealth;

        if (attackRadiusCollider == null || closeRadiusCollider == null)
        {
            Debug.LogError("Please assign both radius colliders in the Inspector!");
        }
    }

    // Called by the child radius trigger scripts
    public void EnemyEnteredAttackRadius(Transform enemy)
    {
        if (!enemiesInRange.Contains(enemy))
            enemiesInRange.Add(enemy);

        if (firingCoroutine == null)
            firingCoroutine = StartCoroutine(FireAtEnemies());
    }

    public void EnemyExitedAttackRadius(Transform enemy)
    {
        enemiesInRange.Remove(enemy);

        if (enemiesInRange.Count == 0 && firingCoroutine != null)
        {
            StopCoroutine(firingCoroutine);
            firingCoroutine = null;
        }
    }

    public void EnemyEnteredCloseRange(Enemy enemy)
    {
        if (!closeRangeEnemies.Contains(enemy))
            closeRangeEnemies.Add(enemy);

        if (closeRangeCoroutine == null)
            closeRangeCoroutine = StartCoroutine(ApplyCloseDamage());
    }

    public void EnemyExitedCloseRange(Enemy enemy)
    {
        closeRangeEnemies.Remove(enemy);

        if (closeRangeEnemies.Count == 0 && closeRangeCoroutine != null)
        {
            StopCoroutine(closeRangeCoroutine);
            closeRangeCoroutine = null;
        }
    }

    private IEnumerator FireAtEnemies()
    {
        while (enemiesInRange.Count > 0)
        {
            Transform target = ClosestEnemy();
            if (target != null)
            {
                GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                Projectile projScript = projectile.GetComponent<Projectile>();
                if (projScript != null)
                    projScript.Launch(target, attackDamage);
            }
            yield return new WaitForSeconds(fireInterval);
        }
    }

    private Transform ClosestEnemy()
    {
        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (Transform enemy in enemiesInRange)
        {
            if (enemy == null) continue;
            float dist = Vector3.Distance(transform.position, enemy.position);
            if (dist < minDistance)
            {
                minDistance = dist;
                closest = enemy;
            }
        }

        return closest;
    }

    private IEnumerator ApplyCloseDamage()
    {
        while (closeRangeEnemies.Count > 0)
        {
            foreach (Enemy enemy in closeRangeEnemies)
            {
                if (enemy != null)
                    TakeDamage(enemy.GetTowerDamage()); // enemy damage method
            }
            yield return new WaitForSeconds(closeDamageInterval);
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;

        if (health <= 66 && midMesh != null)
        {
              GetComponent<MeshFilter>().mesh = midMesh;
        }

        if (health <= 33 && lowMesh != null)
        {
            GetComponent<MeshFilter>().mesh = lowMesh;
        }
        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        Debug.Log("Tower destroyed");
        Destroy(gameObject);
    }
}