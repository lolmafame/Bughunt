using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using Unity.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class LobbyUI : MonoBehaviour
{
    public static LobbyUI Instance;

    [Header("Host UI")]
    public TMP_InputField hostCodeInput;     // where generated code is shown
    public Button generateCodeButton;
    public Button startButton;

    [Header("Client UI")]
    public TMP_InputField joinCodeInput;     // where client types code
    public Button joinButton;

    [Header("Lobby Panel")]
    public GameObject lobbyPanel;           // the right side lobby panel
    public Transform playerListParent;      // parent of player list items
    public GameObject playerListItemPrefab; // prefab for each player row
    public TMP_Text warningText;
    public TMP_Text startingText;

    [Header("Ready Button")]
    public Button readyButton;
    private bool isReady = false;

    private List<GameObject> playerListItems = new List<GameObject>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // Hide lobby panel at start
        if (lobbyPanel != null) lobbyPanel.SetActive(false);
        if (warningText != null) warningText.gameObject.SetActive(false);
        if (startingText != null) startingText.gameObject.SetActive(false);

        // Only host sees start button
        if (startButton != null) startButton.gameObject.SetActive(false);
    }

    // ------------------------------------------------
    // HOST
    // ------------------------------------------------
    public async void OnGenerateCodeClicked()
    {
        generateCodeButton.interactable = false;

        try
        {
            string code = await RelayManager.Instance.CreateRelay();
            hostCodeInput.text = code;
            ShowLobby();
            startButton.gameObject.SetActive(true);
            Debug.Log("Code generated: " + code);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to create relay: " + e.Message);
            generateCodeButton.interactable = true;
        }
    }

    // ------------------------------------------------
    // CLIENT
    // ------------------------------------------------
    public async void OnJoinClicked()
    {
        string code = joinCodeInput.text.Trim().ToUpper();
        if (string.IsNullOrEmpty(code))
        {
            Debug.LogWarning("Enter a join code!");
            return;
        }

        joinButton.interactable = false;

        try
        {
            await RelayManager.Instance.JoinRelay(code);
            ShowLobby();
            Debug.Log("Joined with code: " + code);
        }
        catch (System.Exception e)
        {
            Debug.LogError("Failed to join relay: " + e.Message);
            joinButton.interactable = true;
        }
    }

    // ------------------------------------------------
    // READY
    // ------------------------------------------------
    public void OnReadyClicked()
    {
        isReady = !isReady;

        // Update button text
        if (readyButton != null)
        {
            TMP_Text btnText = readyButton.GetComponentInChildren<TMP_Text>();
            if (btnText != null)
                btnText.text = isReady ? "UNREADY" : "READY";
        }

        if (LobbyManager.Instance != null)
            LobbyManager.Instance.SetReadyServerRpc(
                NetworkManager.Singleton.LocalClientId, isReady);
    }

    // ------------------------------------------------
    // START (host only)
    // ------------------------------------------------
    public void OnStartClicked()
    {
        if (LobbyManager.Instance != null)
            LobbyManager.Instance.StartGame();
    }

    // ------------------------------------------------
    // UI UPDATES
    // ------------------------------------------------
    void ShowLobby()
    {
        if (lobbyPanel != null) lobbyPanel.SetActive(true);
        UpdatePlayerList();
    }

    void UpdatePlayerList()
    {
        // Clear existing
        foreach (GameObject item in playerListItems)
            Destroy(item);
        playerListItems.Clear();

        if (NetworkManager.Singleton == null) return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListParent);
            TMP_Text nameText = item.GetComponentInChildren<TMP_Text>();
            if (nameText != null)
                nameText.text = clientId == NetworkManager.Singleton.LocalClientId
                    ? "You" : "Player " + clientId;
            playerListItems.Add(item);
        }
    }

    public void UpdateReadyList(NetworkList<ulong> readyPlayers)
    {
        // Update player list with ready states
        foreach (GameObject item in playerListItems)
            Destroy(item);
        playerListItems.Clear();

        if (NetworkManager.Singleton == null) return;

        foreach (ulong clientId in NetworkManager.Singleton.ConnectedClientsIds)
        {
            GameObject item = Instantiate(playerListItemPrefab, playerListParent);

            TMP_Text nameText = item.transform.Find("NameText")?.GetComponent<TMP_Text>();
            TMP_Text readyText = item.transform.Find("ReadyText")?.GetComponent<TMP_Text>();

            if (nameText != null)
                nameText.text = clientId == NetworkManager.Singleton.LocalClientId
                    ? "You" : "Player " + clientId;

            if (readyText != null)
                readyText.text = readyPlayers.Contains(clientId) ? "READY" : "...";

            playerListItems.Add(item);
        }
    }

    public void ShowNotReadyWarning()
    {
        if (warningText != null)
        {
            warningText.text = "Not all players are ready!";
            warningText.gameObject.SetActive(true);
            StartCoroutine(HideWarning());
        }
    }

    IEnumerator HideWarning()
    {
        yield return new WaitForSeconds(2f);
        if (warningText != null)
            warningText.gameObject.SetActive(false);
    }

    public void ShowStartingText()
    {
        if (startingText != null)
        {
            startingText.text = "Starting game...";
            startingText.gameObject.SetActive(true);
        }
    }
}