using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Vector3 target;
    private bool isDead = false;

    public void Initialize(Vector3 castlePos, float moveSpeed)
    {
        agent = GetComponent<NavMeshAgent>();
        agent.speed = moveSpeed;
        target = castlePos;
        agent.SetDestination(target);
    }

    public void Die()
    {
        isDead = true;
        if (agent != null)
            agent.isStopped = true;
    }

    private void Update()
    {
        if (!isDead && agent != null && !agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            // Enemy reached the tower – stop moving
            agent.isStopped = true;
        }
    }
}