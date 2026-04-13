using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class LevelSelectManager : MonoBehaviour
{
    [Header("Level UI References")]
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

    void Start()
    {
        levels = new LevelUI[] { level1UI, level2UI, level3UI, level4UI, level5UI };

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
            Debug.LogWarning(">>> LEVEL SELECT: No user logged in. Showing default level 1 open.");
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

            // Level 1 is always open
            if (levels[0] != null) levels[0].SetState(LevelState.Open);

            if (snapshot.Exists)
            {
                for (int i = 0; i < levels.Length; i++)
                {
                    int levelNum = i + 1;

                    // ── Language-prefixed keys (must match GameManager) ────────
                    // e.g. "python_level1_completed", "javascript_level2_completed"
                    string dbLevelKey = $"{lang}_level{levelNum}_completed";
                    string localLevelKey = $"{dbLevelKey}_{userId}";

                    bool isComplete = snapshot.ContainsField(dbLevelKey)
                                     && snapshot.GetValue<bool>(dbLevelKey);

                    if (isComplete)
                    {
                        if (levels[i] != null) levels[i].SetState(LevelState.Complete);

                        // Keep local cache in sync with cloud
                        PlayerPrefs.SetInt(localLevelKey, 1);

                        // Unlock next level if it isn't already complete
                        if (i + 1 < levels.Length && levels[i + 1] != null)
                        {
                            string nextDbKey = $"{lang}_level{levelNum + 1}_completed";
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

        if (levels[0] != null) levels[0].SetState(LevelState.Open);

        for (int i = 0; i < levels.Length; i++)
        {
            int levelNum = i + 1;
            string localLevelKey = $"{lang}_level{levelNum}_completed_{userId}";
            bool isCompleted = PlayerPrefs.GetInt(localLevelKey, 0) == 1;

            if (isCompleted)
            {
                if (levels[i] != null) levels[i].SetState(LevelState.Complete);

                if (i + 1 < levels.Length && levels[i + 1] != null)
                {
                    string nextLocalKey = $"{lang}_level{levelNum + 1}_completed_{userId}";
                    bool nextCompleted = PlayerPrefs.GetInt(nextLocalKey, 0) == 1;

                    if (!nextCompleted)
                        levels[i + 1].SetState(LevelState.Open);
                }
            }
        }

        Debug.Log($">>> LEVEL SELECT: Offline backup loaded for [{lang}] user [{userId}].");
    }
}