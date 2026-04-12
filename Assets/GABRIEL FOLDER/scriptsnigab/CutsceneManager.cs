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

    [Header("Completion")]
    public GameObject completionPanel;
    public float completionPanelDelay = 2f;

    [Header("Objects To Disable During Cutscene")]
    public GameObject[] objectsToDisable;

    private bool hasPlayed = false;
    private CanvasGroup fadeCanvas;

    void Start()
    {
        GameObject canvasObj = new GameObject("FadeCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvas.sortingOrder = 999;
        canvasObj.AddComponent<UnityEngine.UI.CanvasScaler>();

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

        if (completionPanel != null)
            completionPanel.SetActive(false);
        else
            Debug.LogWarning("CutsceneManager: CompletionPanel is not assigned!");
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

        // Disable NPCs/enemies
        foreach (GameObject obj in objectsToDisable)
            if (obj != null) obj.SetActive(false);

        // Fade out to black
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

        // Re-enable NPCs/enemies
        foreach (GameObject obj in objectsToDisable)
            if (obj != null) obj.SetActive(true);

        yield return new WaitForSecondsRealtime(blackScreenDuration);

        // Fade back in
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));

        // Wait then show completion panel
        yield return new WaitForSecondsRealtime(completionPanelDelay);

        if (completionPanel != null)
            completionPanel.SetActive(true);
        else
            Debug.LogWarning("CutsceneManager: CompletionPanel is not assigned!");

        // NOTE: OnCutsceneFinished removed — if GameManager.Completion()
        // loads a new scene it would destroy the panel before you see it
        // Add it back here ONLY if Completion() does NOT load a new scene
    }

    IEnumerator Fade(float from, float to, float duration)
    {
        float elapsed = 0f;
        while (elapsed < duration)
        {
            elapsed += Time.unscaledDeltaTime;
            fadeCanvas.alpha = Mathf.Lerp(from, to, elapsed / duration);
            yield return null;
        }
        fadeCanvas.alpha = to;
    }
}