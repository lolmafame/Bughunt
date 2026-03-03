using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class AccountManager : MonoBehaviour
{
    [Header("UI Display Elements")]
    [SerializeField] private TMP_Text displayUsernameText;
    [SerializeField] private TMP_Text displayEmailText;
    [Tooltip("Drag the Account Panel here so we can hide it on logout")]
    [SerializeField] private GameObject accountPanel; // Brought this back!

    [Header("Scene Transitions")]
    [Tooltip("Drag the Login Panel (with the SpringPanel script) here")]
    [SerializeField] private SpringPanel loginSpringPanel;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Awake()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        if (auth == null) auth = FirebaseAuth.DefaultInstance;
        if (db == null) db = FirebaseFirestore.DefaultInstance;
    }

    public void OpenAccountPanel()
    {
        InitializeFirebase();

        Debug.Log(">>> ACCOUNT MANAGER: OpenAccountPanel called!");

        if (auth.CurrentUser == null)
        {
            Debug.LogError(">>> ERROR: AccountManager tried to open, but no User is logged in!");
            return;
        }

        // We assume the animation/transition into this panel is handled elsewhere,
        // but we still need to populate the data.
        RefreshUserData();
    }

    public void RefreshUserData()
    {
        InitializeFirebase();
        FirebaseUser user = auth.CurrentUser;
        if (user == null) return;

        Debug.Log($">>> ACCOUNT MANAGER: Loading data for user: {user.UserId}");

        // --- Update Email ---
        if (displayEmailText != null)
        {
            displayEmailText.text = string.IsNullOrEmpty(user.Email)
                ? "Guest ID: " + user.UserId.Substring(0, 6)
                : user.Email;
        }

        // --- Update Username ---
        if (displayUsernameText != null)
        {
            DocumentReference userDoc = db.Collection("users").Document(user.UserId);
            userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (task.IsFaulted)
                {
                    Debug.LogError("Failed to fetch username.");
                    displayUsernameText.text = "Error";
                    return;
                }

                DocumentSnapshot snapshot = task.Result;
                if (snapshot.Exists && snapshot.ContainsField("username"))
                {
                    string name = snapshot.GetValue<string>("username");
                    displayUsernameText.text = name;
                    Debug.Log(">>> ACCOUNT MANAGER: Username found: " + name);
                }
                else
                {
                    displayUsernameText.text = "Player_" + user.UserId.Substring(0, 4);
                }
            });
        }
    }

    public void OnLogoutClicked()
    {
        // 1. Sign out of Firebase
        if (auth != null && auth.CurrentUser != null)
        {
            auth.SignOut();
            Debug.Log(">>> ACCOUNT MANAGER: User logged out successfully.");
        }

        // 2. Hide the Account Panel instantly
        if (accountPanel != null)
        {
            accountPanel.SetActive(false);
        }

        // 3. Trigger the animated Login Panel to drop down
        if (loginSpringPanel != null)
        {
            loginSpringPanel.PlayDropBounce();
        }
        else
        {
            Debug.LogWarning(">>> WARNING: loginSpringPanel is not assigned in the Inspector!");
        }
    }
}