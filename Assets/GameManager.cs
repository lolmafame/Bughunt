using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

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

    [Header("Spawn Point")]
    public Transform spawnPoint;

    private bool isPaused = false;
    private bool inputLocked = false;

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
            if (!isPaused)
                PauseGame();
            else
                ResumeGame();
            return;
        }

        if (inputLocked) return;
    }

    // ---------------- Pause ----------------
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        SoundManager.Instance.PlayPauseOpen(); // ← play BEFORE pausing audio
        AudioListener.pause = true; // ← then pause audio
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
        SoundManager.Instance.PlayPauseClose(); // ← add here
    }

    public void OpenSettings()
    {
        Debug.Log("Settings opened");
    }

    // ---------------- Game Over ----------------
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

    // ---------------- Completion ----------------
    public void Completion()
    {
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
            int nextIndex = currentLevel; // 1-based level → 0-based array index of NEXT level

            // Validate: array slot exists and is not empty
            if (nextIndex < levelSceneNames.Length && !string.IsNullOrEmpty(levelSceneNames[nextIndex]))
            {
                string nextScene = levelSceneNames[nextIndex];

                // Validate: scene actually exists in Build Settings before loading
                if (Application.CanStreamedLevelBeLoaded(nextScene))
                {
                    SceneManager.LoadScene(nextScene);
                }
                else
                {
                    // Scene name is set but not found in Build Settings — safe fallback
                    Debug.LogWarning($"Scene '{nextScene}' not found in Build Settings. Returning to Main Menu.");
                    SceneManager.LoadScene(mainMenuSceneName);
                }
            }
            else
            {
                // Array slot is empty or out of range — safe fallback
                Debug.LogWarning($"No scene name set for Level {currentLevel + 1} in levelSceneNames. Returning to Main Menu.");
                SceneManager.LoadScene(mainMenuSceneName);
            }
        }
        else
        {
            // Level 5 completed — all levels done, return to main menu
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

    private void SaveLevelProgress()
    {
        string levelKey = $"level{currentLevel}_completed";
        string levelTimeKey = $"level{currentLevel}_best_time";
        float newTime = gameTimer != null ? gameTimer.GetFinalTime() : 0f;

        // --- Best Time Validation ---
        // Only update the time if it's better (lower) than the previously saved time.
        // float.MaxValue means no previous time exists yet — always save in that case.
        float previousBestTime = PlayerPrefs.GetFloat(levelTimeKey, float.MaxValue);
        bool isNewBestTime = newTime < previousBestTime;

        // --- PlayerPrefs (local backup) ---
        PlayerPrefs.SetInt(levelKey, 1); // always mark completed

        if (isNewBestTime)
        {
            PlayerPrefs.SetFloat(levelTimeKey, newTime);
            Debug.Log($"Level {currentLevel}: New best time! {FormatTime(newTime)} (previous: {FormatTime(previousBestTime)})");
        }
        else
        {
            Debug.Log($"Level {currentLevel}: Time {FormatTime(newTime)} did not beat best of {FormatTime(previousBestTime)}. Best time kept.");
        }

        if (currentLevel == 5)
        {
            PlayerPrefs.SetInt("all_levels_completed", 1);
            Debug.Log("All 5 levels completed!");
        }

        PlayerPrefs.Save();

        // --- Firestore ---
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        if (currentUser != null)
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            DocumentReference userDoc = db.Collection("users").Document(currentUser.UserId);

            Dictionary<string, object> progressData = new Dictionary<string, object>
            {
                { levelKey, true } // always mark completed in Firestore
            };

            // Only push the new time to Firestore if it beats the local best
            if (isNewBestTime)
                progressData[levelTimeKey] = newTime;

            if (currentLevel == 5)
                progressData["all_levels_completed"] = true;

            userDoc.UpdateAsync(progressData).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                    Debug.LogError("Failed to save level progress: " + task.Exception);
                else
                    Debug.Log($"Saved Level {currentLevel} completion for user: {currentUser.UserId}");
            });
        }
        else
        {
            Debug.LogWarning("No user logged in. Level progress not saved to database.");
        }
    }

    // ---------------- Utilities ----------------
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