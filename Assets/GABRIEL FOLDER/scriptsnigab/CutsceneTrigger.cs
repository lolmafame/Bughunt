using System.Collections;
using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    public CutsceneManager cutsceneManager;
    public string playerTag = "Player";

    [Header("Completion Panel")]
    public GameObject completionPanel;
    public float completionDelay = 3f; // Edit this in Inspector

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        if (TerminalManager.Instance != null &&
            TerminalManager.Instance.GetCompletedTerminals() < TerminalManager.Instance.totalTerminals)
        {
            Debug.Log("CutsceneTrigger: Not all terminals completed yet.");
            return;
        }

        hasTriggered = true;

        if (cutsceneManager != null)
            cutsceneManager.PlayCutscene();
        else
            Debug.LogWarning("CutsceneTrigger: CutsceneManager is not assigned!");

        StartCoroutine(ShowCompletionAfterDelay());
    }

    IEnumerator ShowCompletionAfterDelay()
    {
        yield return new WaitForSecondsRealtime(completionDelay);

        if (completionPanel != null)
        {
            completionPanel.SetActive(true);

            // Force it to render on top
            Canvas canvas = completionPanel.GetComponentInParent<Canvas>();
            if (canvas != null)
                canvas.sortingOrder = 1000;
        }

        if (GameManager.Instance != null)
            GameManager.Instance.Completion();
        else
            Debug.LogError("GameManager.Instance is NULL!");
    }
}