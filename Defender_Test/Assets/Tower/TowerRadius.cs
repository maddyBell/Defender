using UnityEngine;

public class TowerRadiusTrigger : MonoBehaviour
{
    public Tower tower;
    public bool isAttackRadius = true; 

//keeping track and telling the tower class when an enemy has entered the collider , set this way due to the child/ parent set up on theses colliders 
    private void OnTriggerEnter(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        if (isAttackRadius)
            tower.EnemyEnteredAttackRadius(other.transform);
        else
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
                tower.EnemyEnteredCloseRange(enemy);
        }
    }

//letting the tower class know that the enemy has left the range, letting it delete the enemy from the list 
    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("Enemy")) return;

        if (isAttackRadius)
            tower.EnemyExitedAttackRadius(other.transform);
        else
        {
            Enemy enemy = other.GetComponent<Enemy>();
            if (enemy != null)
                tower.EnemyExitedCloseRange(enemy);
        }
    }
}
