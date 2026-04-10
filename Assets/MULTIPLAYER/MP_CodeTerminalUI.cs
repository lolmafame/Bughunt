using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class MP_CodeTerminalUI : MonoBehaviour
{
    public static MP_CodeTerminalUI Instance;
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
        SoundManager.Instance.PlayTerminalOpen();

        // Find LOCAL player only
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            MP_ThirdPersonMovement playerMove = p.GetComponent<MP_ThirdPersonMovement>();
            if (playerMove != null && playerMove.IsOwner)
            {
                playerMove.enabled = false;
                break;
            }
        }

        SpiderAI spider = FindAnyObjectByType<SpiderAI>();
        if (spider != null)
            spider.ForceInvestigate(terminal.transform);
    }

    public void Close()
    {
        codePanel.SetActive(false);
        isActive = false;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        GameManager.Instance.SetInputLocked(false);
        SoundManager.Instance.PlayTerminalClose();

        // Find LOCAL player only
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");
        foreach (GameObject p in players)
        {
            MP_ThirdPersonMovement playerMove = p.GetComponent<MP_ThirdPersonMovement>();
            if (playerMove != null && playerMove.IsOwner)
            {
                playerMove.enabled = true;
                break;
            }
        }

        SpiderAI spider = FindAnyObjectByType<SpiderAI>();
        if (spider != null)
            spider.StopInvestigate();
    }

    public void Submit()
    {
        if (currentTerminal == null) return;
        if (inputField.text == currentTerminal.correctAnswer)
        {
            SoundManager.Instance.PlayTerminalCorrect();
            processFlow.PlaySuccess(() =>
            {
                currentTerminal.CompleteTerminal();
                Close();
            });
        }
        else
        {
            SoundManager.Instance.PlayTerminalWrong();
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
        if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Tab) &&
            !Input.GetKeyDown(KeyCode.Return))
            SoundManager.Instance.PlayTerminalTyping();
        if (Input.GetKeyDown(KeyCode.Tab))
            Close();
    }

    public bool IsActive()
    {
        return isActive;
    }
}