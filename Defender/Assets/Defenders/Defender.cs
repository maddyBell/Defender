using UnityEngine;

public class Defender : MonoBehaviour
{
    [Header("Stats")]
    public int health = 50;
    public int damage = 10;
    public float attackSpeed = 1f;

    private Animator animator;
    private float nextAttackTime = 0f;
    private Enemy currentEnemyTarget;

    public Transform firePoint;
    public GameObject projectilePrefab;
    

    void Awake()
    {
        animator = GetComponent<Animator>();
    }

    public void TakeDamage(int amount)
    {
        health -= amount;

        if (health <= 0)
        {
            Die();
        }
    }

    private void Die()
    {
        if (animator != null)
            animator.SetTrigger("Die");

        Destroy(gameObject, 2f); // delay for death anim
    }

   private void Update()
{
    if (currentEnemyTarget != null)
    {
        this.transform.LookAt(currentEnemyTarget.transform);

        // Attack cooldown
        if (Time.time >= nextAttackTime)
        {
            nextAttackTime = Time.time + attackSpeed; // attackSpeed = 5 seconds for example

            // play attack animation
            if (animator != null)
                animator.SetBool("IsAttacking", true);

            // Spawn projectile
            GameObject projectile = Instantiate(projectilePrefab, firePoint.position, Quaternion.identity);
            Projectile projScript = projectile.GetComponent<Projectile>();
            if (projScript != null)
                projScript.Launch(currentEnemyTarget.transform, damage);
        }
    }
    else
    {
        if (animator != null)
            animator.SetBool("IsAttacking", false);
    }
}

    // 🔹 Detect enemies entering the defender's range
   private void OnTriggerEnter(Collider other)
{
    // Ignore own colliders
    if (other.gameObject == gameObject || other.transform.IsChildOf(transform))
        return;

    // Try to get the enemy
    Enemy enemy = other.GetComponent<Enemy>();
    if (enemy == null)
        enemy = other.GetComponentInParent<Enemy>();

    if (enemy != null)
    {
        currentEnemyTarget = enemy;
    }
}
    // 🔹 Clear enemy when it leaves
    private void OnTriggerExit(Collider other)
    {
        Enemy enemy = other.GetComponent<Enemy>();
        if (enemy != null && enemy == currentEnemyTarget)
        {
            currentEnemyTarget = null;
        }
    }
}