using UnityEngine;

public class ContinueIconBlink : MonoBehaviour
{
    public float blinkSpeed = 0.5f;

    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
    }

    void Update()
    {
        if (!gameObject.activeSelf) return;

        float alpha = Mathf.PingPong(Time.time / blinkSpeed, 1f);
        canvasGroup.alpha = alpha;
    }
}
