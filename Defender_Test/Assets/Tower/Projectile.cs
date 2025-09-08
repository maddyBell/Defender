using UnityEngine;

public class Projectile : MonoBehaviour
{
    //setting up the projectile that the tower and defenders throw at the enemies 
    public float speed = 10f;
    private Transform target;
    private int damage;


    //getting the target and how much damage needs to be done when the projectile is thrown and destroying it accordingly 
    public void Launch(Transform enemyTarget, int damageValue)
    {

        target = enemyTarget;
        damage = damageValue;
        Destroy(gameObject, 8f); //destroys after 8s if it doesnt hit the enemy
    }

    void Update() // moving the projectile toward the targetted enemy using its position and direction 
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

// applying damage to the enemy if it successfully collides 
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