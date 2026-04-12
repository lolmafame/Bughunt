using UnityEngine;
using UnityEngine.UI;
using Unity.Netcode;
using TMPro;

/// <summary>
/// Controls the Lobby scene UI.
/// Shows player count to everyone.
/// Shows Start button ONLY to host (disabled until 2+ players).
/// Attach to: LobbyUI GameObject in Lobby scene.
/// </summary>
public class MP_LobbyUI : NetworkBehaviour
{
    public static MP_LobbyUI Instance;

    [Header("UI References")]
    public TMP_Text playerCountText;   // e.g. "Players: 2 / 4"
    public TMP_Text waitingText;       // e.g. "Waiting for players..."
    public Button startButton;         // Only visible + usable by host
    public TMP_Text startButtonText;   // Text on the start button
    public GameObject hostOnlyPanel;   // Panel that wraps the start button (host only)

    private int maxPlayers = 4;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // Only show host panel to the host
        if (hostOnlyPanel != null)
            hostOnlyPanel.SetActive(IsServer);

        if (startButton != null)
            startButton.onClick.AddListener(OnStartClicked);

        // Set initial state
        UpdatePlayerCount(1);
    }

    // ─────────────────────────────────────────
    // UPDATE UI
    // ─────────────────────────────────────────

    public void UpdatePlayerCount(int count)
    {
        // Update player count text (everyone sees this)
        if (playerCountText != null)
            playerCountText.text = $"Players: {count} / {maxPlayers}";

        // Update waiting text
        if (waitingText != null)
        {
            if (count < 2)
                waitingText.text = "Waiting for players to join...";
            else if (count >= maxPlayers)
                waitingText.text = "Room is full!";
            else
                waitingText.text = "Ready to start!";
        }

        // Update start button state (host only)
        if (IsServer && startButton != null)
        {
            // Enable only if at least 2 players (host + 1 joiner)
            bool canStart = count >= 2;
            startButton.interactable = canStart;

            if (startButtonText != null)
            {
                if (count < 2)
                    startButtonText.text = "Waiting for players...";
                else
                    startButtonText.text = $"Start Game ({count} players)";
            }
        }
    }

    // ─────────────────────────────────────────
    // START BUTTON
    // ─────────────────────────────────────────

    void OnStartClicked()
    {
        if (!IsServer) return;

        if (MP_LobbyManager.Instance != null)
            MP_LobbyManager.Instance.StartGame();
    }
}
