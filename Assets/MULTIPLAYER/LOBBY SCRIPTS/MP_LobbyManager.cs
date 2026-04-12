using UnityEngine;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using System.Collections.Generic;

/// <summary>
/// Manages the lobby state: player count tracking, start game logic.
/// Attach to: LobbyManager GameObject in Lobby scene.
/// This is a NetworkBehaviour so it syncs across all clients.
/// </summary>
public class MP_LobbyManager : NetworkBehaviour
{
    public static MP_LobbyManager Instance;

    [Header("Scene Names")]
    public string gameSceneName = "Game"; // Your main game scene name

    [Header("Spawn Points (Lobby)")]
    public Transform[] lobbySpawnPoints;

    // Synced player count visible to all clients
    public NetworkVariable<int> playerCount = new NetworkVariable<int>(
        0,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Server
    );

    private int maxPlayers = 4;

    void Awake()
    {
        Instance = this;
    }

    public override void OnNetworkSpawn()
    {
        // Subscribe to player count changes to update UI
        playerCount.OnValueChanged += OnPlayerCountChanged;

        if (IsServer)
        {
            // Track when clients connect/disconnect
            NetworkManager.Singleton.OnClientConnectedCallback += OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;

            // Count current players (host already connected)
            playerCount.Value = NetworkManager.Singleton.ConnectedClients.Count;

            // Update LAN broadcast with real count
            if (LAN_Discovery.Instance != null)
                LAN_Discovery.Instance.UpdatePlayerCount(playerCount.Value);
        }

        // Notify UI of current count immediately
        if (MP_LobbyUI.Instance != null)
            MP_LobbyUI.Instance.UpdatePlayerCount(playerCount.Value);
    }

    public override void OnNetworkDespawn()
    {
        playerCount.OnValueChanged -= OnPlayerCountChanged;

        if (IsServer)
        {
            NetworkManager.Singleton.OnClientConnectedCallback -= OnClientConnected;
            NetworkManager.Singleton.OnClientDisconnectCallback -= OnClientDisconnected;
        }
    }

    // ─────────────────────────────────────────
    // PLAYER TRACKING (Server only)
    // ─────────────────────────────────────────

    void OnClientConnected(ulong clientId)
    {
        if (!IsServer) return;

        playerCount.Value = NetworkManager.Singleton.ConnectedClients.Count;

        if (LAN_Discovery.Instance != null)
            LAN_Discovery.Instance.UpdatePlayerCount(playerCount.Value);

        Debug.Log($"[Lobby] Client connected. Players: {playerCount.Value}/{maxPlayers}");
    }

    void OnClientDisconnected(ulong clientId)
    {
        if (!IsServer) return;

        playerCount.Value = NetworkManager.Singleton.ConnectedClients.Count;

        if (LAN_Discovery.Instance != null)
            LAN_Discovery.Instance.UpdatePlayerCount(playerCount.Value);

        Debug.Log($"[Lobby] Client disconnected. Players: {playerCount.Value}/{maxPlayers}");
    }

    void OnPlayerCountChanged(int oldVal, int newVal)
    {
        // Update UI on all clients
        if (MP_LobbyUI.Instance != null)
            MP_LobbyUI.Instance.UpdatePlayerCount(newVal);
    }

    // ─────────────────────────────────────────
    // START GAME (Host only)
    // ─────────────────────────────────────────

    public void StartGame()
    {
        if (!IsServer) return;

        Debug.Log("[Lobby] Starting game!");

        // Stop broadcasting — game is starting, no new joiners
        if (LAN_Discovery.Instance != null)
            LAN_Discovery.Instance.StopBroadcasting();

        // Load game scene for ALL connected clients simultaneously
        NetworkManager.Singleton.SceneManager.LoadScene(
            gameSceneName,
            LoadSceneMode.Single
        );
    }

    // ─────────────────────────────────────────
    // SPAWN PLAYER IN LOBBY
    // ─────────────────────────────────────────

    public Vector3 GetLobbySpawnPoint(int index)
    {
        if (lobbySpawnPoints == null || lobbySpawnPoints.Length == 0)
            return Vector3.zero;

        int i = index % lobbySpawnPoints.Length;
        return lobbySpawnPoints[i].position;
    }
}
