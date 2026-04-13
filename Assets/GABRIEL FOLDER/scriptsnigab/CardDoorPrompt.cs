using UnityEngine;

public class CardDoorPrompt : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
            SecurityCardManager.Instance.OnPlayerEnterConsole();
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Player"))
            SecurityCardManager.Instance.OnPlayerExitConsole();
    }
}