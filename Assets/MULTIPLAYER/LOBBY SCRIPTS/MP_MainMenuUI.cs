using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using System.Collections;
using System.Collections.Generic;
using TMPro;

/// <summary>
/// Controls the Main Menu scene UI.
/// Handles Host / Join panel switching and LAN room discovery.
/// Attach to: MainMenuManager GameObject in MainMenu scene.
/// </summary>
public class MP_MainMenuUI : MonoBehaviour
{
    [Header("Panels")]
    public GameObject mainPanel;       // The root panel with Host & Join buttons
    public GameObject joinPanel;       // Panel shown when Join is clicked (room list)

    [Header("Main Panel Buttons")]
    public Button hostButton;
    public Button joinButton;

    [Header("Join Panel")]
    public Transform roomListParent;   // Vertical layout group to hold room entries
    public GameObject roomEntryPrefab; // Prefab: RoomEntry (has Text + Button)
    public Button backButton;
    public TMP_Text noRoomsText;       // "No rooms found..." text

    [Header("Host Name")]
    public TMP_InputField hostNameInput; // Optional: let host set their room name

    [Header("Scene Names")]
    public string lobbySceneName = "Lobby";

    private List<GameObject> roomEntryObjects = new List<GameObject>();
    private float refreshTimer = 0f;
    private float refreshInterval = 1f;

    void Start()
    {
        // Make sure we start clean
        ShowMainPanel();

        hostButton.onClick.AddListener(OnHostClicked);
        joinButton.onClick.AddListener(OnJoinClicked);
        backButton.onClick.AddListener(OnBackClicked);

        // Make sure NetworkManager isn't already running
        if (NetworkManager.Singleton != null &&
            (NetworkManager.Singleton.IsClient || NetworkManager.Singleton.IsServer))
        {
            NetworkManager.Singleton.Shutdown();
        }
    }

    void Update()
    {
        // Refresh room list while join panel is open
        if (joinPanel.activeSelf)
        {
            refreshTimer += Time.deltaTime;
            if (refreshTimer >= refreshInterval)
            {
                refreshTimer = 0f;
                RefreshRoomList();
            }
        }
    }

    // ─────────────────────────────────────────
    // HOST
    // ─────────────────────────────────────────

    void OnHostClicked()
    {
        string roomName = (hostNameInput != null && hostNameInput.text.Trim() != "")
            ? hostNameInput.text.Trim()
            : "Room " + Random.Range(100, 999);

        // Set transport to listen on all interfaces
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData("0.0.0.0", 7777, "0.0.0.0");

        NetworkManager.Singleton.StartHost();

        // Start broadcasting so clients can find this room
        LAN_Discovery.Instance.StartBroadcasting(roomName);

        // Load lobby scene via NetworkManager (syncs all clients)
        NetworkManager.Singleton.SceneManager.LoadScene(lobbySceneName, 
            UnityEngine.SceneManagement.LoadSceneMode.Single);
    }

    // ─────────────────────────────────────────
    // JOIN
    // ─────────────────────────────────────────

    void OnJoinClicked()
    {
        ShowJoinPanel();
        LAN_Discovery.Instance.StartListening();
    }

    void OnBackClicked()
    {
        LAN_Discovery.Instance.StopListening();
        ShowMainPanel();
    }

    public void JoinRoom(RoomInfo room)
    {
        LAN_Discovery.Instance.StopListening();

        // Connect to that host's IP
        UnityTransport transport = NetworkManager.Singleton.GetComponent<UnityTransport>();
        transport.SetConnectionData(room.hostIP, 7777, "0.0.0.0");

        NetworkManager.Singleton.StartClient();
        // NetworkManager will auto-load the Lobby scene since host already loaded it
    }

    // ─────────────────────────────────────────
    // ROOM LIST
    // ─────────────────────────────────────────

    void RefreshRoomList()
    {
        // Clear old entries
        foreach (var obj in roomEntryObjects)
            Destroy(obj);
        roomEntryObjects.Clear();

        List<RoomInfo> rooms;
        lock (LAN_Discovery.Instance.discoveredRooms)
        {
            rooms = new List<RoomInfo>(LAN_Discovery.Instance.discoveredRooms);
        }

        if (rooms.Count == 0)
        {
            noRoomsText.gameObject.SetActive(true);
            return;
        }

        noRoomsText.gameObject.SetActive(false);

        foreach (RoomInfo room in rooms)
        {
            GameObject entry = Instantiate(roomEntryPrefab, roomListParent);
            roomEntryObjects.Add(entry);

            // Find child components in the prefab
            TMP_Text label = entry.GetComponentInChildren<TMP_Text>();
            Button joinBtn = entry.GetComponentInChildren<Button>();

            if (label != null)
                label.text = $"{room.roomName}   {room.currentPlayers}/{room.maxPlayers}";

            if (joinBtn != null)
            {
                RoomInfo captured = room; // capture for lambda
                joinBtn.onClick.AddListener(() => JoinRoom(captured));

                // Disable join button if room is full
                joinBtn.interactable = room.currentPlayers < room.maxPlayers;
            }

            roomEntryObjects.Add(entry);
        }
    }

    // ─────────────────────────────────────────
    // PANEL HELPERS
    // ─────────────────────────────────────────

    void ShowMainPanel()
    {
        mainPanel.SetActive(true);
        joinPanel.SetActive(false);
    }

    void ShowJoinPanel()
    {
        mainPanel.SetActive(false);
        joinPanel.SetActive(true);
        noRoomsText.gameObject.SetActive(true);
        refreshTimer = refreshInterval; // refresh immediately
    }
}
