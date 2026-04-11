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
    private MP_Terminal currentTerminal;
    private bool isActive = false;
    public TMP_Text instructionText;


    void Awake()
    {
        Instance = this;
        codePanel.SetActive(false);
    }


    public void Open(MP_Terminal terminal)
    {
        if (terminal.isInUse.Value) return;
        terminal.SetInUseServerRpc(true);

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
        MP_GameManager.Instance.SetInputLocked(true);
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

        if (MP_SpiderAI.Instance != null && MP_SpiderAI.Instance.IsSpawned)
        {
            MP_SpiderAI.Instance.InvestigateServerRpc(terminal.transform.position);
        }
    }

    public void Close()
    {
        if (currentTerminal != null)
        {
            currentTerminal.SetInUseServerRpc(false);

            MP_GameManager.Instance.NotifySpiderInvestigateServerRpc(currentTerminal.transform.position);
        }

        codePanel.SetActive(false);
        isActive = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        MP_GameManager.Instance.SetInputLocked(false);
        SoundManager.Instance.PlayTerminalClose();

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
    }

    public void Submit()
    {
        Debug.Log("Submit clicked");

        if (currentTerminal == null)
        {
            Debug.LogError("NO TERMINAL SET!");
            return;
        }

            if (inputField.text == currentTerminal.correctAnswer)
        {
            SoundManager.Instance.PlayTerminalCorrect();

            processFlow.PlaySuccess(() =>
            {
                currentTerminal.CompleteTerminalServerRpc();// ✅ FIXED
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