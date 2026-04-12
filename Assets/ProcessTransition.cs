using UnityEngine;
using System.Collections;
using System;

public class ProcessTransition : MonoBehaviour
{
    public GameObject transitionPanel;
    public GameObject successPanel;
    public GameObject failedPanel;
    public LoadingDots loadingDots;
    public float transitionDuration = 2f;
    public float failReturnDelay = 2f;

    // Call this on terminal open to guarantee clean state
    public void ResetPanels()
    {
        transitionPanel.SetActive(false);
        successPanel.SetActive(false);
        failedPanel.SetActive(false);
    }

    public void PlaySuccess(Action onComplete)
    {
        StartCoroutine(SuccessRoutine(onComplete));
    }

    public void PlayFail(Action onReturn)
    {
        StartCoroutine(FailRoutine(onReturn));
    }

    IEnumerator SuccessRoutine(Action onComplete)
    {
        transitionPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(transitionDuration);
        loadingDots.StopDots();
        successPanel.SetActive(true);
        transitionPanel.SetActive(false);
        yield return new WaitForSecondsRealtime(failReturnDelay);
        successPanel.SetActive(false);
        onComplete?.Invoke();
    }

    IEnumerator FailRoutine(Action onReturn)
    {
        transitionPanel.SetActive(true);
        yield return new WaitForSecondsRealtime(transitionDuration);
        loadingDots.StopDots();
        failedPanel.SetActive(true);
        transitionPanel.SetActive(false);
        yield return new WaitForSecondsRealtime(failReturnDelay);
        failedPanel.SetActive(false);
        transitionPanel.SetActive(false);
        onReturn?.Invoke();
    }
}