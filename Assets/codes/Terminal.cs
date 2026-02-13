using UnityEngine;

public class Terminal : MonoBehaviour
{
    public bool isCompleted = false;
    public GameObject interactPrompt;

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
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (isCompleted) return;

        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
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
    }
}
