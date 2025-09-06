using UnityEngine;

public class Enemy : MonoBehaviour
{
    private EnemyDetails data;
    private int currentHealth;
    private EnemySpawner spawner;
    private float nextAttackTime = 0f;

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

        Destroy(gameObject);
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
