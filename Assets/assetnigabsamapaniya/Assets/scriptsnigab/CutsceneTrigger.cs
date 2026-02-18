using UnityEngine;

public class CutsceneTrigger : MonoBehaviour
{
    public CutsceneManager cutsceneManager;
    public string playerTag = "Player";

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag(playerTag))
        {
            cutsceneManager.PlayCutscene();
            // Disable trigger so it only fires once
            gameObject.SetActive(false);
        }
    }
}