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

    private string GetUserId()
    {
        FirebaseUser user = FirebaseAuth.DefaultInstance.CurrentUser;
        return user != null ? user.UserId : "guest";
    }

    void Start()
    {
        levels = new LevelUI[] { level1UI, level2UI, level3UI, level4UI, level5UI };

        // 1. Lock everything down initially while we wait for Firebase
        for (int i = 0; i < levels.Length; i++)
        {
            if (levels[i] != null) levels[i].SetState(LevelState.Closed);
        }

        // 2. ONLY fetch from Firebase. Local data is strictly a fallback now.
        FetchLevelProgress();
    }

    private void FetchLevelProgress()
    {
        FirebaseUser currentUser = FirebaseAuth.DefaultInstance.CurrentUser;

        if (currentUser != null)
        {
            string userId = currentUser.UserId;
            FirebaseFirestore db = FirebaseFirestore.DefaultInstance;
            DocumentReference userDoc = db.Collection("users").Document(userId);

            // Attempt to read from Cloud FIRST
            userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                // IF OFFLINE: Task will fault. Fallback to local save for this specific account.
                if (task.IsFaulted || task.IsCanceled)
                {
                    Debug.LogWarning("Lost connection to Firebase. Falling back to local save for user: " + userId);
                    LoadFromOfflineBackup(userId);
                    return;
                }

                // IF ONLINE: Firebase is King. 
                DocumentSnapshot snapshot = task.Result;

                // Open Level 1 by default
                if (levels[0] != null) levels[0].SetState(LevelState.Open);

                if (snapshot.Exists)
                {
                    for (int i = 0; i < levels.Length; i++)
                    {
                        int levelNum = i + 1;
                        string dbLevelKey = $"level{levelNum}_completed";
                        string localLevelKey = $"level{levelNum}_completed_{userId}";

                        // Check cloud status
                        if (snapshot.ContainsField(dbLevelKey) && snapshot.GetValue<bool>(dbLevelKey))
                        {
                            if (levels[i] != null) levels[i].SetState(LevelState.Complete);

                            // Cloud says complete, so force local cache to match
                            PlayerPrefs.SetInt(localLevelKey, 1);

                            // Unlock next level
                            if (i + 1 < levels.Length && levels[i + 1] != null)
                            {
                                string nextDbKey = $"level{levelNum + 1}_completed";
                                bool isNextComplete = snapshot.ContainsField(nextDbKey) && snapshot.GetValue<bool>(nextDbKey);

                                if (!isNextComplete)
                                {
                                    levels[i + 1].SetState(LevelState.Open);
                                }
                            }
                        }
                        else
                        {
                            // Cloud says NOT complete. Force local cache to match (prevents local cheating/bleeding)
                            PlayerPrefs.SetInt(localLevelKey, 0);
                        }
                    }
                    PlayerPrefs.Save();
                }
                else
                {
                    Debug.Log("New user in database. Starting fresh.");
                    // Optional: Wipe local cache for this user just to be absolutely sure it's a fresh start
                }
            });
        }
        else
        {
            Debug.LogWarning("No user logged in. Showing default level 1 open.");
            if (levels[0] != null) levels[0].SetState(LevelState.Open);
        }
    }

    // This method ONLY runs if Firebase cannot be reached
    private void LoadFromOfflineBackup(string userId)
    {
        if (levels[0] != null) levels[0].SetState(LevelState.Open);

        for (int i = 0; i < levels.Length; i++)
        {
            int levelNum = i + 1;
            bool isCompleted = PlayerPrefs.GetInt($"level{levelNum}_completed_{userId}", 0) == 1;

            if (isCompleted)
            {
                if (levels[i] != null) levels[i].SetState(LevelState.Complete);

                if (i + 1 < levels.Length && levels[i + 1] != null)
                {
                    bool isNextCompleted = PlayerPrefs.GetInt($"level{levelNum + 1}_completed_{userId}", 0) == 1;
                    if (!isNextCompleted)
                    {
                        levels[i + 1].SetState(LevelState.Open);
                    }
                }
            }
        }
    }
}