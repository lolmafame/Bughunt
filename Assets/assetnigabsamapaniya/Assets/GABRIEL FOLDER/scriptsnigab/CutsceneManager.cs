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
    public float fadeOutDuration = 1f;  // how long player cam fades out
    public float blackScreenDuration = 1f; // how long black screen holds
    public float fadeInDuration = 2f;   // how long cutscene cam fades in

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
        canvasObj.AddComponent<UnityEngine.UI.GraphicRaycaster>();

        GameObject panelObj = new GameObject("FadePanel");
        panelObj.transform.SetParent(canvasObj.transform, false);
        UnityEngine.UI.Image img = panelObj.AddComponent<UnityEngine.UI.Image>();
        img.color = Color.black;

        RectTransform rect = panelObj.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.sizeDelta = Vector2.zero;

        fadeCanvas = panelObj.AddComponent<CanvasGroup>();
        fadeCanvas.alpha = 0f;
    }

    public void PlayCutscene()
    {
        if (hasPlayed) return;
        hasPlayed = true;
        StartCoroutine(CutsceneRoutine());
    }

    IEnumerator CutsceneRoutine()
    {
        // --- Fade player camera to black ---
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));

        // --- Switch cameras while completely black ---
        playerVCam.gameObject.SetActive(false);
        cutsceneVCam.gameObject.SetActive(true);
        if (playerController != null) playerController.enabled = false;
        if (playerUI != null) playerUI.SetActive(false);

        // Lock rotation, snap to start position
        Quaternion lockedRotation = cameraStartPoint.rotation;
        cutsceneVCam.transform.position = cameraStartPoint.position;
        cutsceneVCam.transform.rotation = lockedRotation;

        // --- Hold black screen for 1 second ---
        yield return new WaitForSeconds(blackScreenDuration);

        // --- Fade cutscene camera in over 2 seconds ---
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));

        // --- Move camera with no rotation change ---
        float elapsed = 0f;
        while (elapsed < moveDuration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);
            cutsceneVCam.transform.position = Vector3.Lerp(cameraStartPoint.position, cameraEndPoint.position, t);
            cutsceneVCam.transform.rotation = lockedRotation;
            yield return null;
        }

        yield return new WaitForSeconds(1f);

        // --- Fade to black again ---
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));

        // --- Switch back to player ---
        cutsceneVCam.gameObject.SetActive(false);
        playerVCam.gameObject.SetActive(true);
        if (playerController != null) playerController.enabled = true;
        if (playerUI != null) playerUI.SetActive(true);

        // --- Hold black then fade back in ---
        yield return new WaitForSeconds(blackScreenDuration);
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        fadeCanvas.alpha = to;
    }
}