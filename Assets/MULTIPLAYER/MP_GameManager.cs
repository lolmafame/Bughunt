using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;

public class MP_GameManager : NetworkBehaviour
{
    public static MP_GameManager Instance;

    [Header("Panels")]
    public GameObject pausePanel;
    public GameObject gameOverPanel;
    public GameObject completionPanel;

    [Header("Game Over UI")]
    public Text gameOverTimeText;
    public Text gameOverTerminalText;

    [Header("Completion UI")]
    public Text completionTimeText;
    public Text completionTerminalText;

    public GameTimer gameTimer;

    [Header("Scene Settings")]
    public string mainMenuSceneName = "MainMenu";
    public string level2SceneName = "Level2";

    [Header("Spawn Point")]
    public Transform spawnPoint;

    private bool isPaused = false;
    private bool inputLocked = false;

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    [ServerRpc(RequireOwnership = false)]
    public void NotifySpiderInvestigateServerRpc(Vector3 pos)
    {
        MP_SpiderAI spider = FindFirstObjectByType<MP_SpiderAI>();
        if (spider != null && spider.IsSpawned)
            spider.InvestigatePosition(pos);
    }

    public override void OnNetworkSpawn()
    {
        if (pausePanel != null) pausePanel.SetActive(false);
        if (gameOverPanel != null) gameOverPanel.SetActive(false);
        if (completionPanel != null) completionPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused) PauseGame();
            else ResumeGame();
        }
    }

    // ---------------- Pause ----------------
    // Multiplayer — NO timeScale, NO AudioListener pause
    public void PauseGame()
    {
        isPaused = true;
        pausePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayPauseOpen();
    }

    public void ResumeGame()
    {
        isPaused = false;
        pausePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayPauseClose();
    }

    // ---------------- You Died ----------------
    public void ShowYouDied(float countdownSeconds, System.Action onComplete)
    {
        if (gameOverPanel != null)
        {
            gameOverPanel.SetActive(true);
            gameOverPanel.GetComponent<SpringPanel>().PlayDropBounce();

            if (gameOverTimeText != null) gameOverTimeText.gameObject.SetActive(false);
            if (gameOverTerminalText != null) gameOverTerminalText.gameObject.SetActive(false);
        }

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (SoundManager.Instance != null) SoundManager.Instance.PlayGameOver();

        StartCoroutine(YouDiedCountdown(countdownSeconds, onComplete));
    }

    IEnumerator YouDiedCountdown(float seconds, System.Action onComplete)
    {
        yield return new WaitForSeconds(seconds);
        onComplete?.Invoke();
    }

    public void HideYouDied()
    {
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (gameOverTimeText != null) gameOverTimeText.gameObject.SetActive(true);
        if (gameOverTerminalText != null) gameOverTerminalText.gameObject.SetActive(true);

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    // ---------------- Game Over ----------------
    [ServerRpc(RequireOwnership = false)]
    public void TriggerGameOverServerRpc()
    {
        TriggerGameOverClientRpc();
    }

    [ClientRpc]
    void TriggerGameOverClientRpc()
    {
        gameOverPanel.SetActive(true);
        gameOverPanel.GetComponent<SpringPanel>().PlayDropBounce();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (gameOverTimeText != null) gameOverTimeText.gameObject.SetActive(false);
        if (gameOverTerminalText != null) gameOverTerminalText.gameObject.SetActive(false);
    }

    // ---------------- Completion ----------------
    [ServerRpc(RequireOwnership = false)]
    public void TriggerCompletionServerRpc()
    {
        TriggerCompletionClientRpc();
    }

    [ClientRpc]
    void TriggerCompletionClientRpc()
    {
        if (gameTimer != null) gameTimer.StopTimer();
        completionPanel.SetActive(true);
        completionPanel.GetComponent<SpringPanel>().PlayDropBounce();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        float finalTime = gameTimer != null ? gameTimer.GetFinalTime() : 0f;
        completionTimeText.text = "Time: " + FormatTime(finalTime);
        completionTerminalText.text =
            MP_TerminalManager.Instance.completedTerminals.Value +
            " / " + MP_TerminalManager.Instance.totalTerminals;
        SaveLevelProgress();
    }

    public void CompletionContinue()
    {
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(level2SceneName);
    }

    public void QuitToMainMenu()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerPrefs.SetInt("OpenCampaign", 1);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    private void SaveLevelProgress()
    {
        PlayerPrefs.SetInt("level1_completed", 1);
        if (gameTimer != null)
            PlayerPrefs.SetFloat("level1_best_time", gameTimer.GetFinalTime());
        PlayerPrefs.Save();

        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser != null)
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            DocumentReference userDoc = db.Collection("users").Document(currentUser.UserId);
            Dictionary<string, object> progressData = new Dictionary<string, object>
            {
                { "level1_completed", true },
                { "level1_best_time", gameTimer.GetFinalTime() }
            };
            userDoc.UpdateAsync(progressData).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    Debug.LogError("Failed to save progress: " + task.Exception);
                else
                    Debug.Log("Progress saved for: " + currentUser.UserId);
            });
        }
    }

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return $"{minutes:00}:{seconds:00}";
    }

    public void SetInputLocked(bool value)
    {
        inputLocked = value;
    }
}