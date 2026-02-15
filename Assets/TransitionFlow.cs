using UnityEngine;
using System.Collections;

public class TransitionFlow : MonoBehaviour
{
    public GameObject transitionPanel;
    public GameObject objectToOpen;

    public LoadingDots loadingDots;

    public float transitionDuration = 2f;

    public void PlayTransition()
    {
        StartCoroutine(TransitionRoutine());
    }

    IEnumerator TransitionRoutine()
    {
        // Show transition panel
        transitionPanel.SetActive(true);

        yield return new WaitForSecondsRealtime(transitionDuration);

        // Open next panel
        if (objectToOpen != null)
            objectToOpen.SetActive(true);

        // STOP loading animation
        if (loadingDots != null)
            loadingDots.StopDots();

        // Hide transition panel
        transitionPanel.SetActive(false);
    }
}
