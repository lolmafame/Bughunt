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

    [Header("Threat Escalation")]
    public SphereCollider detectionSphere; // drag Spider's own SphereCollider here
    public float calmSpeed = 5f;
    public float calmDetectionRadius = 40f;
    public float alertSpeed = 7f;
    public float alertDetectionRadius = 55f;
    public float maxSpeed = 9f;
    public float maxDetectionRadius = 70f;

    [Header("Terminal Investigation")]
    private bool isInvestigating = false;
    private Transform investigateTargetTransform;

    private float gameTime = 0f;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (detectionSphere == null) detectionSphere = GetComponent<SphereCollider>();
        timer = patrolTimer;
        SetNewPatrolDestination();
    }

    void Update()
    {
        gameTime += Time.deltaTime;
        UpdateThreatLevel();

        // ---------------- Investigation Mode ----------------
        if (isInvestigating && investigateTargetTransform != null)
        {
            agent.SetDestination(investigateTargetTransform.position);
            return;
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

    void UpdateThreatLevel()
    {
        float minutes = gameTime / 60f;

        float targetSpeed;
        float targetRadius;

        if (minutes < 3f)
        {
            targetSpeed = calmSpeed;
            targetRadius = calmDetectionRadius;
        }
        else if (minutes < 6f)
        {
            targetSpeed = alertSpeed;
            targetRadius = alertDetectionRadius;
        }
        else
        {
            targetSpeed = maxSpeed;
            targetRadius = maxDetectionRadius;
        }

        // Smoothly grow detection radius
        if (detectionSphere != null)
            detectionSphere.radius = Mathf.Lerp(detectionSphere.radius, targetRadius, Time.deltaTime * 0.5f);

        // Update chase speed
        chaseSpeed = targetSpeed;

        // If currently chasing, apply new speed immediately
        if (isChasing)
            agent.speed = chaseSpeed;
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
        if (player == null)
        {
            isChasing = false;
            agent.speed = 3.5f;
            SetNewPatrolDestination();
        }
    }
}