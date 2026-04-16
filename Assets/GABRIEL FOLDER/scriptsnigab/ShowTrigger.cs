using UnityEngine;

public class SpiderSpawnTrigger : MonoBehaviour
{
    public GameObject spiderObject;
    private bool hasSpawned = false;
    private bool isReady = false;

    private void Start()
    {
        if (spiderObject != null)
            spiderObject.SetActive(false);

        Invoke(nameof(EnableTrigger), 0.5f);
    }

    void EnableTrigger()
    {
        isReady = true;
    }

    private void OnTriggerEnter(Collider other)
    {
        if (!isReady) return;
        if (other.CompareTag("Player") && !hasSpawned)
        {
            hasSpawned = true;
            if (spiderObject != null)
                spiderObject.SetActive(true);
        }
    }
}