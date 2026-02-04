using UnityEngine;
using System.Collections;

public class regAnim : MonoBehaviour
{
    public Animator animator;
    public GameObject textObject;
    public GameObject regObject;

    public float animationDuration = 60f;
    public float textDuration = 1f;

    void Start()
    {
        Debug.Log("regAnim Start. textObject=" + (textObject != null) + ", regObject=" + (regObject != null));
        textObject.SetActive(false);
        StartCoroutine(AnimationSequence());
    }

    IEnumerator AnimationSequence()
    {
        animator.speed = 1f;
        yield return new WaitForSeconds(animationDuration);

        animator.speed = 0f;
        textObject.SetActive(true);
        yield return new WaitForSeconds(textDuration);

        textObject.SetActive(false);

        animator.speed = 1.1f;
        yield return new WaitForSeconds(animationDuration);

        animator.speed = 0f;
        regObject.SetActive(true);
        Debug.Log("regAnim completed. regObject activated: " + (regObject != null ? regObject.name : "null"));
    }
}