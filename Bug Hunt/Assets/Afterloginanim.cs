using UnityEngine;

public class Afterloginanim : MonoBehaviour
{
    [SerializeField] private Animator animator;

    public void PlayAnimation()
    {
        animator.SetTrigger("PlayAnim");
    }
}
