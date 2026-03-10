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
    public string level2SceneName = "Level2";

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

        // Trigger the database save
        SaveLevelProgress();
    }

    public void CompletionContinue()
    {
        Time.timeScale = 1f;
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
        SceneManager.LoadScene(level2SceneName);
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
        // 1. Grab the currently logged-in user
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (currentUser != null)
        {
            // 2. Get a reference to the user's document in Firestore
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            DocumentReference userDoc = db.Collection("users").Document(currentUser.UserId);

            // 3. Prepare the data to update. 
            // We are marking Level 1 as complete, and as a bonus, saving their completion time!
            Dictionary<string, object> progressData = new Dictionary<string, object>
        {
            { "level1_completed", true },
            { "level1_best_time", gameTimer.GetFinalTime() }
        };

            // 4. Push the update to Firestore asynchronously
            userDoc.UpdateAsync(progressData).ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Failed to save level progress to database: " + task.Exception);
                }
                else
                {
                    Debug.Log("Successfully saved Level 1 completion to database for user: " + currentUser.UserId);
                }
            });
        }
        else
        {
            Debug.LogWarning("No user is currently logged in. Level progress will not be saved.");
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