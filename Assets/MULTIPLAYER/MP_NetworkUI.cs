using UnityEngine;
using Unity.Netcode;

public class MP_NetworkUI : MonoBehaviour
{
    void OnGUI()
    {
        GUILayout.BeginArea(new Rect(10, 10, 200, 100));

        if (!NetworkManager.Singleton.IsClient && !NetworkManager.Singleton.IsServer)
        {
            if (GUILayout.Button("Host")) // starts as server + player
                NetworkManager.Singleton.StartHost();

            if (GUILayout.Button("Client")) // joins existing host
                NetworkManager.Singleton.StartClient();
        }
        else
        {
            GUILayout.Label("Connected!");
        }

        GUILayout.EndArea();
    }
}