using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System;

public class Cert : MonoBehaviour
{
    [Header("Certificate Info")]
    [SerializeField] private TMP_Text displayUsernameText;
    [SerializeField] private TMP_Text displayDateText;

    [Header("Unlock UI Elements")]
    [SerializeField] private GameObject certificateButton;
    [SerializeField] private GameObject incompleteTextObject;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    // Track which user we last loaded data FOR, so we never show stale data
    private string lastLoadedUserId = null;

    void Start()
    {
        InitializeFirebase();
        SetCurrentDate();
    }

    private void InitializeFirebase()
    {
        if (auth == null) auth = FirebaseAuth.DefaultInstance;
        if (db == null) db = FirebaseFirestore.DefaultInstance;

        // ✅ KEY FIX: Subscribe to auth state changes.
        // This fires immediately on Start AND fires again whenever accounts switch.
        auth.StateChanged += OnAuthStateChanged;
    }

    // ✅ This is now the SINGLE entry point for loading data.
    // It fires on login, logout, AND account switches.
    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        FirebaseUser currentUser = auth.CurrentUser;

        // If no user is logged in, OR it's a different user than before → wipe everything
        if (currentUser == null || currentUser.UserId != lastLoadedUserId)
        {
            Debug.Log($">>> CERT: Auth state changed. Resetting UI. New user: {currentUser?.UserId ?? "none"}");
            ResetUIToSafeDefault();
        }

        if (currentUser != null)
        {
            lastLoadedUserId = currentUser.UserId;
            SetCurrentDate();
            CheckCertificateStatus(currentUser);
        }
        else
        {
            // User logged out, clear the tracked ID
            lastLoadedUserId = null;
        }
    }

    // ✅ Always wipe the UI to the most restrictive state FIRST before any async fetch
    private void ResetUIToSafeDefault()
    {
        if (displayUsernameText != null) displayUsernameText.text = "";
        if (displayDateText != null) displayDateText.text = "";

        // Lock everything down — never show certificate button until DB confirms it
        if (certificateButton != null) certificateButton.SetActive(false);
        if (incompleteTextObject != null) incompleteTextObject.SetActive(true);
    }

    private void SetCurrentDate()
    {
        if (displayDateText != null)
            displayDateText.text = DateTime.Now.ToString("MMMM dd, yyyy");
    }

    private void CheckCertificateStatus(FirebaseUser user)
    {
        string expectedUserId = user.UserId;

        // ✅ Reload to get the latest Google account name
        user.ReloadAsync().ContinueWithOnMainThread(reloadTask =>
        {
            if (reloadTask.IsFaulted)
                Debug.LogWarning(">>> CERT: Could not reload user profile. Using cached data.");

            FirebaseUser refreshedUser = auth.CurrentUser;

            if (refreshedUser == null || refreshedUser.UserId != expectedUserId)
            {
                Debug.LogWarning(">>> CERT: User changed during reload. Discarding.");
                return;
            }

            // ✅ Set name immediately from Google — no Firestore involved
            if (displayUsernameText != null)
            {
                string finalName = !string.IsNullOrEmpty(refreshedUser.DisplayName)
                    ? refreshedUser.DisplayName
                    : "Player_" + refreshedUser.UserId.Substring(0, 4);

                displayUsernameText.text = finalName;
            }

            // Now separately fetch Firestore ONLY for completion status
            DocumentReference userDoc = db.Collection("users").Document(expectedUserId);

            userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
            {
                if (auth.CurrentUser == null || auth.CurrentUser.UserId != expectedUserId)
                {
                    Debug.LogWarning(">>> CERT: Discarding stale fetch — user changed mid-flight.");
                    return;
                }

                if (task.IsFaulted)
                {
                    Debug.LogError(">>> CERT: Failed to fetch completion status.");
                    ResetUIToSafeDefault();
                    return;
                }

                DocumentSnapshot snapshot = task.Result;

                bool allLevelsCompleted = false;
                if (snapshot.Exists && snapshot.ContainsField("all_levels_completed"))
                    allLevelsCompleted = snapshot.GetValue<bool>("all_levels_completed");

                UpdateUI(allLevelsCompleted);
                Debug.Log($">>> CERT: Loaded for [{expectedUserId}]. Completed: {allLevelsCompleted}");
            });
        });
    }

    private void UpdateUI(bool isCompleted)
    {
        if (certificateButton != null) certificateButton.SetActive(isCompleted);
        if (incompleteTextObject != null) incompleteTextObject.SetActive(!isCompleted);
    }

    // ✅ CRITICAL: Always unsubscribe to prevent memory leaks and ghost callbacks
    void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthStateChanged;
    }
}