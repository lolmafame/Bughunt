using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;

public class MP_TerminalManager : NetworkBehaviour
{
    public static MP_TerminalManager Instance;

    public int totalTerminals = 5;
    public NetworkVariable<int> completedTerminals = new NetworkVariable<int>(0);

    public Text terminalText;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        completedTerminals.OnValueChanged += OnTerminalCountChanged;
        UpdateUI();
    }

    [ServerRpc(RequireOwnership = false)]
    public void TerminalCompletedServerRpc()
    {
        completedTerminals.Value++;

        PlayCompleteSoundClientRpc();

        if (completedTerminals.Value >= totalTerminals)
        {
            Debug.Log("ALL TERMINALS COMPLETED!");
            PlayAllDoneSoundClientRpc();
            MP_GameManager.Instance.TriggerCompletionServerRpc();
        }
    }

    [ClientRpc]
    void PlayCompleteSoundClientRpc()
    {
        SoundManager.Instance.PlayTerminalComplete();
    }

    [ClientRpc]
    void PlayAllDoneSoundClientRpc()
    {
        SoundManager.Instance.PlayAllTerminalsDone();
    }

    void OnTerminalCountChanged(int oldVal, int newVal)
    {
        UpdateUI();
    }

    void UpdateUI()
    {
        terminalText.text = completedTerminals.Value + " / " + totalTerminals + " Terminals";
    }
}