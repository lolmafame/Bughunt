using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Level UI References")]
    public LevelUI tutorialUI;   // Index 0 — always open
    public LevelUI level1UI;
    public LevelUI level2UI;
    public LevelUI level3UI;
    public LevelUI level4UI;
    public LevelUI level5UI;

    [Header("Language Settings")]
    [Tooltip("Must match the LevelLanguage set on each level's GameManager.")]
    public LevelLanguage levelLanguage = LevelLanguage.Python;

    private LevelUI[] levels;

    // Converts enum to lowercase string for use in save keys (matches GameManager)
    private string LanguageKey => levelLanguage.ToString().ToLower();

    private string GetUserId()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        return user != null ? user.UserId : "guest";
    }

    // Returns the Firestore field key for a given levels[] index.
    // Index 0 → "python_tutorial_completed"
    // Index 1 → "python_level1_completed", etc.
    private string GetDbKey(string lang, int index)
    {
        return index == 0
            ? $"{lang}_tutorial_completed"
            : $"{lang}_level{index}_completed";
    }

    void Start()
    {
        levels = new LevelUI[] { tutorialUI, level1UI, level2UI, level3UI, level4UI, level5UI };

        // Lock everything down while we wait for Firebase
        for (int i = 0; i < levels.Length; i++)
            if (levels[i] != null) levels[i].SetState(LevelState.Closed);

        FetchLevelProgress();
    }

    // ─────────────────────────────────────────────
    // FIREBASE FETCH
    // ─────────────────────────────────────────────

    private void FetchLevelProgress()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (currentUser == null)
        {
            Debug.LogWarning(">>> LEVEL SELECT: No user logged in. Showing default tutorial open.");
            if (levels[0] != null) levels[0].SetState(LevelState.Open);
            return;
        }

        string userId = currentUser.UserId;
        string lang = LanguageKey;

        FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
        DocumentReference userDoc = db.Collection("users").Document(userId);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogWarning($">>> LEVEL SELECT: Firebase unreachable. Falling back to local save for [{lang}] user: {userId}");
                LoadFromOfflineBackup(userId);
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            // Tutorial is always open
            if (levels[0] != null) levels[0].SetState(LevelState.Open);

            if (snapshot.Exists)
            {
                for (int i = 0; i < levels.Length; i++)
                {
                    // ── Language-prefixed keys (must match GameManager) ────────
                    // i==0 → "python_tutorial_completed"
                    // i>=1 → "python_level{i}_completed"
                    string dbLevelKey = GetDbKey(lang, i);
                    string localLevelKey = $"{dbLevelKey}_{userId}";

                    bool isComplete = snapshot.ContainsField(dbLevelKey)
                                     && snapshot.GetValue<bool>(dbLevelKey);

                    if (isComplete)
                    {
                        if (levels[i] != null) levels[i].SetState(LevelState.Complete);

                        // Keep local cache in sync with cloud
                        PlayerPrefs.SetInt(localLevelKey, 1);

                        // Unlock the next level if it isn't already complete
                        if (i + 1 < levels.Length && levels[i + 1] != null)
                        {
                            string nextDbKey = GetDbKey(lang, i + 1);
                            bool nextComplete = snapshot.ContainsField(nextDbKey)
                                                && snapshot.GetValue<bool>(nextDbKey);

                            if (!nextComplete)
                                levels[i + 1].SetState(LevelState.Open);
                        }
                    }
                    else
                    {
                        // Cloud says not complete — override local to prevent cheating
                        PlayerPrefs.SetInt(localLevelKey, 0);
                    }
                }

                PlayerPrefs.Save();
                Debug.Log($">>> LEVEL SELECT: Loaded [{lang}] progress for user [{userId}].");
            }
            else
            {
                Debug.Log($">>> LEVEL SELECT: New user or no [{lang}] progress yet. Starting fresh.");
            }
        });
    }

    // ─────────────────────────────────────────────
    // OFFLINE FALLBACK
    // ─────────────────────────────────────────────

    /// <summary>Only runs when Firebase cannot be reached.</summary>
    private void LoadFromOfflineBackup(string userId)
    {
        string lang = LanguageKey;

        // Tutorial is always open
        if (levels[0] != null) levels[0].SetState(LevelState.Open);

        for (int i = 0; i < levels.Length; i++)
        {
            string localLevelKey = $"{GetDbKey(lang, i)}_{userId}";
            bool isCompleted = PlayerPrefs.GetInt(localLevelKey, 0) == 1;

            if (isCompleted)
            {
                if (levels[i] != null) levels[i].SetState(LevelState.Complete);

                if (i + 1 < levels.Length && levels[i + 1] != null)
                {
                    string nextLocalKey = $"{GetDbKey(lang, i + 1)}_{userId}";
                    bool nextCompleted = PlayerPrefs.GetInt(nextLocalKey, 0) == 1;

                    if (!nextCompleted)
                        levels[i + 1].SetState(LevelState.Open);
                }
            }
        }

        Debug.Log($">>> LEVEL SELECT: Offline backup loaded for [{lang}] user [{userId}].");
    }
}