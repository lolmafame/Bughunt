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

    private LevelUI[] levels;

    void Start()
    {
        // Put all level UIs into an array for easy looping
        levels = new LevelUI[] { level1UI, level2UI, level3UI, level4UI, level5UI };

        // 1. Set default states: Level 1 Open, the rest Closed
        if (levels[0] != null) levels[0].SetState(LevelState.Open);
        for (int i = 1; i < levels.Length; i++)
        {
            if (levels[i] != null) levels[i].SetState(LevelState.Closed);
        }

        // 2. Check local PlayerPrefs progress immediately for quick UI loading
        UpdateUIFromLocalPrefs();

        // 3. Fetch cloud progress to update the UI with remote data
        FetchLevelProgress();
    }

    private void UpdateUIFromLocalPrefs()
    {
        for (int i = 0; i < levels.Length; i++)
        {
            int levelNum = i + 1; // 1-based index for your keys (level1, level2, etc.)
            bool isCompleted = PlayerPrefs.GetInt($"level{levelNum}_completed", 0) == 1;

            if (isCompleted)
            {
                // Mark current level as Complete
                if (levels[i] != null) levels[i].SetState(LevelState.Complete);

                // Open the next level if it exists and isn't already completed
                if (i + 1 < levels.Length && levels[i + 1] != null)
                {
                    bool isNextCompleted = PlayerPrefs.GetInt($"level{levelNum + 1}_completed", 0) == 1;
                    if (!isNextCompleted)
                    {
                        levels[i + 1].SetState(LevelState.Open);
                    }
                }
            }
        }
    }

    private void FetchLevelProgress()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (currentUser != null)
        {
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            DocumentReference userDoc = db.Collection("users").Document(currentUser.UserId);

            userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogError("Failed to fetch level progress: " + task.Exception);
                    return;
                }

                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists)
                {
                    for (int i = 0; i < levels.Length; i++)
                    {
                        int levelNum = i + 1;
                        string levelKey = $"level{levelNum}_completed";

                        // If this level is marked as completed in Firestore
                        if (snapshot.ContainsField(levelKey) && snapshot.GetValue<bool>(levelKey))
                        {
                            // Mark complete in UI
                            if (levels[i] != null) levels[i].SetState(LevelState.Complete);

                            // Sync local PlayerPrefs as a backup
                            PlayerPrefs.SetInt(levelKey, 1);

                            // Open the next level if it exists
                            if (i + 1 < levels.Length && levels[i + 1] != null)
                            {
                                string nextLevelKey = $"level{levelNum + 1}_completed";
                                bool isNextComplete = snapshot.ContainsField(nextLevelKey) && snapshot.GetValue<bool>(nextLevelKey);

                                // Only set to Open if the player hasn't already completed it
                                if (!isNextComplete)
                                {
                                    levels[i + 1].SetState(LevelState.Open);
                                }
                            }
                        }
                    }
                    PlayerPrefs.Save();
                }
            });
        }
        else
        {
            Debug.LogWarning("No user logged in. Showing default level states.");
        }
    }
}