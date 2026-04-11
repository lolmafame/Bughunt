using UnityEngine;
using UnityEngine.AI;
using Unity.Netcode;
using System.Collections.Generic;

public class MP_SpiderAI : NetworkBehaviour
{
    [Header("Patrol Settings")]
    public NavMeshAgent agent;
    public float patrolWaitTime = 1f;        // how long it waits at each terminal
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
    public float baseSearchDuration = 5f;    // how long it searches after arriving
    public float searchDurationIncrement = 3f; // adds this much per trigger
    public float maxSearchDuration = 20f;

    [Header("Roaming")]
    public float stuckThreshold = 2f;
    public float stuckDistanceLimit = 1f;

    // ---- Private State ----
    private enum SpiderState { Patrol, Investigating, Searching, Chasing }
    private SpiderState state = SpiderState.Patrol;
    public static MP_SpiderAI Instance;

    private MP_Terminal[] allTerminals;         // all terminals in scene
    private int currentPatrolIndex = 0;
    private float patrolWaitTimer = 0f;
    private bool isWaitingAtTerminal = false;

    private Transform investigateTarget;     // terminal position being investigated
    private float searchTimer = 0f;
    private float currentSearchDuration;
    private int suspicionLevel = 0;          // increases each trigger

    private float gameTime = 0f;

    private float stuckTimer = 0f;
    private Vector3 lastPosition;
    private List<Transform> playersInRange = new List<Transform>();
    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        Instance = this;

        agent = GetComponent<NavMeshAgent>();
        detectionSphere = GetComponent<SphereCollider>();

        allTerminals = FindObjectsByType<MP_Terminal>(FindObjectsSortMode.None);

        ShuffleTerminals();
        currentSearchDuration = baseSearchDuration;

        lastPosition = transform.position;
        agent.speed = patrolSpeed;

        GoToNextPatrolTerminal();
    }

    void Update()
    {
        if (!IsServer) return; // 🔥 THIS LINE FIXES YOUR WHOLE PROBLEM

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

    [ServerRpc(RequireOwnership = false)]
    public void InvestigateServerRpc(Vector3 position)
    {
        InvestigatePosition(position);
    }

    public void InvestigatePosition(Vector3 pos)
    {
        if (!IsServer) return;

        investigateTarget = null;
        state = SpiderState.Investigating;
        agent.speed = chaseSpeed;
        agent.SetDestination(pos);
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

        while (attempts < allTerminals.Length)
        {
            var terminal = allTerminals[currentPatrolIndex];

            if (!terminal.isCompleted.Value)
                break;

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
        // Increase suspicion each time triggered
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

        // Arrived at terminal
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

        // Wander near terminal while searching
        if (!agent.pathPending && agent.remainingDistance <= agent.stoppingDistance + 0.2f)
        {
            // Pick a small random nearby point to wander
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

    [ServerRpc(RequireOwnership = false)]
    public void StopInvestigateServerRpc()
    {
        StopInvestigate();
    }

    // ------------------------------------------------
    // CHASING
    // ------------------------------------------------
    void HandleChasing()
    {
        // Always chase nearest player
        if (playersInRange.Count > 0)
        {
            Transform nearest = null;
            float minDist = float.MaxValue;
            foreach (Transform p in playersInRange)
            {
                if (p == null) continue;
                float dist = Vector3.Distance(transform.position, p.position);
                if (dist < minDist)
                {
                    minDist = dist;
                    nearest = p;
                }
            }
            player = nearest;
            if (player != null)
                agent.SetDestination(player.position);
        }
    }

    // ------------------------------------------------
    // DETECTION TRIGGER
    // ------------------------------------------------
    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("Player"))
        {
            if (!playersInRange.Contains(other.transform))
                playersInRange.Add(other.transform);
            isChasing = true;
            state = SpiderState.Chasing;
            agent.speed = chaseSpeed;
            Debug.Log("Spider detected player!");
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!IsServer) return;
        if (other.CompareTag("Player"))
        {
            playersInRange.Remove(other.transform);

            if (playersInRange.Count == 0)
            {
                player = null;
                isChasing = false;
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
                // Force move to next terminal
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

            MP_Terminal temp = allTerminals[i];
            allTerminals[i] = allTerminals[rand];
            allTerminals[rand] = temp;
        }
    }

    // Called by ForceInvestigate from CodeTerminalUI
    public void ForceInvestigate(Transform terminalTransform)
    {
        InvestigateTerminal(terminalTransform);
    }

    public void StopInvestigate()
    {
        // Don't stop immediately — let search duration run
        // Only stop if currently still travelling to terminal
        if (state == SpiderState.Investigating)
        {
            searchTimer = currentSearchDuration;
            state = SpiderState.Searching;
        }
    }
}