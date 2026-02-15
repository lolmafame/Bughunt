using UnityEngine;
using UnityEngine.UI;

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


    private bool isPaused = false;

    void Awake()
    {
        Instance = this;

        // Ensure all panels are off at start
        pausePanel.SetActive(false);
        gameOverPanel.SetActive(false);
        completionPanel.SetActive(false);
    }

    void Update()
    {
        // Toggle pause with Escape
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            if (!isPaused) PauseGame();
            else ResumeGame();
        }
    }

    // ---------------- Pause ----------------
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f;
        pausePanel.SetActive(true);

        if (gameTimer != null)
            gameTimer.StopTimer(); // stop timer while paused
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pausePanel.SetActive(false);

        if (gameTimer != null)
            gameTimer.StartTimer(); // resume timer
    }

    // ---------------- Game Over ----------------
    public void GameOver()
    {
        Time.timeScale = 0f;

        gameTimer.StopTimer();
        gameOverPanel.SetActive(true);
        gameOverPanel.GetComponent<SpringPanel>().PlayDropBounce();


        float finalTime = gameTimer.GetFinalTime();

        int minutes = Mathf.FloorToInt(finalTime / 60f);
        int seconds = Mathf.FloorToInt(finalTime % 60f);

        gameOverTimeText.text = "Time: " + string.Format("{0:00}:{1:00}", minutes, seconds);
        gameOverTerminalText.text = "Terminals: " +
            TerminalManager.Instance.GetCompletedTerminals() +
            " / " + TerminalManager.Instance.totalTerminals;
    }


    // ---------------- Completion ----------------
    public void Completion()
    {
        Time.timeScale = 0f;

        gameTimer.StopTimer();

        completionPanel.SetActive(true);
        completionPanel.GetComponent<SpringPanel>().PlayDropBounce();

        float finalTime = gameTimer.GetFinalTime();

        int minutes = Mathf.FloorToInt(finalTime / 60f);
        int seconds = Mathf.FloorToInt(finalTime % 60f);

        completionTimeText.text = "Time: " + string.Format("{0:00}:{1:00}", minutes, seconds);
        completionTerminalText.text = "Terminals: " +
            TerminalManager.Instance.GetCompletedTerminals() +
            " / " + TerminalManager.Instance.totalTerminals;
    }

    // ---------------- Utilities ----------------
    string FormatTime(float time)
    {
        int minutes = Mathf.FloorToInt(time / 60f);
        int seconds = Mathf.FloorToInt(time % 60f);
        return string.Format("{0:00}:{1:00}", minutes, seconds);
    }
}
