using UnityEngine;
using Unity.Netcode;

public class MP_CameraActivator : NetworkBehaviour
{
    public override void OnNetworkSpawn()
    {
        // Disable camera by default for everyone
        gameObject.SetActive(false);

        // Only enable for the local owner
        if (IsOwner)
            gameObject.SetActive(true);
    }
}