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

    [Header("User Agreement")]
    [SerializeField] private GameObject userAgreementPanel;
    [SerializeField] private Toggle agreeToggle;
    [SerializeField] private Button proceedButton;

    [Header("Popups")]
    [SerializeField] private GameObject regCompletePopup;
    [SerializeField] private GameObject processFailedPopup;
    [SerializeField] private TMP_Text processFailedText;
    [SerializeField] private GameObject popupContainer;

    [Header("Dependencies")]
    [SerializeField] private LoginManager loginManager;
    [SerializeField] private regAnim regAnimController;
    [SerializeField] private GameObject registrationPanel;

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private FirebaseUser googleUser;
    private string googleIdToken;
    private string googleAccessToken;
    private bool isGoogleFlow = false;
    private bool hasAgreedToTerms = false;
    private bool listenersWired = false;
    private bool _startedViaGoogleFlow = false;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        CloseAllPopups();

        // Skip panel reset if InitializeForGoogleUser already set up the flow.
        // This prevents Unity's deferred Start() from wiping panel state on the
        // next frame after the object was activated mid-flow.
        if (!_startedViaGoogleFlow)
        {
            if (userAgreementPanel) userAgreementPanel.SetActive(false);
            if (registrationPanel) registrationPanel.SetActive(false);
        }

        WireListeners();
    }

    // Wire once — safe to call multiple times due to Remove-before-Add
    private void WireListeners()
    {
        if (agreeToggle != null)
        {
            // Remove first to prevent duplicate listeners if called again
            agreeToggle.onValueChanged.RemoveListener(OnAgreeToggleChanged);
            agreeToggle.onValueChanged.AddListener(OnAgreeToggleChanged);
        }
        else
        {
            Debug.LogError("RegManager: agreeToggle is NOT assigned in the Inspector!");
        }

        if (proceedButton != null)
        {
            proceedButton.onClick.RemoveListener(OnUserAgreementAccepted);
            proceedButton.onClick.AddListener(OnUserAgreementAccepted);
        }
        else
        {
            Debug.LogError("RegManager: proceedButton is NOT assigned in the Inspector!");
        }

        listenersWired = true;
    }

    // ==========================================
    // USER AGREEMENT
    // ==========================================

    private void ShowUserAgreement()
    {
        // Ensure listeners are ready (in case Start() order was unusual)
        if (!listenersWired) WireListeners();

        // Force reset toggle state
        if (agreeToggle != null)
        {
            agreeToggle.isOn = false;
        }

        // Drive button state directly — don't rely solely on the event
        SetProceedButtonState(false);

        if (userAgreementPanel) userAgreementPanel.SetActive(true);
        if (registrationPanel) registrationPanel.SetActive(false);

        Debug.Log("UserAgreement panel shown.");
    }

    // Drives the proceed button interactability — single source of truth
    private void SetProceedButtonState(bool interactable)
    {
        if (proceedButton != null)
            proceedButton.interactable = interactable;

        Debug.Log("Proceed button interactable: " + interactable);
    }

    // Called by the Toggle's onValueChanged
    public void OnAgreeToggleChanged(bool isChecked)
    {
        Debug.Log(">>> TOGGLE CHANGED: " + isChecked);
        SetProceedButtonState(isChecked);
    }

    // Called by proceedButton.onClick
    public void OnUserAgreementAccepted()
    {
        // Double-check toggle state in case something bypassed the listener
        bool agreed = agreeToggle != null && agreeToggle.isOn;
        if (!agreed)
        {
            Debug.LogWarning("Proceed clicked but checkbox is not ticked.");
            return;
        }

        hasAgreedToTerms = true;
        Debug.Log("Terms accepted. Hiding agreement panel, showing registration.");

        if (userAgreementPanel) userAgreementPanel.SetActive(false);
        if (registrationPanel) registrationPanel.SetActive(true);
    }

    // ==========================================
    // GOOGLE FLOW ENTRY POINT
    // ==========================================

    public void InitializeForGoogleUser(FirebaseUser user, string idToken, string accessToken)
    {
        googleUser = user;
        isGoogleFlow = true;
        googleIdToken = idToken;
        googleAccessToken = accessToken;
        hasAgreedToTerms = false;
        _startedViaGoogleFlow = true;

        if (!gameObject.activeSelf)
        {
            gameObject.SetActive(true);
            Debug.Log("RegManager activated its GameObject.");
        }

        if (passwordField) passwordField.interactable = true;
        if (confirmPasswordField) confirmPasswordField.interactable = true;

        Debug.Log("Registration initialized for Google User: " + user.Email);

        ShowUserAgreement();
    }

    // ==========================================
    // REGISTRATION
    // ==========================================

    public void OnConfirmClicked()
    {
        if (!hasAgreedToTerms)
        {
            ShowProcessFailed("You must agree to the User Agreement before registering.");
            return;
        }

        string username = usernameField.text.Trim();
        Debug.Log("Registration confirm clicked. Username=" + username);

        if (string.IsNullOrEmpty(username))
        {
            ShowProcessFailed("Username cannot be empty.");
            return;
        }

        if (!isGoogleFlow)
        {
            if (passwordField.text != confirmPasswordField.text)
            {
                ShowProcessFailed("Passwords do not match.");
                return;
            }
            if (!IsPasswordStrong(passwordField.text))
            {
                ShowProcessFailed("Password must include at least one uppercase letter and one special character.");
                return;
            }
        }
        else
        {
            if (string.IsNullOrEmpty(passwordField.text) || string.IsNullOrEmpty(confirmPasswordField.text))
            {
                ShowProcessFailed("Both password fields are required.");
                return;
            }
            if (passwordField.text != confirmPasswordField.text)
            {
                ShowProcessFailed("Passwords do not match.");
                return;
            }
            if (!IsPasswordStrong(passwordField.text))
            {
                ShowProcessFailed("Password must include at least one uppercase letter and one special character.");
                return;
            }
        }

        DocumentReference usernameRef = db.Collection("usernames").Document(username);
        usernameRef.GetSnapshotAsync().ContinueWithOnMainThread((Task<DocumentSnapshot> task) =>
        {
            if (task.IsFaulted)
            {
                ShowProcessFailed("Unable to check username. Please try again.");
                return;
            }

            DocumentSnapshot snap = task.Result;
            if (snap.Exists)
            {
                ShowProcessFailed("Username is already taken. Please choose another.");
            }
            else
            {
                if (isGoogleFlow)
                {
                    Debug.Log("Username available. Saving Google user...");
                    LinkGoogleUserWithEmailPassword(username, passwordField.text);
                }
            }
        });
    }

    private void SaveGoogleUserToFirestore(string username)
    {
        if (googleUser == null)
        {
            ShowProcessFailed("Google user session expired. Please sign in again.");
            return;
        }

        WriteBatch batch = db.StartBatch();

        DocumentReference userRef = db.Collection("users").Document(googleUser.UserId);
        Dictionary<string, object> userData = new Dictionary<string, object>
        {
            { "username",      username },
            { "email",         googleUser.Email },
            { "type",          "google" },
            { "agreedToTerms", true },
            { "createdAt",     FieldValue.ServerTimestamp },
            { "lastLogin",     FieldValue.ServerTimestamp }
        };
        batch.Set(userRef, userData);

        DocumentReference usernameRef = db.Collection("usernames").Document(username);
        Dictionary<string, object> nameData = new Dictionary<string, object>
        {
            { "uid", googleUser.UserId }
        };
        batch.Set(usernameRef, nameData);

        batch.CommitAsync().ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted)
                ShowProcessFailed("Failed to save your data. Please try again.");
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
            ShowProcessFailed("Google user session expired. Please sign in again.");
            return;
        }

        googleUser.UpdatePasswordAsync(password).ContinueWithOnMainThread(task =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                LogAuthExceptionDetails(task.Exception);
                ShowProcessFailed("Failed to set password. Please try again.");
                return;
            }

            Debug.Log("Password set for Google user.");
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
            FirebaseException fe = inner as FirebaseException;
            if (fe != null)
            {
                Debug.LogError("Firebase auth error code: " + fe.ErrorCode + ", message: " + fe.Message);
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
            if (char.IsUpper(c)) hasUpper = true;
            else if (!char.IsLetterOrDigit(c)) hasSpecial = true;
            if (hasUpper && hasSpecial) return true;
        }
        return false;
    }

    // ==========================================
    // POPUP HANDLING
    // ==========================================

    private void ShowRegComplete()
    {
        if (popupContainer) popupContainer.SetActive(true);
        if (regCompletePopup) regCompletePopup.SetActive(true);
        Debug.Log("regcomplete popup shown.");
    }

    private void ShowProcessFailed(string message)
    {
        if (popupContainer) popupContainer.SetActive(true);
        if (processFailedText) processFailedText.text = message;
        if (processFailedPopup) processFailedPopup.SetActive(true);
    }

    private void CloseAllPopups()
    {
        if (regCompletePopup) regCompletePopup.SetActive(false);
        if (processFailedPopup) processFailedPopup.SetActive(false);
        if (popupContainer) popupContainer.SetActive(false);
    }

    public void OnRegCompletePopupConfirmed()
    {
        CloseAllPopups();
        if (regAnimController != null) regAnimController.ClosePanels();
        if (registrationPanel != null) registrationPanel.SetActive(false);
        loginManager.CleanupRegistrationRoot();
        Debug.Log("regcomplete confirm clicked. Finalizing login.");
        loginManager.FinalizeLogin();
    }

    public void OnProcessFailedPopupClosed()
    {
        CloseAllPopups();
        Debug.Log("processfailed popup closed.");
    }
}