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
        SoundManager.Instance.PlayTerminalComplete(); // ← single terminal done

        if (completedTerminals >= totalTerminals)
        {
            Debug.Log("ALL TERMINALS COMPLETED!");
            SoundManager.Instance.PlayAllTerminalsDone(); // ← all terminals done
            GameManager.Instance.Completion();
            // Level complete later
        }
    }
    public int GetCompleted()
    {
        return completedTerminals;
    }
    void UpdateUI()
    {
        terminalText.text = completedTerminals + " / " + totalTerminals + " Terminals";
    }

    public int GetCompletedTerminals()
    {
        return completedTerminals;
    }
}
