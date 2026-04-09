using UnityEngine;
using Unity.Netcode;

public class MP_SpawnCamera : MonoBehaviour
{
    void Update()
    {
        if (NetworkManager.Singleton == null) return;
        var localPlayer = NetworkManager.Singleton.LocalClient?.PlayerObject;
        if (localPlayer != null)
            gameObject.SetActive(false);
    }
}