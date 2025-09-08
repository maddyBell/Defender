using UnityEngine;
using System;
using UnityEngine.UI;

public class Enemy : MonoBehaviour
{
    private EnemyDetails data;
    private int currentHealth;
    private int maxHealth = 100;
    private EnemySpawner spawner;
    private float nextAttackTime = 0f;
    public Image healthbar;
    private GameManager gameManager;

    public event Action<Enemy> OnDeath; 

    public void Initialize(EnemyDetails enemyData, EnemySpawner spawner)
    {
        this.data = enemyData;
        this.spawner = spawner;
        this.currentHealth = data.health;
        gameManager = FindObjectOfType<GameManager>();
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        healthbar.fillAmount = (float)currentHealth / maxHealth;

        if (currentHealth <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
       
        spawner?.OnEnemyKilled();

       
        EnemyMovement move = GetComponent<EnemyMovement>();
        if (move != null) move.Die();

        GetComponent<Collider>().enabled = false;
        gameManager.CollectBones();

       
        OnDeath?.Invoke(this);
    }

    public bool AttackTower()
    {
        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + data.attackSpeed;
            return true;
        }
        return false;
    }

    public int GetTowerDamage() => data.towerDamage;
    public float GetMoveSpeed() => data.movementSpeed;
}