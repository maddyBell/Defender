using System.Collections;
using System.Collections.Generic;
using System.Security.Cryptography.X509Certificates;
using UnityEngine;

public class Tower : MonoBehaviour
{
    public int health;
    private int maxHealth = 100;

    public int attackDamage = 10;
    public float fireWait = 10f;
    public GameObject projectilePrefab;
    public Transform firePoint;

    public float closeRangeWait = 5f;

    private List<Transform> enemiesInRange = new List<Transform>();
    private List<Enemy> closeRangeEnemies = new List<Enemy>();

    private Coroutine firingCoroutine;
    private Coroutine closeRangeCoroutine;


    void Start()
    {
        health = maxHealth;

    }

    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy"))
        {
            return;
        }

        if (other.gameObject.name.Contains("AttackRadius"))
        {
            if (!enemiesInRange.Contains(other.transform))
            {
                enemiesInRange.Add(other.transform);
            }

            if (firingCoroutine == null)
            {
                firingCoroutine = StartCoroutine(FireAtEnemies());
            }
        }
        else if (other.gameObject.name.Contains("CloseRadius"))
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null && !closeRangeEnemies.Contains(enemy))
            {
                closeRangeEnemies.Add(enemy);
            }

            if (closeRangeCoroutine == null)
            {
                closeRangeCoroutine = StartCoroutine(ApplyCloseDamage());
            }
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Enemy"))
        {
            return;
        }

        if (other.gameObject.name.Contains("AttackRadius"))
        {
            if (enemiesInRange.Contains(other.transform))
            {
                enemiesInRange.Remove(other.transform);
            }

            if (enemiesInRange.Count == 0 && firingCoroutine != null)
            {
                StopCoroutine(firingCoroutine);
                firingCoroutine = null;
            }
        }
        else if (other.gameObject.name.Contains("CloseRadius"))
        {
            Enemy enemy = other.GetComponentInParent<Enemy>();
            if (enemy != null && closeRangeEnemies.Contains(enemy))
            {
                closeRangeEnemies.Remove(enemy);
            }

            if (closeRangeEnemies.Count == 0 && closeRangeCoroutine != null)
            {
                StopCoroutine(closeRangeCoroutine);
                closeRangeCoroutine = null;
            }
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
                {
                    projScript.Launch(target, attackDamage);
                }
            }
            yield return new WaitForSeconds(fireWait);
        }

    }

    private Transform ClosestEnemy()
    {
        Transform closest = null;
        float minDistance = Mathf.Infinity;

        foreach (Transform enemy in enemiesInRange)
        {
            if (enemy == null)
            {
                continue;
            }
            float distance = Vector3.Distance(transform.position, enemy.position);
            if (distance < minDistance)
            {
                minDistance = distance;
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
                if (enemy != null && enemy.AttacKTower())
                {
                    TakeDamage(enemy.GetTowerDamage());
                }

            }
            yield return new WaitForSeconds(closeRangeWait);
        }
    }

    public void TakeDamage(int damage)
    {
        health -= damage;
        if (health <= 0)
        {
            Die();
        }
    }
    
    private void Die()
    {
        Debug.Log("Tower Destroyed");
        Destroy(gameObject);
    }

}
