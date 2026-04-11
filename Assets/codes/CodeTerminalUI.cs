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

        // Block typing sound briefly so open doesn't trigger it
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

        if (inputField.text == currentTerminal.correctAnswer)
        {
            // ← Sound plays after transition duration
            StartCoroutine(PlaySoundDelayed(true, processFlow.transitionDuration));
            processFlow.PlaySuccess(() =>
            {
                currentTerminal.CompleteTerminal();
                Close();
            });
        }
        else
        {
            // ← Sound plays after transition duration
            StartCoroutine(PlaySoundDelayed(false, processFlow.transitionDuration));
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

    // ← New method that delays the sound
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

        if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Tab) &&
            !Input.GetKeyDown(KeyCode.Return) && !blockTypingSound) // ← typing sound fix
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayTerminalTyping();
        }

        if (Input.GetKeyDown(KeyCode.Tab))
            Close();
    }

    public bool IsActive()
    {
        return isActive;
    }
}