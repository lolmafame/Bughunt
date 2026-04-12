using System.Threading.Tasks;
using Unity.Netcode;
using Unity.Netcode.Transports.UTP;
using Unity.Networking.Transport.Relay;
using Unity.Services.Authentication;
using Unity.Services.Core;
using Unity.Services.Relay;
using Unity.Services.Relay.Models;
using UnityEngine;

public class RelayManager : MonoBehaviour
{
    public static RelayManager Instance;

    private string joinCode;

    void Awake()
    {
        Instance = this;
    }

    public async Task InitializeUnityServices()
    {
        if (UnityServices.State == ServicesInitializationState.Initialized) return;

        await UnityServices.InitializeAsync();

        if (!AuthenticationService.Instance.IsSignedIn)
            await AuthenticationService.Instance.SignInAnonymouslyAsync();

        Debug.Log("Unity Services initialized. Player ID: " + AuthenticationService.Instance.PlayerId);
    }

    public async Task<string> CreateRelay()
    {
        await InitializeUnityServices();

        Allocation allocation = await RelayService.Instance.CreateAllocationAsync(3); // max 3 others + host = 4
        joinCode = await RelayService.Instance.GetJoinCodeAsync(allocation.AllocationId);

        RelayServerData relayServerData = new RelayServerData(allocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartHost();

        Debug.Log("Relay created! Join code: " + joinCode);
        return joinCode;
    }

    public async Task JoinRelay(string code)
    {
        await InitializeUnityServices();

        JoinAllocation joinAllocation = await RelayService.Instance.JoinAllocationAsync(code);

        RelayServerData relayServerData = new RelayServerData(joinAllocation, "dtls");
        NetworkManager.Singleton.GetComponent<UnityTransport>().SetRelayServerData(relayServerData);
        NetworkManager.Singleton.StartClient();

        Debug.Log("Joined relay with code: " + code);
    }

    public string GetJoinCode() => joinCode;
}