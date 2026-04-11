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
    [SerializeField] private GameObject certificateButton; // Show if completed
    [SerializeField] private GameObject incompleteTextObject; // Show if not completed

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Start()
    {
        InitializeFirebase();
        SetCurrentDate();
        CheckCertificateStatus();
    }

    private void InitializeFirebase()
    {
        if (auth == null) auth = FirebaseAuth.DefaultInstance;
        if (db == null) db = FirebaseFirestore.DefaultInstance;
    }

    private void SetCurrentDate()
    {
        // Formats the date to "Month Day, Year" (e.g., April 11, 2026)
        if (displayDateText != null)
        {
            displayDateText.text = DateTime.Now.ToString("MMMM dd, yyyy");
        }
    }

    private void CheckCertificateStatus()
    {
        // SAFETY: Make sure we have a logged-in user
        if (auth == null || auth.CurrentUser == null)
        {
            Debug.LogError(">>> ERROR: Cert script tried to load, but no User is logged in!");
            return;
        }

        FirebaseUser user = auth.CurrentUser;
        DocumentReference userDoc = db.Collection("users").Document(user.UserId);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError(">>> CERT: Failed to fetch user data for certificate.");
                return;
            }

            DocumentSnapshot snapshot = task.Result;
            if (snapshot.Exists)
            {
                // 1. Handle Username
                if (snapshot.ContainsField("username") && displayUsernameText != null)
                {
                    string name = snapshot.GetValue<string>("username");
                    displayUsernameText.text = name;
                }
                else if (displayUsernameText != null)
                {
                    displayUsernameText.text = "Player_" + user.UserId.Substring(0, 4);
                }

                // 2. Handle Completion Status & UI Toggle
                bool allLevelsCompleted = false;
                if (snapshot.ContainsField("all_levels_completed"))
                {
                    allLevelsCompleted = snapshot.GetValue<bool>("all_levels_completed");
                }

                UpdateUI(allLevelsCompleted);
                Debug.Log($">>> CERT: Status loaded. All levels completed: {allLevelsCompleted}");
            }
            else
            {
                Debug.LogWarning(">>> CERT: User document does not exist in DB!");
                UpdateUI(false); // Default to false if no DB entry exists
            }
        });
    }

    private void UpdateUI(bool isCompleted)
    {
        if (certificateButton != null) certificateButton.SetActive(isCompleted);
        if (incompleteTextObject != null) incompleteTextObject.SetActive(!isCompleted);
    }

}