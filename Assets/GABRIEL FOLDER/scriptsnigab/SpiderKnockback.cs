using UnityEngine;

public class SpiderKnockback : MonoBehaviour
{
    [Header("Knockback Settings")]
    public float knockbackForce = 10f;
    public float knockbackDuration = 0.2f;
    public float upwardForce = 3f;

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            ApplyKnockback(other.gameObject);
        }
    }

    // If you're using 2D colliders, use this instead:
    // private void OnTriggerEnter2D(Collider2D other)
    // {
    //     if (other.CompareTag("Player"))
    //         ApplyKnockback(other.gameObject);
    // }

    private void ApplyKnockback(GameObject player)
    {
        Rigidbody rb = player.GetComponent<Rigidbody>();
        // For 2D: Rigidbody2D rb = player.GetComponent<Rigidbody2D>();

        if (rb == null) return;

        // Get direction from spider to player
        Vector3 knockbackDir = (player.transform.position - transform.position).normalized;

        // Zero out current velocity before applying force (optional but cleaner)
        rb.linearVelocity = Vector3.zero;

        // Apply knockback force + upward force
        rb.AddForce(new Vector3(
            knockbackDir.x * knockbackForce,
            upwardForce,
            knockbackDir.z * knockbackForce
        ), ForceMode.Impulse);
    }
}