using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    public CutsceneManager cutsceneManager;
    public string playerTag = "Player";

    private bool hasTriggered = false;

    private void OnTriggerEnter(Collider other)
    {
        if (hasTriggered) return;
        if (!other.CompareTag(playerTag)) return;

        // Only fire if all terminals are completed
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
    }
}