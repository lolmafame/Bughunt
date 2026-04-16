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
        "Level 1 Final", "Level 2 Final", "Level 3 Final", "Level 4 Final", "Level 5 Final Final"
    };

    [Header("Level Settings")]
    [Tooltip("Set to 0 for the Tutorial, or 1-5 for regular levels. Used to build save keys.")]
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

    void Start()
    {
        // Run migration once as soon as we have a logged-in user.
        // Safe to call every scene load — it is guarded by a one-time PlayerPrefs flag.
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        if (user != null)
            MigrateLegacyProgress(user);
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

        // currentLevel == 0 → Tutorial → loads levelSceneNames[0] (Level 1)
        // currentLevel == 1 → Level 1  → loads levelSceneNames[1] (Level 2)
        // etc.
        if (currentLevel < 5)
        {
            int nextIndex = currentLevel; // 0-based array index of the next scene

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
                Debug.LogWarning($"No scene name set for the level after {currentLevel}. Returning to Main Menu.");
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
    // LEGACY MIGRATION  (runs once per user)
    // ─────────────────────────────────────────────
    // Old keys (no language prefix):  level1_completed, level1_best_time …
    // New keys (python prefix)     :  python_level1_completed, python_level1_best_time …
    //
    // A one-time guard flag is stored in PlayerPrefs so this never runs twice:
    //   "legacy_migrated_{userId}" = 1

    private void MigrateLegacyProgress(FirebaseUser user)
    {
        string userId = user.UserId;
        string guardKey = $"legacy_migrated_{userId}";

        // Already migrated — skip.
        if (PlayerPrefs.GetInt(guardKey, 0) == 1) return;

        Debug.Log($"[Migration] Starting legacy → python key migration for user {userId}");

        // ── 1. PlayerPrefs (local) migration ─────────────────────────────────
        bool anyLocalData = false;

        for (int lvl = 1; lvl <= 5; lvl++)
        {
            // Legacy keys (no language prefix)
            string legacyCompleted = $"level{lvl}_completed_{userId}";
            string legacyBestTime = $"level{lvl}_best_time_{userId}";

            // New keys
            string newCompleted = $"python_level{lvl}_completed_{userId}";
            string newBestTime = $"python_level{lvl}_best_time_{userId}";

            if (PlayerPrefs.HasKey(legacyCompleted))
            {
                anyLocalData = true;

                // Copy completed flag — keep the better value if new key already exists
                PlayerPrefs.SetInt(newCompleted, 1);
                PlayerPrefs.DeleteKey(legacyCompleted);

                Debug.Log($"[Migration] PlayerPrefs: level{lvl}_completed → python_level{lvl}_completed");
            }

            if (PlayerPrefs.HasKey(legacyBestTime))
            {
                anyLocalData = true;

                float legacyTime = PlayerPrefs.GetFloat(legacyBestTime);
                float currentNew = PlayerPrefs.GetFloat(newBestTime, float.MaxValue);

                // Keep the shorter (better) time between legacy and any existing new value
                if (legacyTime < currentNew)
                    PlayerPrefs.SetFloat(newBestTime, legacyTime);

                PlayerPrefs.DeleteKey(legacyBestTime);

                Debug.Log($"[Migration] PlayerPrefs: level{lvl}_best_time → python_level{lvl}_best_time ({legacyTime:F2}s)");
            }
        }

        // Legacy "all done" flag
        string legacyAllDone = $"all_levels_completed_{userId}";
        if (PlayerPrefs.HasKey(legacyAllDone))
        {
            anyLocalData = true;
            PlayerPrefs.SetInt($"python_all_levels_completed_{userId}", 1);
            PlayerPrefs.DeleteKey(legacyAllDone);
            Debug.Log("[Migration] PlayerPrefs: all_levels_completed → python_all_levels_completed");
        }

        if (anyLocalData) PlayerPrefs.Save();

        // Mark migration done locally so we never repeat it
        PlayerPrefs.SetInt(guardKey, 1);
        PlayerPrefs.Save();

        // ── 2. Firestore (cloud) migration ────────────────────────────────────
        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference userDoc = db.Collection("users").Document(userId);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("[Migration] Could not read Firestore doc: " + task.Exception);
                return;
            }

            DocumentSnapshot snap = task.Result;
            if (!snap.Exists) return;

            Dictionary<string, object> writeData = new Dictionary<string, object>();
            List<string> deleteFields = new List<string>();

            for (int lvl = 1; lvl <= 5; lvl++)
            {
                string legacyCompleted = $"level{lvl}_completed";
                string legacyBestTime = $"level{lvl}_best_time";
                string newCompleted = $"python_level{lvl}_completed";
                string newBestTime = $"python_level{lvl}_best_time";

                if (snap.TryGetValue(legacyCompleted, out bool completed))
                {
                    // Only write if new field not already set
                    if (!snap.ContainsField(newCompleted))
                        writeData[newCompleted] = completed;

                    deleteFields.Add(legacyCompleted);
                }

                if (snap.TryGetValue(legacyBestTime, out double bestTime))
                {
                    if (!snap.ContainsField(newBestTime) ||
                        (snap.TryGetValue(newBestTime, out double existingNew) && bestTime < existingNew))
                    {
                        writeData[newBestTime] = bestTime;
                    }

                    deleteFields.Add(legacyBestTime);
                }
            }

            // Legacy "all done" cloud field
            if (snap.TryGetValue("all_levels_completed", out bool allDone))
            {
                if (!snap.ContainsField("python_all_levels_completed"))
                    writeData["python_all_levels_completed"] = allDone;

                deleteFields.Add("all_levels_completed");
            }

            if (writeData.Count == 0 && deleteFields.Count == 0)
            {
                Debug.Log("[Migration] Firestore: no legacy fields found, nothing to migrate.");
                return;
            }

            // Add FieldValue.Delete entries for each legacy field
            foreach (string field in deleteFields)
                writeData[field] = FieldValue.Delete;

            userDoc.SetAsync(writeData, SetOptions.MergeAll)
                   .ContinueWithOnMainThread(writeTask =>
                   {
                       if (writeTask.IsFaulted || writeTask.IsCanceled)
                           Debug.LogError("[Migration] Firestore write failed: " + writeTask.Exception);
                       else
                           Debug.Log($"[Migration] Firestore: migrated {deleteFields.Count} legacy field(s) to python_ prefix.");
                   });
        });
    }

    // ─────────────────────────────────────────────
    // SAVE LEVEL PROGRESS
    // ─────────────────────────────────────────────
    // Key format examples (language = Python, level = 1):
    //   Firestore field : "python_level1_completed"
    //   Firestore field : "python_level1_best_time"
    //   PlayerPrefs key : "python_level1_completed_{userId}"
    //   PlayerPrefs key : "python_level1_best_time_{userId}"
    //
    // Tutorial (currentLevel = 0) uses a special key with no best time:
    //   Firestore field : "python_tutorial_completed"
    //   PlayerPrefs key : "python_tutorial_completed_{userId}"

    private void SaveLevelProgress()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;
        string userId = currentUser != null ? currentUser.UserId : "guest";

        string lang = LanguageKey; // e.g. "python"

        // ── Build language-prefixed keys ──────────────────────────────────────
        // Tutorial (currentLevel == 0) uses "tutorial" instead of "level0"
        bool isTutorial = (currentLevel == 0);

        string completedKey = isTutorial
            ? $"{lang}_tutorial_completed"                        // e.g. "python_tutorial_completed"
            : $"{lang}_level{currentLevel}_completed";            // e.g. "python_level1_completed"

        string bestTimeKey = isTutorial
            ? null                                                // Tutorial has no best time
            : $"{lang}_level{currentLevel}_best_time";           // e.g. "python_level1_best_time"

        string allDoneKey = $"{lang}_all_levels_completed";      // e.g. "python_all_levels_completed"

        string localCompletedKey = $"{completedKey}_{userId}";
        string localBestTimeKey = bestTimeKey != null ? $"{bestTimeKey}_{userId}" : null;
        string localAllDoneKey = $"{allDoneKey}_{userId}";

        // ── Local save (PlayerPrefs) ──────────────────────────────────────────
        PlayerPrefs.SetInt(localCompletedKey, 1);

        bool isNewBestTime = false;

        if (!isTutorial)
        {
            // Only regular levels track best time
            float newTime = gameTimer != null ? gameTimer.GetFinalTime() : 0f;
            float previousBestTime = PlayerPrefs.GetFloat(localBestTimeKey, float.MaxValue);
            isNewBestTime = newTime < previousBestTime;

            if (isNewBestTime)
            {
                PlayerPrefs.SetFloat(localBestTimeKey, newTime);
                Debug.Log($"[{lang}] Level {currentLevel}: New best time! {FormatTime(newTime)}");
            }
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
                // No best time entry for tutorial
            };

            if (!isTutorial && isNewBestTime)
            {
                float newTime = gameTimer != null ? gameTimer.GetFinalTime() : 0f;
                progressData[bestTimeKey] = newTime;
            }

            if (currentLevel == 5)
                progressData[allDoneKey] = true;

            string levelLabel = isTutorial ? "Tutorial" : $"Level {currentLevel}";

            userDoc.SetAsync(progressData, SetOptions.MergeAll)
                   .ContinueWithOnMainThread(task =>
                   {
                       if (task.IsFaulted || task.IsCanceled)
                           Debug.LogError($"Failed to save [{lang}] {levelLabel} progress: " + task.Exception);
                       else
                           Debug.Log($"Saved [{lang}] {levelLabel} completion to cloud.");
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