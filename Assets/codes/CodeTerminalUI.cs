using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class CodeTerminalUI : MonoBehaviour
{
    public static CodeTerminalUI Instance;
    public GameObject codePanel;
    public InputField inputField;
    public TransitionFlow transitionFlow;
    public ProcessTransition processFlow;
    private Terminal currentTerminal;
    private DoorTerminal currentDoorTerminal;
    private bool isActive = false;
    private bool blockTypingSound = false;
    public TMP_Text instructionText;

    private ThirdPersonMovement cachedPlayerMove;

    void Awake()
    {
        Instance = this;
        codePanel.SetActive(false);
    }

    void CachePlayerMovement()
    {
        if (cachedPlayerMove != null) return;
        GameObject playerObj = GameObject.FindGameObjectWithTag("Player");
        if (playerObj == null) { Debug.LogError("Player not found!"); return; }
        cachedPlayerMove = playerObj.GetComponent<ThirdPersonMovement>();
        if (cachedPlayerMove == null) Debug.LogError("ThirdPersonMovement not found!");
    }

    public void Open(Terminal terminal)
    {
        currentTerminal = terminal;
        currentDoorTerminal = null;

        processFlow.ResetPanels();

        codePanel.SetActive(true);
        transitionFlow.PlayTransition();
        inputField.text = "";
        isActive = true;

        if (instructionText != null)
            instructionText.text = terminal.instructions;

        SetCursorFree();
        inputField.Select();
        inputField.ActivateInputField();

        blockTypingSound = true;
        Invoke(nameof(UnblockTypingSound), 0.1f);

        if (GameManager.Instance != null)
            GameManager.Instance.SetInputLocked(true);

        CachePlayerMovement();
        if (cachedPlayerMove != null)
            cachedPlayerMove.enabled = false;

        SpiderAI spider = FindAnyObjectByType<SpiderAI>();
        if (spider != null)
            spider.ForceInvestigate(terminal.transform);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayTerminalOpen();
    }

    public void OpenDoor(DoorTerminal terminal)
    {
        currentDoorTerminal = terminal;
        currentTerminal = null;

        processFlow.ResetPanels();

        codePanel.SetActive(true);
        transitionFlow.PlayTransition();
        inputField.text = "";
        isActive = true;

        if (instructionText != null)
            instructionText.text = terminal.instructions;

        SetCursorFree();
        inputField.Select();
        inputField.ActivateInputField();

        blockTypingSound = true;
        Invoke(nameof(UnblockTypingSound), 0.1f);

        if (GameManager.Instance != null)
            GameManager.Instance.SetInputLocked(true);

        CachePlayerMovement();
        if (cachedPlayerMove != null)
            cachedPlayerMove.enabled = false;

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayTerminalOpen();
    }

    void UnblockTypingSound()
    {
        blockTypingSound = false;
    }

    void SetCursorFree()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
        if (currentDoorTerminal != null)
        {
            currentDoorTerminal.OnClose();
            currentDoorTerminal = null;
        }
        currentTerminal = null;

        codePanel.SetActive(false);
        isActive = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        CachePlayerMovement();
        if (cachedPlayerMove != null)
            cachedPlayerMove.enabled = true;

        if (GameManager.Instance != null)
            GameManager.Instance.SetInputLocked(false);

        SpiderAI spider = FindAnyObjectByType<SpiderAI>();
        if (spider != null)
            spider.StopInvestigate();

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayTerminalClose();
    }

    public void Submit()
    {
        bool isDoor = currentDoorTerminal != null;
        bool isTerminal = currentTerminal != null;

        if (!isDoor && !isTerminal) return;
        if (!isActive) return;

        string trimmedInput = inputField.text.Trim();
        string trimmedAnswer = isDoor
            ? currentDoorTerminal.correctAnswer.Trim()
            : currentTerminal.correctAnswer.Trim();

        if (trimmedInput == trimmedAnswer)
        {
            isActive = false;
            StartCoroutine(PlaySoundDelayed(true, processFlow.transitionDuration));
            processFlow.PlaySuccess(() =>
            {
                if (isDoor) currentDoorTerminal.CompleteTerminal();
                else currentTerminal.CompleteTerminal();
                Close();
            });
        }
        else
        {
            isActive = false;
            StartCoroutine(PlaySoundDelayed(false, processFlow.transitionDuration));
            processFlow.PlayFail(() =>
            {
                codePanel.SetActive(true);
                isActive = true;
                inputField.gameObject.SetActive(true);
                inputField.text = "";
                SetCursorFree();
                inputField.Select();
                inputField.ActivateInputField();
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
        if (!isActive) return;

        SetCursorFree();

        if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Tab) &&
            !Input.GetKeyDown(KeyCode.Return) && !blockTypingSound)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayTerminalTyping();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
            Close();

        if (Input.GetKeyDown(KeyCode.Return) || Input.GetKeyDown(KeyCode.KeypadEnter))
            Submit();
    }

    public bool IsActive()
    {
        return isActive;
    }
}