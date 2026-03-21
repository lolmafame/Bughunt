using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;

public class AccountManager : MonoBehaviour
{
    [Header("UI Display Elements")]
    [SerializeField] private TMP_Text displayUsernameText;
    [SerializeField] private TMP_Text displayEmailText;
    [SerializeField] private GameObject accountPanel;

    [Header("Scene Transitions")]
    [SerializeField] private GameObject loginPanelRoot;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    // CHANGED: Use Awake for earlier initialization
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
        // SAFETY: Ensure Firebase is loaded even if Awake didn't run yet
        InitializeFirebase();

        Debug.Log(">>> ACCOUNT MANAGER: OpenAccountPanel called!"); // Debug Proof

        if (auth.CurrentUser == null)
        {
            Debug.LogError(">>> ERROR: AccountManager tried to open, but no User is logged in!");
            return;
        }

        // 1. Hide Login UI / Show Account UI
        if (loginPanelRoot != null) loginPanelRoot.SetActive(false);

        if (accountPanel != null)
        {
            accountPanel.SetActive(true);
        }
        else
        {
            Debug.LogError(">>> ERROR: Account Panel GameObject is not assigned in Inspector!");
        }

        // 2. Populate the text fields
        RefreshUserData();
    }

    public void RefreshUserData()
    {
        InitializeFirebase(); // Double safety
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


    /// Executes ONLY the Firebase SignOut logic.
    public void SignOutFirebaseOnly()
    {
        InitializeFirebase();
        if (auth != null && auth.CurrentUser != null)
        {
            auth.SignOut();
            Debug.Log(">>> ACCOUNT MANAGER: Firebase SignOut complete.");
        }
    }
}