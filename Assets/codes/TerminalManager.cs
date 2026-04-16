using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TerminalManager : MonoBehaviour
{
    public static TerminalManager Instance;

    [Header("Terminal Count")]
    public int totalTerminals = 5;
    private int completedTerminals = 0;
    public Text terminalText;

    [Header("Final Door (opens when ALL terminals done)")]
    public GameObject[] doorObjects;

    [Header("Completion Message")]
    public TMPro.TextMeshProUGUI completionMessageText;
    public float fadeDuration = 1.5f;
    public float displayDuration = 3f;

    void Awake()
    {
        Instance = this;
        UpdateUI();

        // Hide message at start
        if (completionMessageText != null)
        {
            completionMessageText.transform.parent.gameObject.SetActive(true);
            Color c = completionMessageText.color;
            completionMessageText.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    public void TerminalCompleted()
    {
        completedTerminals++;
        UpdateUI();

        Debug.Log("Terminals completed: " + completedTerminals + " / " + totalTerminals);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayTerminalComplete();

        // 🔥 ALL TERMINALS FINISHED
        if (completedTerminals >= totalTerminals)
        {
            Debug.Log("ALL TERMINALS DONE!");

            // Remove doors
            foreach (GameObject door in doorObjects)
            {
                if (door != null)
                    door.SetActive(false);
            }

            // Show completion message
            if (completionMessageText != null)
                StartCoroutine(FadeMessage());

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayAllTerminalsDone();
        }
    }

    private IEnumerator FadeMessage()
    {
        float elapsed = 0f;
        Color c = completionMessageText.color;

        // Fade in
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            completionMessageText.color =
                new Color(c.r, c.g, c.b, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            completionMessageText.color =
                new Color(c.r, c.g, c.b, Mathf.Clamp01(1f - elapsed / fadeDuration));
            yield return null;
        }

        completionMessageText.color = new Color(c.r, c.g, c.b, 0f);
    }

    void UpdateUI()
    {
        if (terminalText != null)
            terminalText.text = completedTerminals + " / " + totalTerminals + " Terminals";
    }

    // ⚠️ OTHER SCRIPTS NEED THIS
    public int GetCompletedTerminals()
    {
        return completedTerminals;
    }
}