using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy : MonoBehaviour
{
    private EnemyDetails details;
    private int currentHealth;
    private float nextAttack = 0f;

    private EnemySpawner enemySpawner;

    public void Initialize(EnemyDetails enemyDetails)
    {
        details = enemyDetails;
        currentHealth = details.health;
        gameObject.name = details.enemyName;
        enemySpawner = FindObjectOfType<EnemySpawner>();
    }

    public int GetTowerDamage() => details.towerDamage;
    public float GetMoveSpeed() => details.movementSpeed;

    public void TakeDamage(int damageAmount)
    {
        currentHealth -= damageAmount;
        Debug.Log(currentHealth);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    public bool AttacKTower()
    {
        if (Time.time >= nextAttack)
        {
            nextAttack = Time.time + details.attackSpeed;
            return true;
        }
        else
        {
            return false;
        }
    }

    private void Die()
    {
        Debug.Log(details.enemyName + " died");
        enemySpawner.OnEnemyKilled();
        Destroy(gameObject);
    }
}
