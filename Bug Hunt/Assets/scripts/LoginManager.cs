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

    [Header("Registration Dependencies")]
    [SerializeField] private RegManager regManager;
    [SerializeField] private GameObject regAnimObject; // The object with regAnim.cs
    [SerializeField] private GameObject registrationRoot; // Parent container for registration UI

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string googleIdToken;
    private string googleAccessToken;

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
            await oidc.OpenLoginPageAsync();
            Task completedTask = await Task.WhenAny(loginTcs.Task, Task.Delay(TimeSpan.FromSeconds(120)));
            if (completedTask != loginTcs.Task)
            {
                Debug.LogError(">>> FAILURE: Login timed out. (User Cancelled?)");
                return;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError(">>> CRITICAL CRASH CAUGHT: " + e.Message);
            return;
        }
        finally
        {
            oidc.LoginCompleted -= loginHandler;
        }

        if (oidc.IsLoggedIn)
        {
            Debug.Log(">>> SUCCESS: Token received. Google SSO is working.");

            var googleProvider = oidc.OidcProvider as GoogleOidcProvider;
            string idToken = googleProvider != null ? googleProvider.IdToken : null;
            googleIdToken = idToken;
            googleAccessToken = oidc.AccessToken;
            Debug.Log(">>> GOOGLE: AccessToken length=" + (string.IsNullOrEmpty(oidc.AccessToken) ? 0 : oidc.AccessToken.Length)
                + ", IdToken length=" + (string.IsNullOrEmpty(idToken) ? 0 : idToken.Length));

            if (string.IsNullOrEmpty(oidc.AccessToken) && string.IsNullOrEmpty(idToken))
            {
                Debug.LogError(">>> FIREBASE ERROR: No token available for Firebase credential.");
                return;
            }

            Debug.Log(">>> GOOGLE: Exchanging token with Firebase...");

            Credential credential = GoogleAuthProvider.GetCredential(idToken, oidc.AccessToken);
            Task<FirebaseUser> signInTask = auth.SignInWithCredentialAsync(credential);
            Task completedSignIn = await Task.WhenAny(signInTask, Task.Delay(TimeSpan.FromSeconds(30)));
            if (completedSignIn != signInTask)
            {
                Debug.LogError(">>> FIREBASE ERROR: Sign-in timed out.");
                return;
            }

            if (signInTask.IsFaulted || signInTask.IsCanceled)
            {
                Debug.LogError(">>> FIREBASE ERROR: " + signInTask.Exception);
                return;
            }

            FirebaseUser firebaseUser = signInTask.Result;
            Debug.Log(">>> FIREBASE: Sign-in complete. UID=" + firebaseUser.UserId + ", Email=" + firebaseUser.Email);
            CheckGoogleUserDatabase(firebaseUser);
        }
    }

    private void CheckGoogleUserDatabase(FirebaseUser user)
    {
        if (user == null)
        {
            Debug.LogError(">>> No Firebase User found. Ensure Google Credential was exchanged successfully.");
            return;
        }

        Debug.Log(">>> GOOGLE: Checking user in Firestore. UID=" + user.UserId + ", Email=" + user.Email);

        DocumentReference userDoc = db.Collection("users").Document(user.UserId);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread((Task<DocumentSnapshot> task) =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Firestore Error: " + task.Exception);
                return;
            }

            DocumentSnapshot snapshot = task.Result;

            if (snapshot.Exists)
            {
                // EXISTING USER: Just update login time and enter
                Debug.Log("Existing Google User found. Logging in...");
                userDoc.UpdateAsync(new Dictionary<string, object> {
                    { "lastLogin", FieldValue.ServerTimestamp }
                });
                TriggerSuccessAnimation();
            }
            else
            {
                // NEW USER: Trigger Registration Flow
                Debug.Log("New Google User detected. Triggering Registration...");

                if (registrationRoot != null)
                {
                    if (!registrationRoot.activeSelf)
                    {
                        registrationRoot.SetActive(true);
                        Debug.Log(">>> REG: registrationRoot activated: " + registrationRoot.name);
                    }
                }
                else
                {
                    Debug.LogWarning(">>> REG: registrationRoot is not assigned in LoginManager.");
                }

                // 1. Activate the Animation Object
                if (regAnimObject != null)
                {
                    if (regAnimObject.activeSelf)
                    {
                        regAnimObject.SetActive(false);
                    }
                    regAnimObject.SetActive(true);
                    Debug.Log(">>> REG: regAnimObject activated: " + regAnimObject.name);
                }
                else
                {
                    Debug.LogError(">>> REG: regAnimObject is not assigned in LoginManager.");
                }

                // 2. Initialize RegManager with this specific user
                if (regManager != null)
                {
                    regManager.InitializeForGoogleUser(user, googleIdToken, googleAccessToken);
                    Debug.Log(">>> REG: RegManager initialized for Google user.");
                }
                else
                {
                    Debug.LogError(">>> REG: RegManager is not assigned in LoginManager.");
                }
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

    public void FinalizeLogin()
    {
        TriggerSuccessAnimation();
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