using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;

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
    public string mainMenuSceneName = "MainMenu"; // match your scene name exactly

    [Header("Spawn Point")]
    public Transform spawnPoint; // drag your spawn point object here in Inspector

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
        pausePanel.SetActive(true);
        if (gameTimer != null) gameTimer.StopTimer();
    }

    // Called by "Continue" button in Pause panel
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);
        if (gameTimer != null) gameTimer.StartTimer();
    }

    // Called by "Settings" button in Pause panel
    public void OpenSettings()
    {
        // Hook up your settings panel here when ready
        Debug.Log("Settings opened");
    }

    // ---------------- Game Over ----------------
    public void GameOver()
    {
        Time.timeScale = 0f;
        gameTimer.StopTimer();
        gameOverPanel.SetActive(true);
        gameOverPanel.GetComponent<SpringPanel>().PlayDropBounce();

        float finalTime = gameTimer.GetFinalTime();
        gameOverTimeText.text = "Time: " + FormatTime(finalTime);
        gameOverTerminalText.text = "Terminals: " +
            TerminalManager.Instance.GetCompletedTerminals() +
            " / " + TerminalManager.Instance.totalTerminals;
    }

    // Called by "Retry" button in Game Over panel
    public void RetryGame()
    {
        Time.timeScale = 1f;

        // Reset timer
        gameTimer.StopTimer();

        // Move player to spawn point
        GameObject player = GameObject.FindGameObjectWithTag("Player");
        if (player != null && spawnPoint != null)
            player.transform.position = spawnPoint.position;

        // Reload the current scene (resets terminals, timer, everything)
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }

    // ---------------- Completion ----------------
    public void Completion()
    {
        Time.timeScale = 0f;
        gameTimer.StopTimer();
        completionPanel.SetActive(true);
        completionPanel.GetComponent<SpringPanel>().PlayDropBounce();

        float finalTime = gameTimer.GetFinalTime();
        completionTimeText.text = "Time: " + FormatTime(finalTime);
        completionTerminalText.text = "Terminals: " +
            TerminalManager.Instance.GetCompletedTerminals() +
            " / " + TerminalManager.Instance.totalTerminals;
    }

    // Called by "Continue" button in Completion panel
    public void CompletionContinue()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("OpenCampaign", 1);
        SceneManager.LoadScene(mainMenuSceneName);
    }

    // Called by "Quit" button in Pause OR Game Over panel
    public void QuitToMainMenu()
    {
        Time.timeScale = 1f;
        PlayerPrefs.SetInt("OpenCampaign", 1);
        SceneManager.LoadScene(mainMenuSceneName);
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