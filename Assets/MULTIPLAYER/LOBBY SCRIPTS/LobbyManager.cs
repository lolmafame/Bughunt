using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class LobbyManager : NetworkBehaviour
{
    public static LobbyManager Instance;

    public int maxPlayers = 4;
    public string gameSceneName = "SampleScene"; // your actual game scene name

    // Track ready states
    private NetworkList<ulong> readyPlayers;

    void Awake()
    {
        Instance = this;
        readyPlayers = new NetworkList<ulong>();
    }

    public override void OnNetworkSpawn()
    {
        readyPlayers.OnListChanged += OnReadyListChanged;
        LobbyUI.Instance?.UpdateReadyList(readyPlayers);
    }

    // Called when a player clicks Ready
    [ServerRpc(RequireOwnership = false)]
    public void SetReadyServerRpc(ulong clientId, bool isReady)
    {
        if (isReady)
        {
            if (!readyPlayers.Contains(clientId))
                readyPlayers.Add(clientId);
        }
        else
        {
            if (readyPlayers.Contains(clientId))
                readyPlayers.Remove(clientId);
        }

        UpdateReadyUIClientRpc();
    }

    void OnReadyListChanged(NetworkListEvent<ulong> changeEvent)
    {
        LobbyUI.Instance?.UpdateReadyList(readyPlayers);
    }

    [ClientRpc]
    void UpdateReadyUIClientRpc()
    {
        LobbyUI.Instance?.UpdateReadyList(readyPlayers);
    }

    // Called by host when clicking START
    public void StartGame()
    {
        if (!IsHost) return;

        int connectedPlayers = NetworkManager.Singleton.ConnectedClientsIds.Count;

        // Check all connected players are ready
        if (readyPlayers.Count < connectedPlayers)
        {
            Debug.Log("Not all players are ready!");
            LobbyUI.Instance?.ShowNotReadyWarning();
            return;
        }

        StartGameClientRpc();
    }

    [ClientRpc]
    void StartGameClientRpc()
    {
        StartCoroutine(LoadGameScene());
    }

    IEnumerator LoadGameScene()
    {
        // Show loading or countdown
        LobbyUI.Instance?.ShowStartingText();
        yield return new WaitForSeconds(2f);

        if (IsHost)
            NetworkManager.Singleton.SceneManager.LoadScene(
                gameSceneName, UnityEngine.SceneManagement.LoadSceneMode.Single);
    }
}