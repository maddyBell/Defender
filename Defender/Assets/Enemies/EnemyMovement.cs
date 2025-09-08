using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Vector3 target;
    private Animator animator;

    private bool isDead = false;

    [Header("Settings")]
    public float stopDistanceFromTower = 1f;
    public float deathAnimationDuration = 2f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    public void Initialize(Vector3 castlePos, float moveSpeed)
    {
        if (agent != null)
        {
            agent.speed = moveSpeed;
            agent.stoppingDistance = stopDistanceFromTower;
            target = castlePos;
            agent.SetDestination(target);
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        if (agent != null)
            agent.isStopped = true;

        if (animator != null)
            animator.SetBool("IsDead", true);

        Destroy(gameObject, deathAnimationDuration);
    }

    public void StopMoving()
    {
        if (agent != null)
            agent.isStopped = true;

        if (animator != null)
            animator.SetBool("IsWalking", false);
    }

    public void ResumeMoving()
    {
        if (isDead || agent == null) return;

        agent.isStopped = false;
        agent.SetDestination(target);

        if (animator != null)
            animator.SetBool("IsWalking", true);
    }

    private void Update()
    {
        if (isDead || agent == null) return;

        // update animator walking state
        if (animator != null)
            animator.SetBool("IsWalking", agent.velocity.sqrMagnitude > 0.1f);

        // stop at tower
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StopMoving();
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Defender defender = other.GetComponent<Defender>();
        if (defender == null)
        {
            defender = other.GetComponentInParent<Defender>();
        }

        if (defender != null)
        {
            GetComponent<Enemy>().EngageDefender(defender);
        }
    }

    public void MoveToDefender(Defender defender)
    {
        if (agent != null && defender != null)
        {
            agent.SetDestination(defender.transform.position);
        }
    }
}