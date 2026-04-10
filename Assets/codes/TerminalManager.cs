using UnityEngine;
using UnityEngine.UI;

public class TerminalManager : MonoBehaviour
{
    public static TerminalManager Instance;
    public int totalTerminals = 5;
    private int completedTerminals = 0;
    public Text terminalText;

    void Awake()
    {
        Instance = this;
        UpdateUI();
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

            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayAllTerminalsDone();

            if (GameManager.Instance != null)
                GameManager.Instance.Completion();
            else
                Debug.LogWarning("TerminalManager: GameManager instance is missing!");
        }
    }

    public int GetCompleted() => completedTerminals;
    public int GetCompletedTerminals() => completedTerminals;

    void UpdateUI()
    {
        if (terminalText != null)
            terminalText.text = completedTerminals + " / " + totalTerminals + " Terminals";
    }
}