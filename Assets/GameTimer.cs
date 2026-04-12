using UnityEngine;
using UnityEngine.UI;

public class GameTimer : MonoBehaviour
{
    public Text timerText;
    float elapsedTime = 0f;
    bool timerRunning = false; // CHANGED — don't auto start

    void Start()
    {
        timerRunning = false; // CHANGED — wait for game to start
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

    public void StartTimer()
    {
        timerRunning = true;
    }

    public void StopTimer()
    {
        timerRunning = false;
    }

    public void ResetTimer()
    {
        elapsedTime = 0f;
        timerRunning = false;
    }

    public float GetFinalTime()
    {
        return elapsedTime;
    }
}