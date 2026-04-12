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
    public float moveDuration = 1f;

    [Header("Fade")]
    public float fadeOutDuration = 1f;
    public float blackScreenDuration = 1f;
    public float fadeInDuration = 0.2f;

    [Header("Player")]
    public MonoBehaviour playerController;

    [Header("UI")]
    public GameObject playerUI;

    [Header("Completion")]
    public GameObject completionPanel;
    public float completionPanelDelay = 0.2f;

    [Header("Objects To Disable During Cutscene")]
    public GameObject[] objectsToDisable;

    private bool hasPlayed = false;
    private CanvasGroup fadeCanvas;

    void Start()
    {
        SetupFadeCanvas();
    }

    void SetupFadeCanvas()
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
    }

    public void PlayCutscene()
    {
        Debug.Log("Cutscene triggered");
        if (hasPlayed) return;
        hasPlayed = true;
        StartCoroutine(CutsceneRoutine());
    }

    IEnumerator CutsceneRoutine()
    {
        // Disable player + UI
        Debug.Log("Coroutine STARTED");
        if (playerController != null) playerController.enabled = false;
        if (playerUI != null) playerUI.SetActive(false);
        Debug.Log("Step 1: Disabled player");

        // Disable other objects
        foreach (GameObject obj in objectsToDisable)
            if (obj != null) obj.SetActive(false);

        // Fade out
        yield return StartCoroutine(Fade(0f, 1f, fadeOutDuration));
        Debug.Log("Step 2: Fade out done");

        // Switch to cutscene camera
        playerVCam.gameObject.SetActive(false);
        cutsceneVCam.gameObject.SetActive(true);
        Debug.Log("Step 3: Camera switched");

        Quaternion lockedRotation = cameraStartPoint.rotation;
        cutsceneVCam.transform.position = cameraStartPoint.position;
        cutsceneVCam.transform.rotation = lockedRotation;


        yield return new WaitForSecondsRealtime(blackScreenDuration);
        Debug.Log("Step 4: Black screen done");

        // Fade in
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));
        Debug.Log("Step 5: Fade in done");

        // Move camera
        float elapsed = 0f;

        while (elapsed < moveDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / moveDuration);

            cutsceneVCam.transform.position =
                Vector3.Lerp(cameraStartPoint.position, cameraEndPoint.position, t);

            cutsceneVCam.transform.rotation = lockedRotation;

            yield return null;
        }

        // Switch back to player
        cutsceneVCam.gameObject.SetActive(false);
        playerVCam.gameObject.SetActive(true);

        if (playerController != null) playerController.enabled = true;
        if (playerUI != null) playerUI.SetActive(true);

        foreach (GameObject obj in objectsToDisable)
            if (obj != null) obj.SetActive(true);

        // Fade back in
        yield return StartCoroutine(Fade(1f, 0f, fadeInDuration));

        // 🔥 MAKE SURE FADE IS GONE
        fadeCanvas.alpha = 0f;
        fadeCanvas.gameObject.SetActive(false);

        // Wait before showing panel
        Debug.Log("Reached completion section");

        // ✅ SHOW COMPLETION PANEL (THIS IS THE IMPORTANT PART)
        if (GameManager.Instance == null)
        {
            Debug.LogError("GameManager.Instance is NULL at runtime!");
        }
        else
        {
            Debug.Log("Calling Completion()");
            GameManager.Instance.Completion();
        }


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