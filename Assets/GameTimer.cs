using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    public Text timerText;

    float elapsedTime = 0f;
    bool timerRunning = true;

    void Start()
    {
        timerRunning = true; // start immediately, or call StartTimer() if needed
    }

    void Update()
    {
        if (!timerRunning) return;

        elapsedTime += Time.deltaTime;
        UpdateTimerUI();
    }

    void UpdateTimerUI()
    {
        int minutes = Mathf.FloorToInt(elapsedTime / 60f);
        int seconds = Mathf.FloorToInt(elapsedTime % 60f);

        timerText.text = string.Format("{0:00}:{1:00}", minutes, seconds);
    }

    // ---------------- PUBLIC FUNCTIONS ----------------

    public void StartTimer()
    {
        timerRunning = true;
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    public float GetFinalTime()
    {
        return elapsedTime;
    }
}
