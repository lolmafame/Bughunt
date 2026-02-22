using UnityEngine;

public class SpiderDamage : MonoBehaviour
{
    public int damageAmount = 20;
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
        }
    }
}