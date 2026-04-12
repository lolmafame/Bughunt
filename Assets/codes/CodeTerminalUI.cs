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

        // Reset transition panels so nothing blocks the submit button
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

    void UnblockTypingSound()
    {
        blockTypingSound = false;
    }

    // Extracted helper — ensures cursor is ALWAYS free while terminal is open
    void SetCursorFree()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    public void Close()
    {
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
        if (currentTerminal == null) return;

        // Prevent double-submits while transition is playing
        if (!isActive) return;

        string trimmedInput = inputField.text.Trim();
        string trimmedAnswer = currentTerminal.correctAnswer.Trim();

        if (trimmedInput == trimmedAnswer)
        {
            isActive = false; // prevent re-submits during success transition
            StartCoroutine(PlaySoundDelayed(true, processFlow.transitionDuration));
            processFlow.PlaySuccess(() =>
            {
                currentTerminal.CompleteTerminal();
                Close();
            });
        }
        else
        {
            isActive = false; // prevent re-submits during fail transition
            StartCoroutine(PlaySoundDelayed(false, processFlow.transitionDuration));
            processFlow.PlayFail(() =>
            {
                // Re-enable everything cleanly after fail
                codePanel.SetActive(true);
                isActive = true;
                inputField.gameObject.SetActive(true);
                inputField.text = "";

                // Force cursor free AFTER the transition restores the panel
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

        // Keep cursor free every frame while terminal is open (guards against anything locking it)
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