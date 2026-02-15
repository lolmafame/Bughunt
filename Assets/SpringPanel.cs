using UnityEngine;
using System.Collections;

public class SpringPanel : MonoBehaviour
{
    public RectTransform panel;

    [Header("Drop Settings")]
    public float dropDistance = 900f;
    public float dropDuration = 0.5f;

    [Header("Bounce Settings")]
    public float bounceHeight = 60f;
    public float bounceDuration = 0.25f;
    public int bounceCount = 2;

    private Vector2 finalPos;

    void Awake()
    {
        finalPos = panel.anchoredPosition;
    }

    public void PlayDropBounce()
    {
        StopAllCoroutines();
        StartCoroutine(DropBounceAnimation());
    }

    IEnumerator DropBounceAnimation()
    {
        Vector2 startPos = finalPos + new Vector2(0, dropDistance);
        panel.anchoredPosition = startPos;

        float time = 0f;

        // ---------- DROP ----------
        while (time < dropDuration)
        {
            time += Time.unscaledDeltaTime;
            float t = time / dropDuration;

            // Ease-in fall
            t = t * t;

            panel.anchoredPosition = Vector2.Lerp(startPos, finalPos, t);

            yield return null;
        }

        panel.anchoredPosition = finalPos;

        // ---------- BOUNCE ----------
        for (int i = 0; i < bounceCount; i++)
        {
            yield return StartCoroutine(Bounce(finalPos, bounceHeight * (1f / (i + 1))));
        }
    }

    IEnumerator Bounce(Vector2 basePos, float height)
    {
        float time = 0f;

        // Bounce Up
        while (time < bounceDuration / 2f)
        {
            time += Time.unscaledDeltaTime;
            float t = time / (bounceDuration / 2f);

            panel.anchoredPosition = basePos + new Vector2(0, Mathf.Lerp(0, height, t));
            yield return null;
        }

        time = 0f;

        // Bounce Down
        while (time < bounceDuration / 2f)
        {
            time += Time.unscaledDeltaTime;
            float t = time / (bounceDuration / 2f);

            panel.anchoredPosition = basePos + new Vector2(0, Mathf.Lerp(height, 0, t));
            yield return null;
        }

        panel.anchoredPosition = basePos;
    }
}