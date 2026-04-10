using UnityEngine;
using UnityEngine.AI;

public class SpiderAI : MonoBehaviour
{
    [Header("Patrol Settings")]
    public NavMeshAgent agent;
    public float patrolWaitTime = 1f;
    public float patrolSpeed = 3.5f;

    [Header("Chase Settings")]
    public float chaseSpeed = 5f;
    private bool isChasing = false;
    private Transform player;

    [Header("Threat Escalation")]
    public SphereCollider detectionSphere;
    public float calmSpeed = 5f;
    public float calmDetectionRadius = 40f;
    public float alertSpeed = 7f;
    public float alertDetectionRadius = 55f;
    public float maxSpeed = 9f;
    public float maxDetectionRadius = 70f;

    [Header("Investigation Settings")]
    public float baseSearchDuration = 5f;
    public float searchDurationIncrement = 3f;
    public float maxSearchDuration = 20f;

    [Header("Roaming")]
    public float stuckThreshold = 2f;
    public float stuckDistanceLimit = 1f;

    // ---- Private State ----
    private enum SpiderState { Patrol, Investigating, Searching, Chasing }
    private SpiderState state = SpiderState.Patrol;

    private Terminal[] allTerminals;
    private int currentPatrolIndex = 0;
    private float patrolWaitTimer = 0f;
    private bool isWaitingAtTerminal = false;

    private Transform investigateTarget;
    private float searchTimer = 0f;
    private float currentSearchDuration;
    private int suspicionLevel = 0;

    private float gameTime = 0f;

    private float stuckTimer = 0f;
    private Vector3 lastPosition;

    void Start()
    {
        if (agent == null) agent = GetComponent<NavMeshAgent>();
        if (detectionSphere == null) detectionSphere = GetComponent<SphereCollider>();

        allTerminals = FindObjectsByType<Terminal>(FindObjectsSortMode.None);

        if (allTerminals.Length == 0)
        {
            Debug.LogWarning("SpiderAI: No terminals found in scene!");
            return;
        }

        ShuffleTerminals();

        currentSearchDuration = baseSearchDuration;
        lastPosition = transform.position;
        agent.speed = patrolSpeed;

        GoToNextPatrolTerminal();
    }

    void Update()
    {
        gameTime += Time.deltaTime;
        UpdateThreatLevel();
        CheckIfStuck();

        switch (state)
        {
            case SpiderState.Patrol:
                HandlePatrol();
                break;
            case SpiderState.Investigating:
                HandleInvestigating();
                break;
            case SpiderState.Searching:
                HandleSearching();
                break;
            case SpiderState.Chasing:
                HandleChasing();
                break;
        }
    }

    // ------------------------------------------------
    // PATROL
    // ------------------------------------------------
    void HandlePatrol()
    {
        if (allTerminals.Length == 0) return;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            if (!isWaitingAtTerminal)
            {
                isWaitingAtTerminal = true;
                patrolWaitTimer = patrolWaitTime;
            }

            patrolWaitTimer -= Time.deltaTime;

            if (patrolWaitTimer <= 0f)
            {
                isWaitingAtTerminal = false;
                GoToNextPatrolTerminal();
            }
        }
    }

    void GoToNextPatrolTerminal()
    {
        if (allTerminals.Length == 0) return;

        int attempts = 0;
        while (allTerminals[currentPatrolIndex].isCompleted && attempts < allTerminals.Length)
        {
            currentPatrolIndex = (currentPatrolIndex + 1) % allTerminals.Length;
            attempts++;
        }

        agent.speed = patrolSpeed;
        agent.SetDestination(allTerminals[currentPatrolIndex].transform.position);
        currentPatrolIndex = (currentPatrolIndex + 1) % allTerminals.Length;
    }

    // ------------------------------------------------
    // INVESTIGATION
    // ------------------------------------------------
    public void InvestigateTerminal(Transform terminalTransform)
    {
        suspicionLevel++;
        currentSearchDuration = Mathf.Min(
            baseSearchDuration + (searchDurationIncrement * suspicionLevel),
            maxSearchDuration
        );

        investigateTarget = terminalTransform;
        state = SpiderState.Investigating;
        agent.speed = chaseSpeed;
        agent.SetDestination(investigateTarget.position);

        Debug.Log("Spider investigating terminal. Suspicion level: " + suspicionLevel);
    }

    void HandleInvestigating()
    {
        if (investigateTarget == null)
        {
            ResumePatrol();
            return;
        }

        agent.SetDestination(investigateTarget.position);

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.5f)
        {
            Debug.Log("Spider arrived at terminal, searching for " + currentSearchDuration + " seconds");
            searchTimer = currentSearchDuration;
            state = SpiderState.Searching;
        }
    }

    // ------------------------------------------------
    // SEARCHING
    // ------------------------------------------------
    void HandleSearching()
    {
        searchTimer -= Time.deltaTime;

        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            Vector3 randomDir = Random.insideUnitSphere * 8f + transform.position;
            NavMeshHit hit;
            if (NavMesh.SamplePosition(randomDir, out hit, 8f, NavMesh.AllAreas))
                agent.SetDestination(hit.position);
        }

        if (searchTimer <= 0f)
        {
            Debug.Log("Spider finished searching, resuming patrol");
            ResumePatrol();
        }
    }

    // ------------------------------------------------
    // CHASING
    // ------------------------------------------------
    void HandleChasing()
    {
        if (player != null)
            agent.SetDestination(player.position);
    }

    // ------------------------------------------------
    // DETECTION TRIGGER
    // ------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = other.transform;
            isChasing = true;
            state = SpiderState.Chasing;
            agent.speed = chaseSpeed;

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySpiderDetected();
            else
                Debug.LogWarning("SpiderAI: SoundManager instance is missing!");

            Debug.Log("Spider detected player!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            player = null;
            isChasing = false;

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlaySpiderLost();
            else
                Debug.LogWarning("SpiderAI: SoundManager instance is missing!");

            if (investigateTarget != null)
            {
                state = SpiderState.Searching;
                searchTimer = currentSearchDuration;
            }
            else
            {
                ResumePatrol();
            }
        }
    }

    // ------------------------------------------------
    // UTILITIES
    // ------------------------------------------------
    void ResumePatrol()
    {
        state = SpiderState.Patrol;
        investigateTarget = null;
        agent.speed = patrolSpeed;
        GoToNextPatrolTerminal();
    }

    void UpdateThreatLevel()
    {
        float minutes = gameTime / 60f;
        float targetRadius;

        if (minutes < 3f)
        {
            chaseSpeed = calmSpeed;
            targetRadius = calmDetectionRadius;
        }
        else if (minutes < 6f)
        {
            chaseSpeed = alertSpeed;
            targetRadius = alertDetectionRadius;
        }
        else
        {
            chaseSpeed = maxSpeed;
            targetRadius = maxDetectionRadius;
        }

        if (detectionSphere != null)
            detectionSphere.radius = Mathf.Lerp(detectionSphere.radius, targetRadius, Time.deltaTime * 0.5f);
    }

    void CheckIfStuck()
    {
        if (state == SpiderState.Chasing) return;

        stuckTimer += Time.deltaTime;
        if (stuckTimer >= stuckThreshold)
        {
            float distanceMoved = Vector3.Distance(transform.position, lastPosition);
            if (distanceMoved < stuckDistanceLimit)
            {
                GoToNextPatrolTerminal();
            }
            lastPosition = transform.position;
            stuckTimer = 0f;
        }
    }

    void ShuffleTerminals()
    {
        for (int i = allTerminals.Length - 1; i > 0; i--)
        {
            int rand = Random.Range(0, i + 1);
            Terminal temp = allTerminals[i];
            allTerminals[i] = allTerminals[rand];
            allTerminals[rand] = temp;
        }
    }

    public void ForceInvestigate(Transform terminalTransform)
    {
        InvestigateTerminal(terminalTransform);
    }

    public void StopInvestigate()
    {
        if (state == SpiderState.Investigating)
        {
            searchTimer = currentSearchDuration;
            state = SpiderState.Searching;
        }
    }
}