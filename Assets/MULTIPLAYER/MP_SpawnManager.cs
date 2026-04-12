using UnityEngine;
using Unity.Netcode;

public class SpawnManager : NetworkBehaviour
{
    public static SpawnManager Instance;

    public Transform[] spawnPoints; // drag your spawn points here

    void Awake()
    {
        Instance = this;
    }

    public Vector3 GetRandomSpawnPoint()
    {
        if (spawnPoints == null || spawnPoints.Length == 0)
        {
            Debug.LogWarning("No spawn points set!");
            return Vector3.zero;
        }
        return spawnPoints[Random.Range(0, spawnPoints.Length)].position;
    }
}