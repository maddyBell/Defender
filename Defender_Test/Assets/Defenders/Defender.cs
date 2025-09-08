using UnityEngine;
using UnityEngine.UI;

public class Defender : MonoBehaviour
{
    //public variable to get the needed stats and the details 
    public int health, startHealth = 100, damage = 15;
    public float attackSpeed = 1f;
    public Transform firePoint;
    public GameObject projectilePrefab;
    public Image healthBar;

    //private variables getting the animtor to play animations, set an attack timer and get the enemy targetted
    private Animator animator;
    private float nextAttackTime = 0f;
    private Enemy currentEnemyTarget;

  


    void Awake()
    {
        animator = GetComponent<Animator>();
        health = startHealth;
    }

//making the defender take damage, updating the health bar and checking if its dead
    public void TakeDamage(int amount)
    {
        health -= amount;
        healthBar.fillAmount = (float)health / startHealth;

        if (health <= 0)
        {
            Die();
        }
    }

    //run when the defender is dead, playing the animation and destroying the object after a delay
    private void Die()
    {
        if (animator != null)
            animator.SetTrigger("Die");

        Destroy(gameObject, 2f); // delay for death anim
    }

//running the update method, checking if there is an emeny in the defenders range and running the attack funstions 
    private void Update()
    {
        if (currentEnemyTarget != null)
        {
            this.transform.LookAt(currentEnemyTarget.transform);

            // Attack cooldown
            if (Time.time >= nextAttackTime)
            {
                nextAttackTime = Time.time + attackSpeed; // attackSpeed = 5 seconds for example

                // getting the animator to play the attack animation ** some issue with attack, idle may be too long so is messing uop
                if (animator != null)
                    animator.SetBool("IsAttacking", true);

                // spawning a projectile that targets the enemy 
                GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
                Projectile projScript = projectile.GetComponent<Projectile>();
                if (projScript != null)
                    projScript.Launch(currentEnemyTarget.transform, damage);
            }
        }
        else
        { // revert back to idle animation if no enemies to attack 
            if (animator != null)
                animator.SetBool("IsAttacking", false);
        }
    }

   private void OnTriggerEnter(Collider other)
{
    // ignoring the space radius specifically cos was casuing issues with detecting enemies 
    if (other.gameObject == gameObject || other.transform.IsChildOf(transform))
        return;

   //getting the enemy off the trigger, double chefcking cos some enemy's werent picking up so backup with the parent stuff 
    Enemy enemy = other.GetComponent<Enemy>();
    if (enemy == null)
        enemy = other.GetComponentInParent<Enemy>();

    if (enemy != null)
    {
        currentEnemyTarget = enemy;
    }
}
   // tracks the exit so diesnt get stuck on attacking out of range enemies 
    private void OnTriggerExit(Collider other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null && enemy == currentEnemyTarget)
        {
            currentEnemyTarget = null;
        }
    }
}