using System.Collections;
using UnityEngine;
using Unity.Cinemachine;

public class CutsceneManager : MonoBehaviour
{
    [Header("Cameras")]
    public CinemachineCamera playerVCam;
    public CinemachineCamera cutsceneVCam;

    [Header("Camera Movement")]
    public Transform cameraStartPoint;
    public Transform cameraEndPoint;
    public float moveDuration = 5f;

    [Header("Fade")]
    public float fadeOutDuration = 1f;
    public float blackScreenDuration = 1f;
    public float fadeInDuration = 2f;

    [Header("Player")]
    public MonoBehaviour playerController;

    [Header("UI")]
    public GameObject playerUI;

    private bool hasPlayed = false;
    private CanvasGroup fadeCanvas;

    void Start()
    {
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();
        // No GraphicRaycaster — fade panel must never block clicks

        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image img = panelObj.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;
        img.raycastTarget = false;

        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        fadeCanvas = panelObj.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 0f;
        fadeCanvas.blocksRaycasts = false;
    }

    public void PlayCutscene()
    {
        if (hasPlayed) return;
        hasPlayed = true;
        StartCoroutine(CutsceneRoutine());
    }

    IEnumerator CutsceneRoutine()
    {
        // Disable player control and UI
        if (playerController != null) playerController.enabled = false;
        if (playerUI != null) playerUI.SetActive(false);

        // Fade out to black — unscaled so timeScale doesn't matter
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));

        // Switch to cutscene camera while black
        playerVCam.gameObject.SetActive(false);
        cutsceneVCam.gameObject.SetActive(true);

        Quaternion lockedRotation = cameraStartPoint.rotation;
        cutsceneVCam.transform.position = cameraStartPoint.position;
        cutsceneVCam.transform.rotation = lockedRotation;

        yield return new WaitForSecondsRealtime(blackScreenDuration);

        // Fade cutscene camera in
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));

        // Move camera from start to end
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
            cutsceneVCam.transform.position = Vector3.Lerp(
                cameraStartPoint.position, cameraEndPoint.position, t);
            cutsceneVCam.transform.rotation = lockedRotation;
            yield return null;
        }

        yield return new WaitForSecondsRealtime(1f);

        // Fade to black
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));

        // Switch back to player camera
        cutsceneVCam.gameObject.SetActive(false);
        playerVCam.gameObject.SetActive(true);
        if (playerController != null) playerController.enabled = true;
        if (playerUI != null) playerUI.SetActive(true);

        yield return new WaitForSecondsRealtime(blackScreenDuration);

        // Fade back in
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));

        // Cutscene fully done
        OnCutsceneFinished();
    }

    private void OnCutsceneFinished()
    {
        if (GameManager.Instance != null)
            GameManager.Instance.Completion();
        else
            Debug.LogWarning("CutsceneManager: GameManager instance is missing!");
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime; // ← unscaled, works regardless of timeScale
            fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        fadeCanvas.alpha = to;
    }
}