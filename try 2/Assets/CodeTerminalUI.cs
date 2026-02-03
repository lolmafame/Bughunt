using UnityEngine;
using UnityEngine.UI;

public class CodeTerminalUI : MonoBehaviour
{
    public static CodeTerminalUI Instance;

    public GameObject codePanel;
    public InputField inputField;

    private Terminal currentTerminal;
    private bool isActive = false;

    void Awake()
    {
        Instance = this;
        codePanel.SetActive(false);
    }

    public void Open(Terminal terminal)
    {
        currentTerminal = terminal;
        codePanel.SetActive(true);
        inputField.text = "";
        isActive = true;

        // Lock player movement
        ThirdPersonMovement playerMove = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonMovement>();
        playerMove.enabled = false;
    }

    public void Close()
    {
        codePanel.SetActive(false);
        isActive = false;

        // Unlock player movement
        ThirdPersonMovement playerMove = GameObject.FindGameObjectWithTag("Player").GetComponent<ThirdPersonMovement>();
        playerMove.enabled = true;
    }

    public void Submit()
    {
        if (currentTerminal == null) return;

        // Example: correct code = "int x;"
        if (inputField.text == "int x;")
        {
            currentTerminal.CompleteTerminal();
            Close();
        }
        else
        {
            Debug.Log("Wrong code! Try again.");
        }
    }

    void Update()
    {
        if (isActive && Input.GetKeyDown(KeyCode.Escape))
        {
            Close();
        }
    }
}
