using UnityEngine;
using System.Collections;

public class panelExit : MonoBehaviour
{
    [Header("Panel Settings")]
    [SerializeField] private RectTransform panel;
    [SerializeField] private float springDistance = 50f; // how much it dips down first
    [SerializeField] private float exitDistance = 600f;  // how far it moves up
    [SerializeField] private float springDuration = 0.2f;
    [SerializeField] private float exitDuration = 0.5f;

    [Header("Objects to Activate")]
    [SerializeField] private GameObject objectToActivate1;
    [SerializeField] private GameObject objectToActivate2;

    private Vector2 startPos;

    void Awake()
    {
        startPos = panel.anchoredPosition;
    }

    public void StartPanelExit()
    {
        StartCoroutine(SpringAndExit());
    }

    private IEnumerator SpringAndExit()
    {
        Vector2 dipPos = startPos - new Vector2(0, springDistance);
        float time = 0f;

        while (time < springDuration)
        {
            time += Time.deltaTime;
            float t = time / springDuration;
            t = t * t * (3f - 2f * t); 
            panel.anchoredPosition = Vector2.Lerp(startPos, dipPos, t);
            yield return null;
        }

        panel.anchoredPosition = dipPos;

        Vector2 targetPos = startPos + new Vector2(0, exitDistance);
        time = 0f;

        while (time < exitDuration)
        {
            time += Time.deltaTime;
            float t = time / exitDuration;
            t = Mathf.Sin(t * Mathf.PI * 0.5f); 
            panel.anchoredPosition = Vector2.Lerp(dipPos, targetPos, t);
            yield return null;
        }

        panel.anchoredPosition = targetPos;

        panel.gameObject.SetActive(false);

        if (objectToActivate1 != null) objectToActivate1.SetActive(true);
        if (objectToActivate2 != null) objectToActivate2.SetActive(true);
    }
}