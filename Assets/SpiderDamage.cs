using UnityEngine;
using System.Collections;

public class SpiderDamage : MonoBehaviour
{
    public int damageAmount = 20;
    public float knockbackForce = 5f;
    public float knockbackDuration = 0.3f;

    private Animator animator;

    void Start()
    {
        animator = GetComponentInParent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            if (animator != null)
                animator.SetTrigger("Attack");

            PlayerHealth ph = other.GetComponent<PlayerHealth>();
            if (ph != null)
            {
                ph.TakeDamage(damageAmount);
            }

            // --- Knockback via CharacterController ---
            CharacterController cc = other.GetComponent<CharacterController>();
            if (cc != null)
            {
                Vector3 knockbackDir = (other.transform.position - transform.position).normalized;
                knockbackDir.y = 0f;
                knockbackDir.Normalize();

                MonoBehaviour playerMono = other.GetComponent<MonoBehaviour>();
                if (playerMono != null)
                    playerMono.StartCoroutine(ApplyKnockback(cc, knockbackDir));
            }
        }
    }

    private IEnumerator ApplyKnockback(CharacterController cc, Vector3 direction)
    {
        float elapsed = 0f;
        while (elapsed < knockbackDuration)
        {
            cc.Move(direction * knockbackForce * Time.deltaTime);
            elapsed += Time.deltaTime;
            yield return null;
        }
    }
}