using UnityEngine;

public class Terminal : MonoBehaviour
{
    public bool isCompleted = false;
    public GameObject interactPrompt;
    private bool isOpen = false;

    [TextArea]
    public string instructions;
    [TextArea]
    public string correctAnswer;

    [Header("Door Settings")]
    public GameObject doorObject;           // Drag your 3D door object here
    public float disappearDelay = 0f;      // Optional delay before door disappears

    private void OnTriggerEnter(Collider other)
    {
        if (isCompleted) return;
        if (other.CompareTag("Player"))
        {
            interactPrompt.SetActive(true);
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            interactPrompt.SetActive(false);
            OnClose();
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isCompleted) return;
        if (isOpen) return;
        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = true;
            interactPrompt.SetActive(false);
            CodeTerminalUI.Instance.Open(this);
        }
    }

    public void CompleteTerminal()
    {
        isCompleted = true;
        TerminalManager.Instance.TerminalCompleted();
        Debug.Log("Terminal completed!");
        GetComponent<Renderer>().material.color = Color.green;

        // Remove the door
        if (doorObject != null)
        {
            if (disappearDelay > 0f)
                Invoke(nameof(RemoveDoor), disappearDelay);
            else
                RemoveDoor();
        }
    }

    private void RemoveDoor()
    {
        if (doorObject != null)
        {
            doorObject.SetActive(false);   // Hides the door (keep if you need to re-enable later)
            // Destroy(doorObject);        // Permanently removes it — uncomment if preferred
        }
    }

    public void OnClose()
    {
        isOpen = false;
    }
}