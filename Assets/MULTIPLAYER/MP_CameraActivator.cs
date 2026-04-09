using UnityEngine;
using Unity.Netcode;

public class MP_CameraActivator : MonoBehaviour
{
    private void Start()
    {
        NetworkObject parentNetObj = GetComponentInParent<NetworkObject>();
        if (parentNetObj == null) return;

        // Disable camera by default
        gameObject.SetActive(false);

        // Only enable if this is the local player
        if (parentNetObj.IsOwner)
            gameObject.SetActive(true);
    }
}