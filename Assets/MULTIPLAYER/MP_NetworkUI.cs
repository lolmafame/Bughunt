using UnityEngine;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using TMPro;
using UnityEngine.UI;
using System.Net;
using System.Net.Sockets;
using System.Collections;
using System.Collections.Generic;

public class MP_NetworkUI : MonoBehaviour
{
    public static MP_NetworkUI Instance;

    [Header("Connect Panel — solid full screen")]
    public GameObject connectPanel;
    public Button generateCodeButton;
    public TMP_InputField ipInputField;
    public Button joinButton;

    [Header("Lobby HUD — small floating UI while walking")]
    public GameObject lobbyHUD;
    public TMP_Text yourIPText;        // host sees their IP here
    public Button startButton;         // host only

    [Header("Game UI — hidden until game starts")]
    public GameObject staminaBG;
    public GameObject healthBarBG;
    public GameObject terminalCounter;
    public GameObject gameTimerText;
    public GameObject hurtFlash;
    public GameObject invincibleFlash;

    [Header("Game Objects — hidden until game starts")]
    public GameObject spiderObject;
    public GameObject[] terminalObjects;

    [Header("Spawn Points")]
    public Transform[] lobbySpawnPoints;
    public Transform[] gameSpawnPoints;

    [Header("Settings")]
    public ushort port = 7777;

    private bool gameStarted = false;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Show connect panel
        if (connectPanel != null) connectPanel.SetActive(true);

        // Hide lobby HUD
        if (lobbyHUD != null) lobbyHUD.SetActive(false);

        // Hide all game UI
        SetGameUIActive(false);

        // Hide game objects
        SetGameObjectsActive(false);
    }

    // ------------------------------------------------
    // HOST — clicks Generate Code
    // ------------------------------------------------
    public void OnGenerateCodeClicked()
    {
        string localIP = GetLocalIPAddress();

        // Set transport
        UnityTransport transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", port);

        NetworkManager.Singleton.StartHost();

        // Hide connect panel
        if (connectPanel != null) connectPanel.SetActive(false);

        // Show lobby HUD with IP
        if (lobbyHUD != null) lobbyHUD.SetActive(true);
        if (yourIPText != null) yourIPText.text = "Your IP: " + localIP;

        // Show start button — host only
        if (startButton != null) startButton.gameObject.SetActive(true);

        // Spawn host in lobby
        SpawnInLobby(NetworkManager.Singleton.LocalClientId);

        Debug.Log("Hosting on: " + localIP + ":" + port);
    }

    // ------------------------------------------------
    // CLIENT — types IP and clicks Join
    // ------------------------------------------------
    public void OnJoinClicked()
    {
        string ip = ipInputField.text.Trim();
        if (string.IsNullOrEmpty(ip))
        {
            Debug.LogWarning("Enter host IP!");
            return;
        }

        UnityTransport transport =
            NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(ip, port);

        NetworkManager.Singleton.StartClient();

        // Hide connect panel
        if (connectPanel != null) connectPanel.SetActive(false);

        // Show lobby HUD — no IP text, no start button for client
        if (lobbyHUD != null) lobbyHUD.SetActive(true);
        if (yourIPText != null) yourIPText.gameObject.SetActive(false);
        if (startButton != null) startButton.gameObject.SetActive(false);

        Debug.Log("Joining: " + ip + ":" + port);
    }

    // ------------------------------------------------
    // START — host only
    // ------------------------------------------------
    public void OnStartClicked()
    {
        if (!NetworkManager.Singleton.IsHost) return;

        StartGameClientRpc();
    }

    [System.Obsolete]
    void StartGameClientRpc()
    {
        StartCoroutine(StartGameCoroutine());
    }

    IEnumerator StartGameCoroutine()
    {
        // Show starting text or just hide HUD
        if (lobbyHUD != null) lobbyHUD.SetActive(false);

        yield return new WaitForSeconds(1f);

        // Teleport all players to game area
        TeleportAllPlayersToGame();

        // Show game UI
        SetGameUIActive(true);

        // Show game objects
        SetGameObjectsActive(true);

        // Start timer
        if (MP_GameManager.Instance != null &&
            MP_GameManager.Instance.gameTimer != null)
            MP_GameManager.Instance.gameTimer.StartTimer();

        gameStarted = true;

        Debug.Log("Game started!");
    }

    void TeleportAllPlayersToGame()
    {
        int index = 0;
        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            if (NetworkManager.Singleton.ConnectedClients
                .TryGetValue(clientId, out var client))
            {
                if (gameSpawnPoints.Length > 0)
                {
                    Transform spawn = gameSpawnPoints[index % gameSpawnPoints.Length];
                    client.PlayerObject.transform.position = spawn.position;
                }
                index++;
            }
        }
    }

    void SpawnInLobby(ulong clientId)
    {
        if (lobbySpawnPoints.Length == 0) return;

        if (NetworkManager.Singleton.ConnectedClients
            .TryGetValue(clientId, out var client))
        {
            int index = NetworkManager.Singleton.ConnectedClientsIds.Count %
                        lobbySpawnPoints.Length;
            client.PlayerObject.transform.position =
                lobbySpawnPoints[index].position;
        }
    }

    // ------------------------------------------------
    // HELPERS
    // ------------------------------------------------
    void SetGameUIActive(bool active)
    {
        if (staminaBG != null) staminaBG.SetActive(active);
        if (healthBarBG != null) healthBarBG.SetActive(active);
        if (terminalCounter != null) terminalCounter.SetActive(active);
        if (gameTimerText != null) gameTimerText.SetActive(active);
        if (hurtFlash != null) hurtFlash.SetActive(active);
        if (invincibleFlash != null) invincibleFlash.SetActive(active);
    }

    void SetGameObjectsActive(bool active)
    {
        if (spiderObject != null) spiderObject.SetActive(active);
        foreach (GameObject terminal in terminalObjects)
            if (terminal != null) terminal.SetActive(active);
    }

    string GetLocalIPAddress()
    {
        try
        {
            using Socket socket = new Socket(
                AddressFamily.InterNetwork, SocketType.Dgram, 0);
            socket.Connect("8.8.8.8", 65530);
            IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
            return endPoint.Address.ToString();
        }
        catch
        {
            return "127.0.0.1";
        }
    }
}