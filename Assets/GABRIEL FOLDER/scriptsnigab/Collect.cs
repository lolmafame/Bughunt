using UnityEngine;

public class SecurityCard : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("Player"))
        {
            SecurityCardManager.Instance.CollectCard();
            gameObject.SetActive(false); // Hides the card
        }
    }
}