using UnityEngine;
using System;

public class Enemy : MonoBehaviour
{
    private EnemyDetails data;
    private int currentHealth;
    private EnemySpawner spawner;
    private float nextAttackTime = 0f;

    private Defender currentDefenderTarget;
    private float defenderEngageEndTime = 0f; // time to move on

    public event Action<Enemy> OnDeath;

    public void Initialize(EnemyDetails enemyData, EnemySpawner spawner)
    {
        this.data = enemyData;
        this.spawner = spawner;
        this.currentHealth = data.health;
    }

    public void TakeDamage(int amount)
    {
        currentHealth -= amount;

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

    // --- Defender interactions ---
    public void EngageDefender(Defender defender)
    {
        currentDefenderTarget = defender;
        defenderEngageEndTime = Time.time + 5f; // attack for 5 seconds
        GetComponent<EnemyMovement>().MoveToDefender(defender);
    }

    private void Update()
    {
        if (currentDefenderTarget != null)
        {
            // Still within 5 seconds
            if (Time.time < defenderEngageEndTime)
            {
                if (Time.time >= nextAttackTime)
                {
                    nextAttackTime = Time.time + data.attackSpeed;
                    currentDefenderTarget.TakeDamage(data.towerDamage);
                }
            }
            else
            {
                // Done engaging, move on
                currentDefenderTarget = null;
                GetComponent<EnemyMovement>().ResumeMoving();
            }
        }
    }
}