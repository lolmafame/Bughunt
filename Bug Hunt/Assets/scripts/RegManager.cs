using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine.UI;

public class RegManager : MonoBehaviour
{
    [Header("Input Fields")]
    [SerializeField] private TMP_InputField usernameField;
    [SerializeField] private TMP_InputField passwordField;
    [SerializeField] private TMP_InputField confirmPasswordField;

    [Header("Popups")]
    [SerializeField] private GameObject regCompletePopup;
    [SerializeField] private GameObject processFailedPopup;
    // Add other popups here if needed (e.g. noRecordWarning)

    [Header("Dependencies")]
    [SerializeField] private LoginManager loginManager;

    private FirebaseFirestore db;
    private FirebaseUser googleUser; // Holds the user if coming from Google
    private bool isGoogleFlow = false;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        CloseAllPopups();
    }

    // Called by LoginManager when a new Google user is detected
    public void InitializeForGoogleUser(FirebaseUser user)
    {
        googleUser = user;
        isGoogleFlow = true;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            Debug.Log("RegManager activated its GameObject.");
        }

        // Visual setup: Google users don't need passwords
        if (passwordField) passwordField.interactable = false;
        if (confirmPasswordField) confirmPasswordField.interactable = false;

        Debug.Log("Registration initialized for Google User: " + user.Email);
    }

    // Call this via the "Confirm" / "Register" button in your UI
    public void OnConfirmClicked()
    {
        string username = usernameField.text.Trim();
        Debug.Log("Registration confirm clicked. Username=" + username);

        // 1. Basic Validation
        if (string.IsNullOrEmpty(username))
        {
            Debug.LogError("Username is empty");
            ShowProcessFailed();
            return;
        }

        // If NOT Google flow, validate passwords (future proofing)
        if (!isGoogleFlow)
        {
            if (passwordField.text != confirmPasswordField.text)
            {
                Debug.LogError("Passwords do not match");
                ShowProcessFailed();
                return;
            }
        }

        // 2. Check if Username is Taken (Database Check)
        DocumentReference usernameRef = db.Collection("usernames").Document(username);

        usernameRef.GetSnapshotAsync().ContinueWithOnMainThread((Task<DocumentSnapshot> task) =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Username check failed: " + task.Exception);
                ShowProcessFailed();
                return;
            }

            DocumentSnapshot snap = task.Result;
            if (snap.Exists)
            {
                Debug.LogError("Username already taken!");
                ShowProcessFailed(); // Or a specific "Username Taken" popup if you have one
            }
            else
            {
                // Username is free, proceed to create account
                if (isGoogleFlow)
                {
                    Debug.Log("Username available. Saving Google user...");
                    SaveGoogleUserToFirestore(username);
                }
                else
                {
                    // Handle standard email registration creation here if needed
                }
            }
        });
    }

    private void SaveGoogleUserToFirestore(string username)
    {
        if (googleUser == null)
        {
            Debug.LogError("Google user not set for registration.");
            ShowProcessFailed();
            return;
        }
        // Batch write to ensure both User and Username are reserved together
        WriteBatch batch = db.StartBatch();

        // A. Data for 'users' collection
        DocumentReference userRef = db.Collection("users").Document(googleUser.UserId);
        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "username", username },
            { "email", googleUser.Email },
            { "type", "google" },
            { "createdAt", FieldValue.ServerTimestamp },
            { "lastLogin", FieldValue.ServerTimestamp }
        };
        batch.Set(userRef, userData);

        // B. Data for 'usernames' collection (Reverse lookup/Reservation)
        DocumentReference usernameRef = db.Collection("usernames").Document(username);
        Dictionary<string, object> nameData = new Dictionary<string, object>
        {
            { "uid", googleUser.UserId }
        };
        batch.Set(usernameRef, nameData);

        // Commit
        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Failed to save user data: " + task.Exception);
                ShowProcessFailed();
            }
            else
            {
                Debug.Log("Registration Complete!");
                ShowRegComplete();
            }
        });
    }

    // ==========================================
    // POPUP HANDLING
    // ==========================================

    private void ShowRegComplete()
    {
        if (regCompletePopup) regCompletePopup.SetActive(true);
        Debug.Log("regcomplete popup shown.");
    }

    private void ShowProcessFailed()
    {
        if (processFailedPopup) processFailedPopup.SetActive(true);
        Debug.Log("processfailed popup shown.");
    }

    private void CloseAllPopups()
    {
        if (regCompletePopup) regCompletePopup.SetActive(false);
        if (processFailedPopup) processFailedPopup.SetActive(false);
    }

    // Link this to the 'Confirm' button inside the 'regcomplete' popup
    public void OnRegCompletePopupConfirmed()
    {
        CloseAllPopups();
        Debug.Log("regcomplete confirm clicked. Finalizing login.");
        // Trigger the final exit animation in LoginManager
        loginManager.FinalizeLogin();
    }

    // Link this to the 'Confirm/Close' button inside the 'processfailed' popup
    public void OnProcessFailedPopupClosed()
    {
        CloseAllPopups();
        Debug.Log("processfailed popup closed.");
    }
}