using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Vector3 target;
    private Animator animator;
    private bool isDead = false;
    private bool isInitialized = false;

    public float stopDistanceFromTower = 1f;
    public float deathAnimationDuration = 2f;

    void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Initializes movement toward the castle and forces immediate NavMesh pathing.
    /// </summary>
    public void Initialize(Vector3 castlePos, float moveSpeed)
    {
        if (agent == null) return;

        // Snap spawn position to NavMesh to avoid issues on irregular terrain
        NavMeshHit hit;
        if (NavMesh.SamplePosition(transform.position, out hit, 5f, NavMesh.AllAreas))
        {
            agent.Warp(hit.position); // ensures the agent starts on NavMesh
        }

        // Snap target position to NavMesh for safety
        if (NavMesh.SamplePosition(castlePos, out hit, 5f, NavMesh.AllAreas))
        {
            castlePos = hit.position;
        }

        target = castlePos;
        agent.speed = moveSpeed;
        agent.stoppingDistance = stopDistanceFromTower;

        // Force immediate movement
        agent.ResetPath();
        agent.SetDestination(target);
        agent.updatePosition = true;
        agent.updateRotation = true;

        isInitialized = true;

        // Set walking animation
        if (animator != null)
            animator.SetBool("IsWalking", true);
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
        if (!isInitialized || isDead || agent == null) return;

        if (animator != null)
            animator.SetBool("IsWalking", agent.velocity.sqrMagnitude > 0.1f);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance)
        {
            StopMoving();
        }

        if (!agent.pathPending && agent.pathStatus != NavMeshPathStatus.PathComplete)
        {
            Debug.LogError($"Enemy '{name}': No valid path to target!");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        Defender defender = other.GetComponent<Defender>() ?? other.GetComponentInParent<Defender>();
        if (defender != null)
        {
            GetComponent<Enemy>().EngageDefender(defender);
        }
    }

    public void MoveToDefender(Defender defender)
    {
        if (agent == null || defender == null) return;

        agent.stoppingDistance = 1f;
        agent.isStopped = false;
        agent.SetDestination(defender.transform.position);
    }
}