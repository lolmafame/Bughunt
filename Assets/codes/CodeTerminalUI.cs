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

    public TMP_Text instructionText;

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

        if (instructionText != null)
            instructionText.text = terminal.instructions;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        inputField.Select();
        inputField.ActivateInputField();

        GameManager.Instance.SetInputLocked(true);

        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) { Debug.LogError("Player not found!"); return; }

        ThirdPersonMovement playerMove = playerObj.GetComponent<ThirdPersonMovement>();
        if (playerMove == null) { Debug.LogError("ThirdPersonMovement not found!"); return; }

        playerMove.enabled = false;

        SpiderAI spider = FindAnyObjectByType<SpiderAI>();
        if (spider != null)
            spider.ForceInvestigate(terminal.transform); // CHANGED: terminal not player
        SoundManager.Instance.PlayTerminalOpen(); // ← add here
    }

    public void Close()
    {
        codePanel.SetActive(false);
        isActive = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        ThirdPersonMovement playerMove = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonMovement>();
        playerMove.enabled = true;
        GameManager.Instance.SetInputLocked(false);

        SpiderAI spider = FindAnyObjectByType<SpiderAI>();
        if (spider != null)
            spider.StopInvestigate();
        SoundManager.Instance.PlayTerminalClose(); // ← add here
    }

    public void Submit()
    {
        if (currentTerminal == null) return;

        if (inputField.text == currentTerminal.correctAnswer)
        {
            SoundManager.Instance.PlayTerminalCorrect(); // ← add here
            processFlow.PlaySuccess(() =>
            {
                currentTerminal.CompleteTerminal();
                Close();
            });
        }
        else
        {
            SoundManager.Instance.PlayTerminalWrong(); // ← add here
            processFlow.PlayFail(() =>
            {
                codePanel.SetActive(true);
                isActive = true;
                inputField.gameObject.SetActive(true);
                inputField.text = "";
                inputField.Select();
                inputField.ActivateInputField();
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            });
        }
    }

    void Update()
    {
        if (!isActive) return;

        if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Tab) && !Input.GetKeyDown(KeyCode.Return))
            SoundManager.Instance.PlayTerminalTyping(); // ← fires on each keypress

        if (Input.GetKeyDown(KeyCode.Tab))
            Close();
    }

    public bool IsActive()
    {
        return isActive;
    }
}