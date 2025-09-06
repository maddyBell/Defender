using UnityEngine;

public class TowerRadiusTrigger : MonoBehaviour
{
    public Tower tower;
    public bool isAttackRadius = true; 

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
