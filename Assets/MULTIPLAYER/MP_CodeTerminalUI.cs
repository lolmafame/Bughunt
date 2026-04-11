using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

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

        // ← Delayed focus fix
        StartCoroutine(FocusInputField());

        MP_GameManager.Instance.SetInputLocked(true);
        SoundManager.Instance.PlayTerminalOpen();

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
            MP_SpiderAI.Instance.InvestigateServerRpc(terminal.transform.position);
    }

    // ← New focus coroutine
    IEnumerator FocusInputField()
    {
        yield return new WaitForEndOfFrame();
        inputField.Select();
        inputField.ActivateInputField();
    }

    public void Close()
    {
        if (currentTerminal != null)
        {
            currentTerminal.SetInUseServerRpc(false);
            MP_GameManager.Instance.NotifySpiderInvestigateServerRpc(
                currentTerminal.transform.position);
        }

        codePanel.SetActive(false);
        isActive = false;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

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
        if (currentTerminal == null)
        {
            Debug.LogError("NO TERMINAL SET!");
            return;
        }

        string playerAnswer = inputField.text.Trim().ToLower().Replace(" ", "");
        string correctAnswer = currentTerminal.correctAnswer.Trim().ToLower().Replace(" ", "");

        Debug.Log("Player answer: '" + playerAnswer + "'");
        Debug.Log("Correct answer: '" + correctAnswer + "'");
        Debug.Log("Match: " + (playerAnswer == correctAnswer));

        if (playerAnswer == correctAnswer)
        {
            StartCoroutine(PlaySoundDelayed(true, processFlow.transitionDuration));
            processFlow.PlaySuccess(() =>
            {
                currentTerminal.CompleteTerminalServerRpc();
                Close();
            });
        }
        else
        {
            StartCoroutine(PlaySoundDelayed(false, processFlow.transitionDuration));
            processFlow.PlayFail(() =>
            {
                codePanel.SetActive(true);
                isActive = true;
                inputField.gameObject.SetActive(true);
                inputField.text = "";
                StartCoroutine(FocusInputField());
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible = true;
            });
        }
    }

    IEnumerator PlaySoundDelayed(bool success, float delay)
    {
        yield return new WaitForSecondsRealtime(delay);
        if (SoundManager.Instance == null) yield break;
        if (success)
            SoundManager.Instance.PlayTerminalCorrect();
        else
            SoundManager.Instance.PlayTerminalWrong();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Return))
        {
            Submit();
        }

        if (!isActive) return;

        // Keep input field always focused
        if (!inputField.isFocused)
            StartCoroutine(FocusInputField());

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