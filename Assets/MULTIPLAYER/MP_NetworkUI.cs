using UnityEngine;
using Unity.Netcode;
public class MP_NetworkUI : MonoBehaviour
{
    void OnGUI()
    {
        // Add null check before accessing NetworkManager
        if (NetworkManager.Singleton == null) return;
        GUILayout.BeginArea(new Rect(10, 10, 200, 100));
        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Host"))
                NetworkManager.Singleton.StartHost();
            if (GUILayout.Button("Client"))
                NetworkManager.Singleton.StartClient();
        }
        else
        {
            GUILayout.Label("Connected!");
        }
        GUILayout.EndArea();
    }
}