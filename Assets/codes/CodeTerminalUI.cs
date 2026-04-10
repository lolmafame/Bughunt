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

        if (GameManager.Instance != null)
            GameManager.Instance.SetInputLocked(true);

        // Lock player movement
        CachePlayerMovement();
        if (cachedPlayerMove != null)
            cachedPlayerMove.enabled = false;

        SpiderAI spider = FindAnyObjectByType<SpiderAI>();
        if (spider != null)
            spider.ForceInvestigate(terminal.transform);

        if (SoundManager.Instance != null)
            SoundManager.Instance.PlayTerminalOpen();
        else
            Debug.LogWarning("CodeTerminalUI: SoundManager instance is missing!");
    }

    public void Close()
    {
        codePanel.SetActive(false);
        isActive = false;

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        // Unlock player movement
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
        else
            Debug.LogWarning("CodeTerminalUI: SoundManager instance is missing!");
    }

    public void Submit()
    {
        if (currentTerminal == null) return;

        if (inputField.text == currentTerminal.correctAnswer)
        {
            if (SoundManager.Instance != null)
                SoundManager.Instance.PlayTerminalCorrect();

            processFlow.PlaySuccess(() =>
            {
                currentTerminal.CompleteTerminal();
                Close();
            });
        }
        else
        {
            if (SoundManager.Instance != null)
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

        if (Input.anyKeyDown && !Input.GetKeyDown(KeyCode.Tab) && !Input.GetKeyDown(KeyCode.Return))
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