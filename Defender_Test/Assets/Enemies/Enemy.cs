using UnityEngine;
using System;
using UnityEngine.UI;   

public class Enemy : MonoBehaviour
{

    //private variables to get the enemy data and stuff needed to attack 
    private EnemyDetails data;
    private int currentHealth;
    private EnemySpawner spawner;
    private float nextAttackTime = 0f;
    private Defender currentDefenderTarget;
    private float defenderEngageEndTime = 0f; 

//public variables to use with the wave spawn system, show the health and award bones when dead
    public event Action<Enemy> OnDeath;
    public Image healthBar;
    public GameManager gameManager;

    void Start()
    {
        gameManager = FindObjectOfType<GameManager>();
        Debug.Log("enemy has " + gameManager);
    }
    //initialising the details for the enemy, set up with factory method
    public void Initialize(EnemyDetails enemyData, EnemySpawner spawner, GameManager gameManager)
    {
        Debug.Log(gameManager);
        this.data = enemyData;
        this.spawner = spawner;
        this.currentHealth = data.health;
        this.gameManager = gameManager;

        if (gameManager == null)
        {
            gameManager = FindObjectOfType<GameManager>();
        }

        
    }

// taking damage when hit by the tower or defender projectiles 
    public void TakeDamage(int amount)
    {
        currentHealth -= amount;
        healthBar.fillAmount = (float)currentHealth / data.health; //updating the little health bar

        if (currentHealth <= 0)
        {
            Die();
        }
    }
    
    //running when the enemy dies and letting the movement script and game manager know its dead
    private void Die()
    {
        spawner?.OnEnemyKilled();

        EnemyMovement move = GetComponent<EnemyMovement>();
        if (move != null)
        {
            move.Die();
        }

        GetComponent<Collider>().enabled = false;
        if (gameManager != null)
        {
            gameManager.CollectBones();
        }


        OnDeath?.Invoke(this);
    }

// attacking the tower, wwont just spam attack, has like a recovery time between attacks 
    public bool AttackTower()
    {
        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + data.attackSpeed;
            return true;
        }
        return false;
    }

    public int GetTowerDamage() => data.towerDamage; // getting the amount of damage the enemy does to towers 
    public float GetMoveSpeed() => data.movementSpeed; // getting the speed they can walk at 

    // engaging wih the defenders, engaging for 10 to 15 seconds before going back to the towewre focus
    // uses the movement script to change direction and move toward the defender, looked weird having it just stop in place and caused a blockage in the path
    public void EngageDefender(Defender defender)
    {
        if (defender == null)
        {
            return;
        }

        currentDefenderTarget = defender;

        float engageDuration = UnityEngine.Random.Range(10f, 15f);
        defenderEngageEndTime = Time.time + engageDuration;
        GetComponent<EnemyMovement>().MoveToDefender(defender);
    }

    private void Update()
    {
        if (currentDefenderTarget != null)
        {
            //checks the enemy is still fighting the defender 
            if (Time.time < defenderEngageEndTime)
            {
                // makes sure the enemy is close enoughj 
                EnemyMovement move = GetComponent<EnemyMovement>();
                var agent = move.GetComponent<UnityEngine.AI.NavMeshAgent>();

                if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
                {
                    if (Time.time >= nextAttackTime)
                    {
                        nextAttackTime = Time.time + data.attackSpeed;
                        currentDefenderTarget.TakeDamage(data.defenderDamage); // applying damage to the defender 

                    }
                }
            }
            else
            {
                // diengages from the defender and strarts moving back to the tower 
                currentDefenderTarget = null;
                GetComponent<EnemyMovement>().ResumeMoving();
            }
        }
    }

    // when entering the defenders collider to get the defender and sert up the targetted attacks and apply damage 
    private void OnTriggerEnter(Collider other)
    {
        Defender defender = other.GetComponent<Defender>() ?? other.GetComponentInParent<Defender>();

        if (defender != null)
        {
            EngageDefender(defender);
        }
    }
}