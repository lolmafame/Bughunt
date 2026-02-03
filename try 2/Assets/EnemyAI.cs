using UnityEngine;
using UnityEngine.AI;

public class EnemyAI : MonoBehaviour
{
    public NavMeshAgent agent;
    public Transform player;

    public float detectionRadius = 10f;
    public float attackDistance = 2f;

    private bool isChasing = false;

    void Update()
    {
        float distance = Vector3.Distance(transform.position, player.position);

        if (distance <= detectionRadius)
        {
            isChasing = true;
        }
        else
        {
            isChasing = false;
        }

        if (isChasing)
        {
            agent.SetDestination(player.position);

            if (distance <= attackDistance)
            {
                // Player caught!
                Debug.Log("Player Caught!");
                // TODO: Handle game over
            }
        }
        else
        {
            // Idle or patrol logic can go here
            agent.SetDestination(transform.position);
        }
    }

    private void OnDrawGizmosSelected()
    {
        // Visualize detection radius
        Gizmos.color = Color.red;
        Gizmos.DrawWireSphere(transform.position, detectionRadius);
    }
}
