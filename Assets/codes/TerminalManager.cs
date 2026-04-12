using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class TerminalManager : MonoBehaviour
{
    public static TerminalManager Instance;
    public int totalTerminals = 5;
    private int completedTerminals = 0;
    public Text terminalText;

    [Header("Door")]
    public GameObject doorObject;

    [Header("Completion Message")]
    public TMPro.TextMeshProUGUI completionMessageText;
    public float fadeDuration = 1.5f;
    public float displayDuration = 3f;

    void Awake()
    {
        Instance = this;
        UpdateUI();
        if (completionMessageText != null)
        {
            Color c = completionMessageText.color;
            completionMessageText.color = new Color(c.r, c.g, c.b, 0f);
        }
    }

    public void TerminalCompleted()
    {
        completedTerminals++;
        UpdateUI();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayTerminalComplete();
        else
            Debug.LogWarning("TerminalManager: SoundManager instance is missing!");

        if (completedTerminals >= totalTerminals)
        {
            Debug.Log("ALL TERMINALS COMPLETED!");

            if (doorObject != null)
                doorObject.SetActive(false);
            else
                Debug.LogWarning("TerminalManager: Door object is not assigned!");

            if (completionMessageText != null)
                StartCoroutine(FadeMessage());
            else
                Debug.LogWarning("TerminalManager: Completion message Text is not assigned!");

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
            completionMessageText.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(elapsed / fadeDuration));
            yield return null;
        }

        yield return new WaitForSeconds(displayDuration);

        // Fade out
        elapsed = 0f;
        while (elapsed < fadeDuration)
        {
            elapsed += Time.deltaTime;
            completionMessageText.color = new Color(c.r, c.g, c.b, Mathf.Clamp01(1f - (elapsed / fadeDuration)));
            yield return null;
        }

        completionMessageText.color = new Color(c.r, c.g, c.b, 0f);
    }

    public int GetCompleted() => completedTerminals;
    public int GetCompletedTerminals() => completedTerminals;

    void UpdateUI()
    {
        if (terminalText != null)
            terminalText.text = completedTerminals + " / " + totalTerminals + " Terminals";
    }
}