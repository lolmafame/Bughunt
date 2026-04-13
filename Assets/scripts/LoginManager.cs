using UnityEngine;
using TMPro;
using Firebase;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using i5.Toolkit.Core.ServiceCore;
using i5.Toolkit.Core.OpenIDConnectClient;
using System;
using System.Threading.Tasks;
using System.Collections;
using System.Collections.Generic;

public class LoginManager : MonoBehaviour
{
    [Header("UI Dependencies")]
    [SerializeField] private TMP_InputField emailInput;
    [SerializeField] private TMP_InputField passwordInput;
    [SerializeField] private panelExit exitAnimationScript;
    [SerializeField] private GameObject loginPanel;

    [Header("Registration Dependencies")]
    [SerializeField] private RegManager regManager;
    [SerializeField] private GameObject regAnimObject;
    [SerializeField] private GameObject registrationRoot;

    [Header("Forgot Password Dependencies")]
    [SerializeField] private GameObject forgotPasswordRoot;

    private FirebaseAuth auth;
    private FirebaseFirestore db;
    private string googleIdToken;
    private string googleAccessToken;

    // Tracks whether Firebase is ready to use
    private bool firebaseReady = false;

    // ==========================================
    // FIREBASE INITIALIZATION
    // ==========================================

    void Start()
    {
        FirebaseApp.CheckAndFixDependenciesAsync().ContinueWithOnMainThread(task =>
        {
            if (task.Result == DependencyStatus.Available)
            {
                auth = FirebaseAuth.DefaultInstance;
                db = FirebaseFirestore.DefaultInstance;
                firebaseReady = true;
                Debug.Log(">>> FIREBASE: Ready.");
            }
            else
            {
                Debug.LogError(">>> FIREBASE: Could not resolve dependencies: " + task.Result);
            }
        });
    }

    // Helper — shows a clear error if any button is tapped before Firebase is ready
    private bool EnsureFirebaseReady()
    {
        if (!firebaseReady)
        {
            Debug.LogError(">>> FIREBASE: Not ready yet. Please wait a moment and try again.");
            return false;
        }
        return true;
    }

    // ==========================================
    // 1. GUEST LOGIN HANDLING
    // ==========================================

    public void OnGuestClicked()
    {
        if (!EnsureFirebaseReady()) return;
        Debug.Log("Attempting Guest Login...");

        auth.SignInAnonymouslyAsync().ContinueWithOnMainThread((Task task) =>
        {
            if (task.IsCanceled || task.IsFaulted)
            {
                Debug.LogError("Guest Login Failed: " + task.Exception);
                return;
            }

            AuthResult result = ((Task<AuthResult>)task).Result;
            HandleGuestDatabaseLogic(result.User);
        });
    }

    private void HandleGuestDatabaseLogic(FirebaseUser user)
    {
        DocumentReference userDoc = db.Collection("users").Document(user.UserId);

        userDoc.GetSnapshotAsync().ContinueWithOnMainThread((Task task) =>
        {
            if (task.IsFaulted)
            {
                Debug.LogError("Database Error: " + task.Exception);
                return;
            }

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
                userDoc.SetAsync(new Dictionary<string, object>
                {
                    { "type",      "guest" },
                    { "createdAt", FieldValue.ServerTimestamp },
                    { "lastLogin", FieldValue.ServerTimestamp }
                });
            }

            TriggerSuccessAnimation();
        });
    }

    // ==========================================
    // 2. GOOGLE LOGIN HANDLING
    // ==========================================

    public async void OnGoogleClicked()
    {
        if (!EnsureFirebaseReady()) return;
        Debug.Log(">>> GOOGLE: Button Clicked. Checking Service...");

        if (!ServiceManager.ServiceExists<OpenIDConnectService>())
        {
            Debug.LogError(">>> ERROR: i5 Service is missing. Check GameBootstrapper.");
            return;
        }

        var oidc = ServiceManager.GetService<OpenIDConnectService>();
        Debug.Log(">>> GOOGLE: Opening Browser...");

        EventHandler loginHandler = null;
        var loginTcs = new TaskCompletionSource<bool>();
        loginHandler = (sender, args) => { loginTcs.TrySetResult(true); };
        oidc.LoginCompleted += loginHandler;

        try
        {
            await oidc.OpenLoginPageAsync();
            Task completedTask = await Task.WhenAny(loginTcs.Task, Task.Delay(TimeSpan.FromSeconds(120)));
            if (completedTask != loginTcs.Task)
            {
                Debug.LogError(">>> FAILURE: Login timed out.");
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

        if (!oidc.IsLoggedIn) return;

        Debug.Log(">>> SUCCESS: Token received.");
        googleIdToken = null;
        googleAccessToken = oidc.AccessToken;

        if (string.IsNullOrEmpty(oidc.AccessToken))
        {
            Debug.LogError(">>> FIREBASE ERROR: No token available.");
            return;
        }

        Debug.Log(">>> GOOGLE: Exchanging token with Firebase...");
        Credential credential = GoogleAuthProvider.GetCredential(null, oidc.AccessToken);
        Task<FirebaseUser> signInTask = auth.SignInWithCredentialAsync(credential);
        Task completedSignIn = await Task.WhenAny(signInTask, Task.Delay(TimeSpan.FromSeconds(30)));

        if (completedSignIn != signInTask || signInTask.IsFaulted || signInTask.IsCanceled)
        {
            Debug.LogError(">>> FIREBASE ERROR: Sign-in failed or timed out. " + signInTask.Exception);
            return;
        }

        FirebaseUser firebaseUser = signInTask.Result;
        Debug.Log(">>> FIREBASE: Sign-in complete. UID=" + firebaseUser.UserId);

        // Small yield to let Firestore finish connecting after Auth completes
        StartCoroutine(CheckGoogleUserWithRetry(firebaseUser));
    }

    // Retries the Firestore check up to 3 times with a 2-second gap
    // to handle the brief offline window right after Firebase Auth resolves
    private IEnumerator CheckGoogleUserWithRetry(FirebaseUser user, int maxRetries = 3)
    {
        int attempt = 0;
        bool done = false;

        while (attempt < maxRetries && !done)
        {
            attempt++;
            Debug.Log($">>> FIRESTORE: Attempt {attempt} to check user record...");

            bool waiting = true;
            bool succeeded = false;

            // Fire the async Firestore call and capture result flags
            db.Collection("users").Document(user.UserId)
              .GetSnapshotAsync()
              .ContinueWithOnMainThread(task =>
              {
                  if (task.IsFaulted)
                  {
                      // Check specifically for the offline error
                      bool isOffline = task.Exception != null &&
                                       task.Exception.ToString().Contains("client is offline");

                      if (isOffline && attempt < maxRetries)
                      {
                          Debug.LogWarning($">>> FIRESTORE: Offline on attempt {attempt}. Retrying...");
                      }
                      else
                      {
                          Debug.LogError(">>> FIRESTORE: Failed after retries. " + task.Exception);
                          done = true; // Give up
                      }
                  }
                  else
                  {
                      succeeded = true;
                      done = true;
                      HandleGoogleUserSnapshot(task.Result, user);
                  }

                  waiting = false;
              });

            // Wait until the async call resolves
            yield return new WaitUntil(() => !waiting);

            // If it failed and we haven't given up, wait before retrying
            if (!done && !succeeded)
                yield return new WaitForSeconds(2f);
        }
    }

    private void HandleGoogleUserSnapshot(DocumentSnapshot snapshot, FirebaseUser user)
    {
        DocumentReference userDoc = db.Collection("users").Document(user.UserId);

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
            Debug.Log("New Google User detected. Triggering Registration...");

            if (loginPanel != null)
                loginPanel.SetActive(false);

            if (registrationRoot != null && !registrationRoot.activeSelf)
                registrationRoot.SetActive(true);
            else if (registrationRoot == null)
                Debug.LogWarning(">>> REG: registrationRoot not assigned.");

            if (regAnimObject != null)
            {
                regAnimObject.SetActive(false);
                regAnimObject.SetActive(true);
            }
            else
                Debug.LogError(">>> REG: regAnimObject not assigned.");

            if (regManager != null)
                regManager.InitializeForGoogleUser(user, googleIdToken, googleAccessToken);
            else
                Debug.LogError(">>> REG: RegManager not assigned.");
        }
    }

    // ==========================================
    // 3. EMAIL/PASSWORD HANDLING
    // ==========================================

    public void OnLoginButtonClicked()
    {
        if (!EnsureFirebaseReady()) return;
        Debug.Log(">>> EMAIL LOGIN: Button clicked.");

        if (forgotPasswordRoot != null && forgotPasswordRoot.activeSelf)
            forgotPasswordRoot.SetActive(false);

        string email = emailInput.text;
        string pass = passwordInput.text;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(pass))
        {
            Debug.LogError("Please enter both email and password.");
            return;
        }

        if (email.Contains("@"))
        {
            Debug.Log(">>> EMAIL LOGIN: Signing in with email " + email);
            SignInWithEmail(email, pass);
        }
        else
        {
            Debug.Log(">>> USERNAME LOGIN: Resolving username " + email.Trim());
            ResolveUsernameAndLogin(email.Trim(), pass);
        }
    }

    private void SignInWithEmail(string email, string pass)
    {
        auth.SignInWithEmailAndPasswordAsync(email, pass).ContinueWithOnMainThread((Task task) =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError(">>> EMAIL LOGIN ERROR: " + task.Exception);
                return;
            }

            FirebaseUser user = ((Task<AuthResult>)task).Result.User;
            Debug.Log(">>> EMAIL LOGIN: Successful. UID=" + user.UserId);
            TriggerSuccessAnimation();
        });
    }

    private void ResolveUsernameAndLogin(string username, string pass)
    {
        db.Collection("usernames").Document(username)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread((Task<DocumentSnapshot> task) =>
          {
              if (task.IsFaulted || task.IsCanceled)
              {
                  Debug.LogError(">>> USERNAME LOGIN ERROR: " + task.Exception);
                  return;
              }

              DocumentSnapshot snapshot = task.Result;
              if (!snapshot.Exists)
              {
                  Debug.LogError(">>> USERNAME LOGIN ERROR: Username not found.");
                  return;
              }

              if (!snapshot.TryGetValue("uid", out string uid) || string.IsNullOrEmpty(uid))
              {
                  Debug.LogError(">>> USERNAME LOGIN ERROR: UID not found.");
                  return;
              }

              db.Collection("users").Document(uid)
                .GetSnapshotAsync()
                .ContinueWithOnMainThread((Task<DocumentSnapshot> userTask) =>
                {
                    if (userTask.IsFaulted || userTask.IsCanceled)
                    {
                        Debug.LogError(">>> USERNAME LOGIN ERROR: " + userTask.Exception);
                        return;
                    }

                    DocumentSnapshot userSnapshot = userTask.Result;
                    if (!userSnapshot.Exists)
                    {
                        Debug.LogError(">>> USERNAME LOGIN ERROR: User record not found.");
                        return;
                    }

                    if (!userSnapshot.TryGetValue("email", out string email) || string.IsNullOrEmpty(email))
                    {
                        Debug.LogError(">>> USERNAME LOGIN ERROR: Email not found.");
                        return;
                    }

                    Debug.Log(">>> USERNAME LOGIN: Resolved email " + email);
                    SignInWithEmail(email, pass);
                });
          });
    }

    // ==========================================
    // FINALIZE
    // ==========================================

    public void FinalizeLogin()
    {
        TriggerSuccessAnimation();
    }

    public void CleanupRegistrationRoot()
    {
        if (registrationRoot != null)
            registrationRoot.SetActive(false);
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
            Debug.LogWarning("Login Success, but 'panelExit' script is not assigned!");
        }
    }
}