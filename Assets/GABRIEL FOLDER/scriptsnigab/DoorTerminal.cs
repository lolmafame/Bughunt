using UnityEngine;
public class DoorTerminal : MonoBehaviour
{
    public bool isCompleted = false;
    public GameObject interactPrompt;
    private bool isOpen = false;
    [TextArea]
    public string instructions;
    [TextArea]
    public string correctAnswer;
    [Header("Linked Door")]
    public GameObject linkedDoor;

    private void OnTriggerEnter(Collider other)
    {
        if (isCompleted) return;
        if (other.CompareTag("Player"))
            interactPrompt.SetActive(true);
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
            CodeTerminalUI.Instance.OpenDoor(this);
        }
    }
    public void CompleteTerminal()
    {
        isCompleted = true;
        Renderer r = GetComponent<Renderer>();
        if (r != null) r.material.color = Color.green;

        // Opens its own linked door immediately
        if (linkedDoor != null)
            linkedDoor.SetActive(false);
        else
            Debug.LogWarning("DoorTerminal: No linked door assigned on " + gameObject.name);

        TerminalManager.Instance.TerminalCompleted();
        Debug.Log("DoorTerminal completed — door opened!");
    }
    public void OnClose()
    {
        isOpen = false;
    }
}