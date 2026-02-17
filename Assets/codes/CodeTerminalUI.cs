using UnityEngine;
using UnityEngine.UI;
using TMPro;


public class CodeTerminalUI : MonoBehaviour
{
    public static CodeTerminalUI Instance;

    public GameObject codePanel;
    public InputField inputField;
    public TransitionFlow transitionFlow;
    public ProcessTransition processFlow;

    private Terminal currentTerminal;
    private bool isActive = false;


  
    public TMP_Text instructionText; // Assign in Inspector

void Awake()
    {
        Instance = this;
        codePanel.SetActive(false);
    }

    public void Open(Terminal terminal)
    {
        currentTerminal = terminal;
        codePanel.SetActive(true);
        transitionFlow.PlayTransition();
        inputField.text = "";
        isActive = true;

        // ===== ADDED: Show instruction =====
        if (instructionText != null)
            instructionText.text = terminal.instructions;

        GameManager.Instance.SetInputLocked(true);
        ThirdPersonMovement playerMove =
            GameObject.FindGameObjectWithTag("Player")
            .GetComponent<ThirdPersonMovement>();
        playerMove.enabled = false;
        SpiderAI spider = FindObjectOfType<SpiderAI>();
        if (spider != null)
        {
            spider.ForceInvestigate(playerMove.transform);
        }
    }

    public void Close()
    {
        codePanel.SetActive(false);
        isActive = false;

        // Unlock player movement
        ThirdPersonMovement playerMove = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonMovement>();
        playerMove.enabled = true;
        GameManager.Instance.SetInputLocked(false);


        // Stop spider investigation
        SpiderAI spider = FindObjectOfType<SpiderAI>();
        if (spider != null)
        {
            spider.StopInvestigate();
        }
    }


    public void Submit()
    {
        if (currentTerminal == null) return;

        if (inputField.text == currentTerminal.correctAnswer)
        {
            processFlow.PlaySuccess(() =>
            {
                currentTerminal.CompleteTerminal();
                Close();
            });
        }
        else
        {
            processFlow.PlayFail(() =>
            {
                codePanel.SetActive(true);
            });
        }
    }


    void Update()
    {
        if (!isActive) return;

        // TAB closes terminal
        if (Input.GetKeyDown(KeyCode.Tab))
        {
            Close();
        }
    }

    public bool IsActive()
    {
        return isActive;
    }
}

