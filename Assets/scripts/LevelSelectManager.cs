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

    void Start()
    {
        // Set default states before data loads
        if (level1UI != null) level1UI.SetState(LevelState.Open);
        if (level2UI != null) level2UI.SetState(LevelState.Closed);
        if (level3UI != null) level3UI.SetState(LevelState.Closed);
        if (level4UI != null) level4UI.SetState(LevelState.Closed);
        if (level5UI != null) level5UI.SetState(LevelState.Closed);

        // Check local PlayerPrefs progress
        bool level1Complete = PlayerPrefs.GetInt("level1_completed", 0) == 1;

        if (level1Complete)
        {
            if (level1UI != null) level1UI.SetState(LevelState.Complete);
            if (level2UI != null) level2UI.SetState(LevelState.Open);
        }

        // Fetch progress to update the UI
        FetchLevelProgress();
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
                    // Check if Level 1 is completed
                    if (snapshot.ContainsField("level1_completed") && snapshot.GetValue<bool>("level1_completed"))
                    {
                        // Level 1 is done, so mark it complete and open Level 2!
                        if (level1UI != null) level1UI.SetState(LevelState.Complete);
                        if (level2UI != null) level2UI.SetState(LevelState.Open);

                        // Sync with local PlayerPrefs
                        PlayerPrefs.SetInt("level1_completed", 1);
                        PlayerPrefs.Save();
                    }
                }
            });
        }
        else
        {
            Debug.LogWarning("No user logged in. Showing default level states.");
        }
    }
}