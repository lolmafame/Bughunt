using UnityEngine;
using UnityEngine.AI;

public class SpiderAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public NavMeshAgent agent;
    public float patrolRadius = 20f;
    public float patrolTimer = 5f;
    private float timer;

    [Header("Chase Settings")]
    public float chaseSpeed = 5f;
    private bool isChasing = false;
    private Transform player;

    [Header("Terminal Investigation")]
    private bool isInvestigating = false;
    private Transform investigateTargetTransform;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        timer = patrolTimer;
        SetNewPatrolDestination();
    }

    void Update()
    {
        // ---------------- Investigation Mode ----------------
        if (isInvestigating && investigateTargetTransform != null)
        {
            agent.SetDestination(investigateTargetTransform.position);
            return; // skip patrol/chase logic
        }

        // ---------------- Chasing Mode ----------------
        if (isChasing && player != null)
        {
            agent.SetDestination(player.position);
        }
        else
        {
            // ---------------- Patrol Mode ----------------
            timer += Time.deltaTime;
            if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
                SetNewPatrolDestination();

            if (timer >= patrolTimer)
            {
                SetNewPatrolDestination();
                timer = 0f;
            }
        }
    }

    void SetNewPatrolDestination()
    {
        for (int i = 0; i < 10; i++)
        {
            Vector3 randomDir = Random.insideUnitSphere * patrolRadius + transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, patrolRadius, NavMesh.AllAreas))
            {
                agent.SetDestination(hit.position);
                return;
            }
        }
    }

    // ---------------- Detection ----------------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            isChasing = true;
            agent.speed = chaseSpeed;

            // Damage handled in PlayerHealth or separate script
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = null;
            isChasing = false;
            agent.speed = 3.5f;
            SetNewPatrolDestination();
        }
    }

    // ---------------- Terminal Investigation ----------------
    public void ForceInvestigate(Transform targetTransform)
    {
        isInvestigating = true;
        investigateTargetTransform = targetTransform;
        isChasing = true;
        agent.speed = chaseSpeed;
    }

    public void StopInvestigate()
    {
        isInvestigating = false;
        investigateTargetTransform = null;

        // Only resume patrol if player is not currently in detection trigger
        if (player == null)
        {
            isChasing = false;
            agent.speed = 3.5f;
            SetNewPatrolDestination();
        }
    }
}
