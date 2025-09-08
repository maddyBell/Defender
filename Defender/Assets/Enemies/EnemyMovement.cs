using System.Collections;
using UnityEngine;
using UnityEngine.AI;

[RequireComponent(typeof(NavMeshAgent))]
public class EnemyMovement : MonoBehaviour
{
    private NavMeshAgent agent;
    private Animator animator;
    private Vector3 target;
    private bool isDead = false;

    [Header("Settings")]
    public float stopDistanceFromTower = 2f;      
    public float deathAnimationDuration = 2f;     
         private Transform towerTarget;   
    private Transform currentTarget; 

    public float stopDistanceFromTarget = 1.5f; 
    public float attackInterval = 2f;           
    public int defenderDamage = 5;             

    private Coroutine attackRoutine;
    private Coroutine defenderRoutine;
       public void Initialize(Vector3 castlePos, float moveSpeed)
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        target = castlePos;

        if (agent == null) return;

        agent.speed = moveSpeed;
        agent.stoppingDistance = stopDistanceFromTower;

    
        if (!agent.isOnNavMesh)
        {
            NavMeshHit hit;
            if (NavMesh.SamplePosition(transform.position, out hit, 2f, NavMesh.AllAreas))
            {
                agent.Warp(hit.position);
            }
            else
            {
                Debug.LogWarning($"{name} could not find a valid NavMesh position at spawn.");
            }
        }

        agent.isStopped = false;
        agent.SetDestination(target);
    }

  void Awake()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
    }

    public void SetTarget(Transform tower)
    {
        towerTarget = tower;
        currentTarget = towerTarget; 
        agent.SetDestination(currentTarget.position);
    }

    void Update()
    {
        if (isDead || currentTarget == null) return;

        agent.SetDestination(currentTarget.position);

        if (agent.remainingDistance > stopDistanceFromTarget)
        {
            animator.Play("Walk");
            if (attackRoutine != null)
            {
                StopCoroutine(attackRoutine);
                attackRoutine = null;
            }
        }
        else
        {
            animator.Play("Idle");
            if (attackRoutine == null)
                attackRoutine = StartCoroutine(AttackTarget());
        }
    }

    public void Die()
    {
        if (isDead) return;
        isDead = true;

        agent.isStopped = true;
        animator.Play("Loose");
        Destroy(gameObject, 2f); 
    }

 
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Defender"))
        {
            if (defenderRoutine != null) StopCoroutine(defenderRoutine);
            defenderRoutine = StartCoroutine(FocusOnDefender(other.transform));
        }
    }

    private IEnumerator FocusOnDefender(Transform defender)
    {
        currentTarget = defender;

        float duration = Random.Range(10f, 15f);
        float elapsed = 0f;

        while (elapsed < duration && defender != null)
        {
            elapsed += Time.deltaTime;
            yield return null;
        }

        currentTarget = towerTarget;
    }


    private IEnumerator AttackTarget()
    {
        while (!isDead && currentTarget != null)
        {
            
            if (currentTarget.CompareTag("Defender"))
            {
                Defender def = currentTarget.GetComponent<Defender>();
                if (def != null) def.TakeDamage(defenderDamage);
            }


            yield return new WaitForSeconds(attackInterval);
        }
    }
}