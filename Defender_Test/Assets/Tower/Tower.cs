using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class Tower : MonoBehaviour
{
   
    public int maxHealth = 100;
    private int health;
    public int attackDamage = 25;

  
    public GameObject projectilePrefab;
    public Transform firePoint;
    public float fireInterval = 3f;

 
    public float closeDamageInterval = 5f;
    //public int closeDamageAmount = 2;

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
    public GameManager gameManager;

    public Image healthBar;

    void Start() // setting up the health, making sure the colliders are in and finding the game manager 
    {
        health = maxHealth;

        if (attackRadiusCollider == null || closeRadiusCollider == null)
        {
            Debug.LogError("Please assign both radius colliders in the Inspector!");
        }
        gameManager = FindObjectOfType<GameManager>();
    }

    // Called by the child radius trigger scripts
    public void EnemyEnteredAttackRadius(Transform enemy)
    {
        if (!enemiesInRange.Contains(enemy))
            enemiesInRange.Add(enemy); // keeping track of all the enemies within the fireing range 

        if (firingCoroutine == null)
            firingCoroutine = StartCoroutine(FireAtEnemies()); // starts a coroutine to fire at the enemies, easier to structure in a way that feels more realistic and not just spam shoot projectiles 
    }

    public void EnemyExitedAttackRadius(Transform enemy) 
    {
        //removes the enemy from the attack radius mainly to prevent the tower from shooting in the directions of enemies that have already died 
        enemiesInRange.Remove(enemy);

        if (enemiesInRange.Count == 0 && firingCoroutine != null)
        {
            StopCoroutine(firingCoroutine);
            firingCoroutine = null;
        }
    }

    public void EnemyEnteredCloseRange(Enemy enemy) // checking if the enemies have entered the close rrange trigger where they can apply damage to the tower 
    {
        if (!closeRangeEnemies.Contains(enemy))
            closeRangeEnemies.Add(enemy);

        if (closeRangeCoroutine == null)
            closeRangeCoroutine = StartCoroutine(ApplyCloseDamage());
    }

    public void EnemyExitedCloseRange(Enemy enemy) // checking if the enemy is still there again to prevent stupid shooting 
    {
        closeRangeEnemies.Remove(enemy);

        if (closeRangeEnemies.Count == 0 && closeRangeCoroutine != null)
        {
            StopCoroutine(closeRangeCoroutine);
            closeRangeCoroutine = null;
        }
    }

//setting up the projectiles to fire at the enemies direction and taking a small break between firing for a realistic feel 
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

//getting the position of the enemy closest to the tower, to set it as the priority target 
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

//taking damage from the enemies that are within the range to inflict damage 
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

//actually applying the damage the enemies are giving, updating the health bar and adding a visual destruction effect by changing the mesh filter 
    public void TakeDamage(int damage)
    {
        health -= damage;
        healthBar.fillAmount = (float)health / maxHealth;

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

    //run when the tower has died, destroying the object and triggering the game over screen 

    private void Die()
    {
        Debug.Log("Tower destroyed");
        gameManager.TowerDead(true);
        Destroy(gameObject);
    }
}