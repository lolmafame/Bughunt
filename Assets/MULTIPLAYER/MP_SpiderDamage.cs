using UnityEngine;
using Unity.Netcode;

public class MP_SpiderDamage : NetworkBehaviour
{
    public int damageAmount = 20;
    private Animator animator;

    void Start()
    {
        animator = GetComponentInParent<Animator>();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!IsServer) return; // only server handles damage

        if (other.CompareTag("Player"))
        {
            if (animator != null)
                animator.SetTrigger("Attack");

            MP_PlayerHealth ph = other.GetComponent<MP_PlayerHealth>();
            if (ph != null)
                ph.TakeDamage(damageAmount);
        }
    }
}