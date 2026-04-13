using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

// ── Language options shown in the Inspector dropdown ──────────────────────────
public enum LevelLanguage
{
    Python,
    Javascript,
    CSharp,
    Java,
    CPlusPlus
}

public class GameManager : MonoBehaviour
{
    public static GameManager Instance;

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

    [Tooltip("Exact scene names in order: [0]=Level1, [1]=Level2, ... [4]=Level5")]
    public string[] levelSceneNames = new string[5]
    {
        "level 1 updated", "level 2", "level 3", "level 4", "level 5"
    };

    [Header("Level Settings")]
    [Tooltip("Set this to 1-5 in the Inspector for each level scene.")]
    public int currentLevel = 1;

    [Tooltip("Select the programming language taught in this level. Used as a prefix for all save keys.")]
    public LevelLanguage levelLanguage = LevelLanguage.Python;

    [Header("Spawn Point")]
    public Transform spawnPoint;

    private bool isPaused = false;
    private bool inputLocked = false;

    // ── Converts the enum to a lowercase string safe for use in save keys ─────
    // CSharp → "csharp" | CPlusPlus → "cplusplus" | others → lowercase name
    private string LanguageKey => levelLanguage.ToString().ToLower();

    void Awake()
    {
        Instance = this;
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        completionPanel.SetActive(false);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused) PauseGame();
            else ResumeGame();
            return;
        }

        if (inputLocked) return;
    }

    // ─────────────────────────────────────────────
    // PAUSE
    // ─────────────────────────────────────────────

    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SoundManager.Instance.PlayPauseOpen();
        AudioListener.pause = true;
        pausePanel.SetActive(true);
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        if (gameTimer != null) gameTimer.StopTimer();
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        AudioListener.pause = false;
        pausePanel.SetActive(false);
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        if (gameTimer != null) gameTimer.StartTimer();
        SoundManager.Instance.PlayPauseClose();
    }

    public void OpenSettings()
    {
        Debug.Log("Settings opened");
    }

    // ─────────────────────────────────────────────
    // GAME OVER
    // ─────────────────────────────────────────────

    public void GameOver()
    {
        Time.timeScale = 0f;
        gameTimer.StopTimer();
        gameOverPanel.SetActive(true);
        gameOverPanel.GetComponent<SpringPanel>().PlayDropBounce();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float finalTime = gameTimer.GetFinalTime();
        gameOverTimeText.text = "Time: " + FormatTime(finalTime);
        gameOverTerminalText.text = "Terminals: " +
            TerminalManager.Instance.GetCompletedTerminals() +
            " / " + TerminalManager.Instance.totalTerminals;
    }

    public void RetryGame()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        gameTimer.StopTimer();

        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && spawnPoint != null)
            player.transform.position = spawnPoint.position;

        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ─────────────────────────────────────────────
    // COMPLETION
    // ─────────────────────────────────────────────

    private bool completionTriggered = false;

    public void Completion()
    {
        Debug.Log("Completion() CALLED");
        if (completionTriggered) return;
        completionTriggered = true;

        Time.timeScale = 0f;
        gameTimer.StopTimer();
        completionPanel.SetActive(true);
        completionPanel.GetComponent<SpringPanel>().PlayDropBounce();
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        float finalTime = gameTimer.GetFinalTime();
        completionTimeText.text = "Time: " + FormatTime(finalTime);
        completionTerminalText.text = "Terminals: " +
            TerminalManager.Instance.GetCompletedTerminals() +
            " / " + TerminalManager.Instance.totalTerminals;

        SaveLevelProgress();
    }

    public void CompletionContinue()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (currentLevel < 5)
        {
            int nextIndex = currentLevel; // 1-based level → 0-based array index of next level

            if (nextIndex < levelSceneNames.Length && !string.IsNullOrEmpty(levelSceneNames[nextIndex]))
            {
                string nextScene = levelSceneNames[nextIndex];

                if (Application.CanStreamedLevelBeLoaded(nextScene))
                    SceneManager.LoadScene(nextScene);
                else
                {
                    Debug.LogWarning($"Scene '{nextScene}' not found in Build Settings. Returning to Main Menu.");
                    SceneManager.LoadScene(mainMenuSceneName);
                }
            }
            else
            {
                Debug.LogWarning($"No scene name set for Level {currentLevel + 1}. Returning to Main Menu.");
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }
        else
        {
            PlayerPrefs.SetInt("OpenCampaign", 1);
            SceneManager.LoadScene(mainMenuSceneName);
        }
    }

    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        PlayerPrefs.SetInt("OpenCampaign", 1);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // ─────────────────────────────────────────────
    // SAVE LEVEL PROGRESS
    // ─────────────────────────────────────────────
    // Key format examples (language = Python, level = 1):
    //   Firestore field : "python_level1_completed"
    //   Firestore field : "python_level1_best_time"
    //   PlayerPrefs key : "python_level1_completed_{userId}"
    //   PlayerPrefs key : "python_level1_best_time_{userId}"

    private void SaveLevelProgress()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        string userId = currentUser != null ? currentUser.UserId : "guest";

        // ── Build language-prefixed keys ──────────────────────────────────────
        string lang = LanguageKey;                                   // e.g. "python"
        string completedKey = $"{lang}_level{currentLevel}_completed";       // e.g. "python_level1_completed"
        string bestTimeKey = $"{lang}_level{currentLevel}_best_time";       // e.g. "python_level1_best_time"
        string allDoneKey = $"{lang}_all_levels_completed";                // e.g. "python_all_levels_completed"

        string localCompletedKey = $"{completedKey}_{userId}";
        string localBestTimeKey = $"{bestTimeKey}_{userId}";
        string localAllDoneKey = $"{allDoneKey}_{userId}";

        float newTime = gameTimer != null ? gameTimer.GetFinalTime() : 0f;

        // ── Local save (PlayerPrefs) ──────────────────────────────────────────
        float previousBestTime = PlayerPrefs.GetFloat(localBestTimeKey, float.MaxValue);
        bool isNewBestTime = newTime < previousBestTime;

        PlayerPrefs.SetInt(localCompletedKey, 1);

        if (isNewBestTime)
        {
            PlayerPrefs.SetFloat(localBestTimeKey, newTime);
            Debug.Log($"[{lang}] Level {currentLevel}: New best time! {FormatTime(newTime)}");
        }

        if (currentLevel == 5)
            PlayerPrefs.SetInt(localAllDoneKey, 1);

        PlayerPrefs.Save();

        // ── Firestore save ────────────────────────────────────────────────────
        if (currentUser != null)
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            DocumentReference userDoc = db.Collection("users").Document(userId);

            Dictionary<string, object> progressData = new Dictionary<string, object>
            {
                { completedKey, true }
            };

            if (isNewBestTime)
                progressData[bestTimeKey] = newTime;

            if (currentLevel == 5)
                progressData[allDoneKey] = true;

            userDoc.SetAsync(progressData, SetOptions.MergeAll)
                   .ContinueWithOnMainThread(task =>
                   {
                       if (task.IsFaulted || task.IsCanceled)
                           Debug.LogError($"Failed to save [{lang}] level {currentLevel} progress: " + task.Exception);
                       else
                           Debug.Log($"Saved [{lang}] Level {currentLevel} completion to cloud.");
                   });
        }
    }

    // ─────────────────────────────────────────────
    // UTILITIES
    // ─────────────────────────────────────────────

    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    public void SetInputLocked(bool value)
    {
        inputLocked = value;
    }
}