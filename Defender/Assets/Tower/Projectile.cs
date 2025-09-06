using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 10f;
    private Transform target;
    private int damage;

    public void Launch(Transform enemyTarget, int damageValue)
    {
        Debug.Log("Projectile launched at " + enemyTarget.name + " with damage " + damageValue);
        target = enemyTarget;
        damage = damageValue;
        Destroy(gameObject, 8f); //destroys after 8s if it doesnt hit the enemy
    }

    void Update()
    {
        if (target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        transform.forward = dir;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other == null) return;

        if (other.TryGetComponent<Enemy>(out Enemy enemy))
        {
            enemy.TakeDamage(damage);
            Destroy(gameObject);
            return;
        }

        
    }
}