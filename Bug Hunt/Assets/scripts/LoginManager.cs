using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using i5.Toolkit.Core.ServiceCore;
using i5.Toolkit.Core.OpenIDConnectClient;
using System;
using System.Threading.Tasks;
using System.Collections.Generic;

public class LoginManager : MonoBehaviour
{
    [Header("UI Dependencies")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private panelExit exitAnimationScript;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    void Start()
    {
        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;
    }

    // ==========================================
    // 1. GUEST LOGIN HANDLING
    // ==========================================
    public void OnGuestClicked()
    {
        Debug.Log("Attempting Guest Login...");

        // FIX 1: Change parameter to base 'Task'
        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread((Task task) =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Guest Login Failed: " + task.Exception);
                return;
            }

            // FIX 2: Cast the task back to 'Task<AuthResult>' to get the .Result
            AuthResult result = ((Task<AuthResult>)task).Result;
            FirebaseUser user = result.User;

            HandleGuestDatabaseLogic(user);
        });
    }

    private void HandleGuestDatabaseLogic(FirebaseUser user)
    {
        DocumentReference userDoc = db.Collection("users").Document(user.UserId);

        // FIX 1: Change parameter to base 'Task'
        userDoc.GetSnapshotAsync().ContinueWithOnMainThread((Task task) =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Database Error: " + task.Exception);
                return;
            }

            // FIX 2: Cast to 'Task<DocumentSnapshot>' to get the result
            DocumentSnapshot snapshot = ((Task<DocumentSnapshot>)task).Result;

            if (snapshot.Exists)
            {
                Debug.Log($"Welcome back Guest: {user.UserId}");
                userDoc.UpdateAsync(new Dictionary<string, object> {
                    { "lastLogin", FieldValue.ServerTimestamp }
                });
            }
            else
            {
                Debug.Log("New Guest Detected. Creating Record...");
                var newUserData = new Dictionary<string, object>
                {
                    { "type", "guest" },
                    { "createdAt", FieldValue.ServerTimestamp },
                    { "lastLogin", FieldValue.ServerTimestamp }
                };
                userDoc.SetAsync(newUserData);
            }

            TriggerSuccessAnimation();
        });
    }

    // ==========================================
    // 2. GOOGLE LOGIN HANDLING
    // ==========================================
    public async void OnGoogleClicked()
    {
        Debug.Log(">>> GOOGLE: Button Clicked. Checking Service...");

        if (!ServiceManager.ServiceExists<OpenIDConnectService>())
        {
            Debug.LogError(">>> ERROR: i5 Service is missing. Check GameBootstrapper.");
            return;
        }

        var oidc = ServiceManager.GetService<OpenIDConnectService>();

        Debug.Log(">>> GOOGLE: Opening Browser... (Don't stop the game!)");

        EventHandler loginHandler = null;
        var loginTcs = new TaskCompletionSource<bool>();
        loginHandler = (sender, args) =>
        {
            loginTcs.TrySetResult(true);
        };

        oidc.LoginCompleted += loginHandler;

        try
        {
            // This is the danger zone. 
            // If the Secret is wrong, or Unity pauses, it crashes HERE.
            await oidc.OpenLoginPageAsync();

            Debug.Log(">>> GOOGLE: Browser Task Finished!");

            Task completedTask = await Task.WhenAny(loginTcs.Task, Task.Delay(TimeSpan.FromSeconds(120)));
            if (completedTask != loginTcs.Task)
            {
                Debug.LogError(">>> FAILURE: Login timed out. (User Cancelled?)");
                return;
            }
        }
        catch (System.Exception e)
        {
            // THIS IS WHAT WE NEED TO SEE
            Debug.LogError(">>> CRITICAL CRASH CAUGHT: " + e.Message);
            return;
        }
        finally
        {
            oidc.LoginCompleted -= loginHandler;
        }

        // If we survived the crash, continue...
        if (oidc.IsLoggedIn)
        {
            Debug.Log(">>> SUCCESS: Token received. Google SSO is working.");
            CheckGoogleUserDatabase(null);
        }
        else
        {
            Debug.LogError(">>> FAILURE: Not Logged In. (Secret Mismatch or User Cancelled?)");
        }
    }

    private void CheckGoogleUserDatabase(FirebaseUser user)
    {
        if (user == null)
        {
            Debug.LogWarning(">>> NOTE: Firebase user lookup is skipped for now. Coming soon.");
            TriggerSuccessAnimation();
            return;
        }

        DocumentReference userDoc = db.Collection("users").Document(user.UserId);

        // FIX 1: Change parameter to base 'Task'
        userDoc.GetSnapshotAsync().ContinueWithOnMainThread((Task task) =>
        {
            // FIX 2: Cast to 'Task<DocumentSnapshot>'
            DocumentSnapshot snapshot = ((Task<DocumentSnapshot>)task).Result;

            if (snapshot.Exists)
            {
                Debug.Log("Existing Google User found. Logging in...");
                userDoc.UpdateAsync(new Dictionary<string, object> {
                    { "lastLogin", FieldValue.ServerTimestamp }
                });
                TriggerSuccessAnimation();
            }
            else
            {
                Debug.LogWarning("Success! New Google User detected.");
                Debug.LogWarning("DEBUG: Not integrated yet. Soon! but it works.");

                TriggerSuccessAnimation();
            }
        });
    }

    // ==========================================
    // 3. EMAIL/PASSWORD HANDLING
    // ==========================================
    public void OnLoginButtonClicked()
    {
        string email = emailInput.text;
        string pass = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            Debug.LogError("Please enter both email and password.");
            return;
        }

        // FIX 1: Change parameter to base 'Task'
        auth.SignInWithEmailAndPasswordAsync(email, pass).ContinueWithOnMainThread((Task task) =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Wrong Credentials or Login Error.");
                return;
            }

            // FIX 2: Cast to 'Task<AuthResult>'
            AuthResult result = ((Task<AuthResult>)task).Result;
            FirebaseUser user = result.User;

            Debug.Log("Email Login Successful: " + user.UserId);
            TriggerSuccessAnimation();
        });
    }

    private void TriggerSuccessAnimation()
    {
        if (exitAnimationScript != null)
        {
            Debug.Log("Triggering Panel Exit Animation...");
            exitAnimationScript.StartPanelExit();
        }
        else
        {
            Debug.LogWarning("Login Success, but 'panelExit' script is not assigned in Inspector!");
        }
    }
}