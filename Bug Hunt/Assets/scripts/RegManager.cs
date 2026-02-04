using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using Firebase;
using System;
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
    [SerializeField] private GameObject popupContainer;
    // Add other popups here if needed (e.g. noRecordWarning)

    [Header("Dependencies")]
    [SerializeField] private LoginManager loginManager;
    [SerializeField] private regAnim regAnimController;
    [SerializeField] private GameObject registrationPanel;

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser googleUser; // Holds the user if coming from Google
    private string googleIdToken;
    private string googleAccessToken;
    private bool isGoogleFlow = false;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        CloseAllPopups();
    }

    // Called by LoginManager when a new Google user is detected
    public void InitializeForGoogleUser(FirebaseUser user, string idToken, string accessToken)
    {
        googleUser = user;
        isGoogleFlow = true;
        googleIdToken = idToken;
        googleAccessToken = accessToken;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            Debug.Log("RegManager activated its GameObject.");
        }

        // Google users will link an email/password credential here
        if (passwordField) passwordField.interactable = true;
        if (confirmPasswordField) confirmPasswordField.interactable = true;

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

            if (!IsPasswordStrong(passwordField.text))
            {
                Debug.LogError("Password must include at least one uppercase letter and one special character.");
                ShowProcessFailed();
                return;
            }
        }
        else
        {
            if (string.IsNullOrEmpty(passwordField.text) || string.IsNullOrEmpty(confirmPasswordField.text))
            {
                Debug.LogError("Password fields are required for Google linking");
                ShowProcessFailed();
                return;
            }

            if (passwordField.text != confirmPasswordField.text)
            {
                Debug.LogError("Passwords do not match");
                ShowProcessFailed();
                return;
            }

            if (!IsPasswordStrong(passwordField.text))
            {
                Debug.LogError("Password must include at least one uppercase letter and one special character.");
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
                    LinkGoogleUserWithEmailPassword(username, passwordField.text);
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

    private void LinkGoogleUserWithEmailPassword(string username, string password)
    {
        if (googleUser == null)
        {
            Debug.LogError("Google user not set for registration.");
            ShowProcessFailed();
            return;
        }

        googleUser.UpdatePasswordAsync(password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Failed to set password for Google user: " + task.Exception);
                LogAuthExceptionDetails(task.Exception);
                ShowProcessFailed();
                return;
            }

            Debug.Log("Password set for Google user. Email/password sign-in should work.");
            SaveGoogleUserToFirestore(username);
        });
    }

    private void LogAuthExceptionDetails(System.Exception exception)
    {
        AggregateException aggregate = exception as AggregateException;
        if (aggregate == null)
        {
            Debug.LogWarning("Auth error (non-aggregate): " + exception.GetType().Name + ": " + exception.Message);
            return;
        }

        foreach (System.Exception inner in aggregate.Flatten().InnerExceptions)
        {
            FirebaseException firebaseException = inner as FirebaseException;
            if (firebaseException != null)
            {
                Debug.LogError("Firebase auth error code: " + firebaseException.ErrorCode + ", message: " + firebaseException.Message);
                return;
            }
        }

        Debug.LogWarning("Auth error (no FirebaseException found): " + aggregate.Flatten().Message);
    }

    private bool IsPasswordStrong(string password)
    {
        bool hasUpper = false;
        bool hasSpecial = false;

        foreach (char c in password)
        {
            if (char.IsUpper(c))
            {
                hasUpper = true;
            }
            else if (!char.IsLetterOrDigit(c))
            {
                hasSpecial = true;
            }

            if (hasUpper && hasSpecial)
            {
                return true;
            }
        }

        return false;
    }

    // ==========================================
    // POPUP HANDLING
    // ==========================================

    private void ShowRegComplete()
    {
        if (regAnimController != null)
        {
            regAnimController.ClosePanels();
        }
        if (popupContainer) popupContainer.SetActive(true);
        if (regCompletePopup) regCompletePopup.SetActive(true);
        Debug.Log("regcomplete popup shown.");
    }

    private void ShowProcessFailed()
    {
        if (popupContainer) popupContainer.SetActive(true);
        if (processFailedPopup) processFailedPopup.SetActive(true);
        Debug.Log("processfailed popup shown.");
    }

    private void CloseAllPopups()
    {
        if (regCompletePopup) regCompletePopup.SetActive(false);
        if (processFailedPopup) processFailedPopup.SetActive(false);
        if (popupContainer) popupContainer.SetActive(false);
    }

    // Link this to the 'Confirm' button inside the 'regcomplete' popup
    public void OnRegCompletePopupConfirmed()
    {
        CloseAllPopups();
        if (regAnimController != null)
        {
            regAnimController.ClosePanels();
        }
        if (registrationPanel != null)
        {
            registrationPanel.SetActive(false);
        }
        Debug.Log("regcomplete confirm clicked. Finalizing login.");
        loginManager.FinalizeLogin();
    }

    // Link this to the 'Confirm/Close' button inside the 'processfailed' popup
    public void OnProcessFailedPopupClosed()
    {
        CloseAllPopups();
        Debug.Log("processfailed popup closed.");
    }
}