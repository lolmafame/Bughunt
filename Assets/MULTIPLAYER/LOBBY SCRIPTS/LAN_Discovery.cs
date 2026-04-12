using UnityEngine;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;
using System.Collections.Generic;

/// <summary>
/// Handles LAN room broadcasting (host side) and room discovery (client side).
/// Attach this to a persistent GameObject in MainMenu scene.
/// </summary>
public class LAN_Discovery : MonoBehaviour
{
    public static LAN_Discovery Instance;

    [Header("Network Settings")]
    public int broadcastPort = 47777;
    public float broadcastInterval = 1f;

    // Room info that gets broadcasted
    public string roomName = "Game Room";
    public int currentPlayers = 1;
    public int maxPlayers = 4;

    // Internal
    private UdpClient broadcaster;
    private UdpClient listener;
    private Thread listenThread;
    private bool isBroadcasting = false;
    private bool isListening = false;

    // Discovered rooms (client side)
    public List<RoomInfo> discoveredRooms = new List<RoomInfo>();
    private float cleanupTimer = 0f;
    private float roomTimeout = 3f; // remove room if not heard from in 3 seconds

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    void Update()
    {
        // Clean up stale rooms
        if (isListening)
        {
            cleanupTimer += Time.deltaTime;
            if (cleanupTimer >= 1f)
            {
                cleanupTimer = 0f;
                CleanupStaleRooms();
            }
        }
    }

    // ─────────────────────────────────────────
    // HOST: Broadcast room info over LAN
    // ─────────────────────────────────────────

    public void StartBroadcasting(string hostName)
    {
        if (isBroadcasting) return;
        roomName = hostName;
        isBroadcasting = true;
        InvokeRepeating(nameof(SendBroadcast), 0f, broadcastInterval);
        Debug.Log("[LAN] Started broadcasting room: " + roomName);
    }

    public void StopBroadcasting()
    {
        isBroadcasting = false;
        CancelInvoke(nameof(SendBroadcast));
        broadcaster?.Close();
        broadcaster = null;
        Debug.Log("[LAN] Stopped broadcasting.");
    }

    public void UpdatePlayerCount(int count)
    {
        currentPlayers = count;
    }

    void SendBroadcast()
    {
        try
        {
            broadcaster = new UdpClient();
            broadcaster.EnableBroadcast = true;

            // Format: "ROOM|roomName|currentPlayers|maxPlayers|hostIP"
            string localIP = GetLocalIP();
            string message = $"ROOM|{roomName}|{currentPlayers}|{maxPlayers}|{localIP}";
            byte[] data = Encoding.UTF8.GetBytes(message);
            string[] parts = localIP.Split('.');
            string subnetBroadcast = parts[0] + "." + parts[1] + "." + parts[2] + ".255";
            IPEndPoint endpoint = new IPEndPoint(IPAddress.Parse(subnetBroadcast), broadcastPort);


            broadcaster.Send(data, data.Length, endpoint);
            broadcaster.Close();
            broadcaster = null;
        }
        catch (System.Exception e)
        {
            Debug.LogWarning("[LAN] Broadcast error: " + e.Message);
        }
    }

    // ─────────────────────────────────────────
    // CLIENT: Listen for room broadcasts
    // ─────────────────────────────────────────

    public void StartListening()
    {
        if (isListening) return;
        isListening = true;
        discoveredRooms.Clear();

        listenThread = new Thread(ListenLoop);
        listenThread.IsBackground = true;
        listenThread.Start();
        Debug.Log("[LAN] Started listening for rooms...");
    }

    public void StopListening()
    {
        isListening = false;
        listener?.Close();
        listener = null;
        listenThread?.Abort();
        Debug.Log("[LAN] Stopped listening.");
    }

    void ListenLoop()
    {
        try
        {
            listener = new UdpClient(broadcastPort);
            listener.EnableBroadcast = true;

            while (isListening)
            {
                IPEndPoint remoteEP = new IPEndPoint(IPAddress.Any, 0);
                byte[] data = listener.Receive(ref remoteEP);
                string message = Encoding.UTF8.GetString(data);

                if (message.StartsWith("ROOM|"))
                {
                    ParseRoomMessage(message, remoteEP.Address.ToString());
                }
            }
        }
        catch (System.Exception e)
        {
            if (isListening)
                Debug.LogWarning("[LAN] Listen error: " + e.Message);
        }
    }

    void ParseRoomMessage(string message, string senderIP)
    {
        // Format: "ROOM|roomName|currentPlayers|maxPlayers|hostIP"
        string[] parts = message.Split('|');
        if (parts.Length < 5) return;

        string rName = parts[1];
        int rCurrent = int.Parse(parts[2]);
        int rMax = int.Parse(parts[3]);
        string rIP = parts[4];

        // Update on main thread via lock
        lock (discoveredRooms)
        {
            RoomInfo existing = discoveredRooms.Find(r => r.hostIP == rIP);
            if (existing != null)
            {
                existing.roomName = rName;
                existing.currentPlayers = rCurrent;
                existing.maxPlayers = rMax;
                existing.lastSeen = Time.realtimeSinceStartup;
            }
            else
            {
                if (rCurrent < rMax) // only show rooms that aren't full
                {
                    discoveredRooms.Add(new RoomInfo
                    {
                        roomName = rName,
                        currentPlayers = rCurrent,
                        maxPlayers = rMax,
                        hostIP = rIP,
                        lastSeen = Time.realtimeSinceStartup
                    });
                    Debug.Log("[LAN] Found room: " + rName + " at " + rIP);
                }
            }
        }
    }

    void CleanupStaleRooms()
    {
        float now = Time.realtimeSinceStartup;
        lock (discoveredRooms)
        {
            discoveredRooms.RemoveAll(r => now - r.lastSeen > roomTimeout);
        }
    }

    // ─────────────────────────────────────────
    // UTILITY
    // ─────────────────────────────────────────

    public static string GetLocalIP()
    {
        try
        {
            using (Socket socket = new Socket(AddressFamily.InterNetwork, SocketType.Dgram, 0))
            {
                socket.Connect("8.8.8.8", 65530);
                IPEndPoint endPoint = socket.LocalEndPoint as IPEndPoint;
                return endPoint.Address.ToString();
            }
        }
        catch
        {
            return "127.0.0.1";
        }
    }

    void OnDestroy()
    {
        StopBroadcasting();
        StopListening();
    }
}

[System.Serializable]
public class RoomInfo
{
    public string roomName;
    public int currentPlayers;
    public int maxPlayers;
    public string hostIP;
    public float lastSeen;
}
