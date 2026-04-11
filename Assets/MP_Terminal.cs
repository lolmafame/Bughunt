using UnityEngine;
using Unity.Netcode;

public class MP_Terminal : NetworkBehaviour
{
    public GameObject interactPrompt;

    [TextArea] public string instructions;
    [TextArea] public string correctAnswer;

    public NetworkVariable<bool> isCompleted = new NetworkVariable<bool>(false);
    public NetworkVariable<bool> isInUse = new NetworkVariable<bool>(false);

    private bool isOpen = false;

    private void Start()
    {
        isCompleted.OnValueChanged += OnCompletedChanged;
    }

    // ------------------------
    // PLAYER INTERACTION
    // ------------------------

    private void OnTriggerEnter(Collider other)
    {
        if (isCompleted.Value) return;

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
        if (isCompleted.Value) return;
        if (isOpen) return;

        if (other.CompareTag("Player") && Input.GetKeyDown(KeyCode.E))
        {
            isOpen = true;
            interactPrompt.SetActive(false);

            MP_CodeTerminalUI.Instance.Open(this); 
        }
    }

    // ------------------------
    // MULTIPLAYER LOGIC
    // ------------------------

    [ServerRpc(RequireOwnership = false)]
    public void CompleteTerminalServerRpc()
    {
        if (isCompleted.Value) return;

        isCompleted.Value = true;

        MP_TerminalManager.Instance.TerminalCompletedServerRpc();
    }

    [ServerRpc(RequireOwnership = false)]
    public void SetInUseServerRpc(bool value)
    {
        isInUse.Value = value;
    }

    // ------------------------
    // SYNC VISUALS
    // ------------------------

    void OnCompletedChanged(bool oldVal, bool newVal)
    {
        if (newVal)
        {
            Debug.Log("Terminal completed!");

            if (interactPrompt != null)
                interactPrompt.SetActive(false);

            GetComponent<Renderer>().material.color = Color.green;
        }
    }

    // ------------------------
    // UI CLOSE
    // ------------------------

    public void OnClose()
    {
        isOpen = false;
    }
}