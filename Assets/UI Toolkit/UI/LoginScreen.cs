using UnityEngine;
using UnityEngine.UIElements;
using Firebase.Auth; 
using Firebase.Firestore; 
using System.Net;
using System.IO;
using UnityEngine.Networking;
using System.Threading.Tasks;
using UnityEngine.SceneManagement;
using System.Collections.Generic; 
using System.Linq; 

public class LoginScreen : MonoBehaviour
{
    private UIDocument _uiDocument;

    // References to UI elements
    private TextField _emailField;
    private TextField _passwordField;
    private Button _loginButton;
    private Button _googleButton;
    private Button _signupLink;
    private Toggle _rememberMeToggle;
    private const string NextSceneName = "MainMenu";

    // --- New Firebase and Google Variables ---
    private FirebaseAuth auth;
    private const string WebClientId = "1022881527607-vlffirg46mkbvev5gh119i4eb0mslfgb.apps.googleusercontent.com";
    private const string ClientSecret = "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";

    // --- Firestore Manager ---
    private FirestoreManager _firestoreManager;
    // -----------------------------------------


    // Updated reference for the eye icon to be a Label
    private Label _showPasswordEye;
    // Unicode strings for Font Awesome icons
    private const string EYE_ICON = "\uf06e"; // fa-eye
    private const string EYE_SLASH_ICON = "\uf070"; // fa-eye-slash
    private const string CLOSE_ICON = "\uf00d"; // fa-times (for close button)

    // --- NEW MODAL UI VARIABLES ---
    private VisualElement _profileSetupModal;
    private TextField _usernameFieldModal;
    private TextField _newPasswordFieldModal;
    private TextField _confirmPasswordFieldModal;
    private Label _statusMessageModal;
    private Button _submitProfileButton;
    private Label _showNewPasswordEye;
    private Label _showConfirmPasswordEye;


    private VisualElement _passwordContainerNew;
    private VisualElement _passwordContainerConfirm;
    // ------------------------------

    // --- ADDED: FORGOT PASSWORD MODAL ---
    private Button _forgotPasswordLink;
    private VisualElement _forgotPasswordModal;
    private Button _closeForgotPasswordButton;
    private TextField _emailFieldForgot;
    private Label _statusMessageForgot;
    private Button _sendResetButton;
    // ------------------------------------


    void Awake()
    {
        _uiDocument = GetComponent<UIDocument>();
        if (_uiDocument == null)
        {
            Debug.LogError("UIDocument component not found!");
            return;
        }

        var root = _uiDocument.rootVisualElement;

        // Query for all the elements by their name
        _emailField = root.Q<TextField>("email-field");
        _passwordField = root.Q<TextField>("password-field");
        _loginButton = root.Q<Button>("login-button");
        _googleButton = root.Q<Button>("google-button");
        _signupLink = root.Q<Button>("signup-link");
        _rememberMeToggle = root.Q<Toggle>("remember-me-toggle");

        // Get reference to the eye icon LABEL using its name
        _showPasswordEye = root.Q<Label>("eye-icon");

        // --- Query for Modal Elements ---
        _profileSetupModal = root.Q<VisualElement>("profile-setup-modal");
        _usernameFieldModal = root.Q<TextField>("username-field-modal");
        _newPasswordFieldModal = root.Q<TextField>("new-password-field-modal");
        _confirmPasswordFieldModal = root.Q<TextField>("confirm-password-field-modal");
        _statusMessageModal = root.Q<Label>("status-message-modal");
        _submitProfileButton = root.Q<Button>("submit-profile-button");
        _showNewPasswordEye = root.Q<Label>("eye-icon-new");
        _showConfirmPasswordEye = root.Q<Label>("eye-icon-confirm");

        _passwordContainerNew = root.Q<VisualElement>("password-container-new");
        _passwordContainerConfirm = root.Q<VisualElement>("password-container-confirm");

        // --- ADDED: Query for Forgot Password Elements ---
        _forgotPasswordLink = root.Q<Button>("forgot-password-link");
        _forgotPasswordModal = root.Q<VisualElement>("forgot-password-modal");
        _closeForgotPasswordButton = root.Q<Button>("close-forgot-modal");
        _emailFieldForgot = root.Q<TextField>("email-field-forgot");
        _statusMessageForgot = root.Q<Label>("status-message-forgot");
        _sendResetButton = root.Q<Button>("send-reset-button");

        // Register button callbacks
        _loginButton.RegisterCallback<ClickEvent>(OnLoginClicked);
        _googleButton.RegisterCallback<ClickEvent>(OnGoogleClicked);
        _signupLink.RegisterCallback<ClickEvent>(OnContinueAsGuestClicked);
        _submitProfileButton.RegisterCallback<ClickEvent>(OnSubmitProfileClicked); // New Callback

        // --- ADDED: Register Forgot Password Callbacks ---
        _forgotPasswordLink.RegisterCallback<ClickEvent>(OnForgotPasswordClicked);
        _closeForgotPasswordButton.RegisterCallback<ClickEvent>(OnCloseForgotPasswordClicked);
        _sendResetButton.RegisterCallback<ClickEvent>(OnSendResetEmailClicked);
        // -------------------------------------------------

        // Register callback for eye icons
        // Use a lambda to pass the specific field and icon to the reusable method
        if (_showPasswordEye != null)
        {
            _showPasswordEye.RegisterCallback<ClickEvent>(evt => OnTogglePasswordVisibility(_passwordField, _showPasswordEye));
        }
        if (_showNewPasswordEye != null)
        {
            _showNewPasswordEye.RegisterCallback<ClickEvent>(evt => OnTogglePasswordVisibility(_newPasswordFieldModal, _showNewPasswordEye));
        }
        if (_showConfirmPasswordEye != null)
        {
            _showConfirmPasswordEye.RegisterCallback<ClickEvent>(evt => OnTogglePasswordVisibility(_confirmPasswordFieldModal, _showConfirmPasswordEye));
        }


        // --- Initialize Google Sign-In and Firebase Auth ---
        InitializeFirebase();

        // Ensure modals are hidden on start
        ShowProfileSetupModal(false);
        ShowForgotPasswordModal(false); // ADDED
    }

    private void InitializeFirebase()
    {
        // 1. Initialize Firebase Auth
        auth = FirebaseAuth.DefaultInstance;

        // 2. Initialize Firestore Manager
        _firestoreManager = new FirestoreManager();

        // 3. Optional: Check Firebase dependencies
        Firebase.FirebaseApp.CheckAndFixDependenciesAsync().ContinueWith(task => {
            var dependencyStatus = task.Result;
            if (dependencyStatus == Firebase.DependencyStatus.Available)
            {
                Debug.Log("Firebase dependencies are resolved.");
            }
            else
            {
                Debug.LogError($"Could not resolve Firebase dependencies: {dependencyStatus}");
            }
        });
    }

    /// <summary>
    /// MODIFIED: Handles login with either Email OR Username.
    /// </summary>
    private async void OnLoginClicked(ClickEvent evt)
    {
        string loginInput = _emailField.value;
        string password = _passwordField.value;
        string emailToLogin;

        // --- NEW LOGIC: Check if input is Email or Username ---
        if (loginInput.Contains("@"))
        {
            // Input is an email, use it directly
            emailToLogin = loginInput;
        }
        else
        {
            // Input is a username, look up the email in Firestore
            Debug.Log($"Input '{loginInput}' is a username. Looking up email...");
            emailToLogin = await _firestoreManager.GetEmailFromUsername(loginInput);

            if (string.IsNullOrEmpty(emailToLogin))
            {
                Debug.LogError($"Login Failed: No user found with username '{loginInput}'.");
                // TODO: Show this error to the user in the UI
                return;
            }
            Debug.Log($"Found email '{emailToLogin}' for username '{loginInput}'. Proceeding with login.");
        }
        // --------------------------------------------------------

        // Proceed with login using the determined email
        try
        {
            AuthResult result = await auth.SignInWithEmailAndPasswordAsync(emailToLogin, password);
            FirebaseUser user = result.User;
            Debug.Log($"User logged in successfully: {user.DisplayName ?? "N/A"} ({user.UserId})");

            // --- NEW: Save data to manager --
            GameDataManager.Instance.SetUserData(user, user.DisplayName, user.Email);
            LoadNextScene();
        }
        catch (System.Exception e)
        {
            // Safely extract the Firebase error code
            string errorCode = string.Empty;
            if (e.InnerException is Firebase.FirebaseException firebaseEx)
            {
                // This is the correct way to get the auth error code
                errorCode = ((AuthError)firebaseEx.ErrorCode).ToString();
            }

            if (errorCode == "WrongPassword") // Use the string name of the AuthError enum
            {
                Debug.LogError("Login Failed: Incorrect password for existing user.");
                // TODO: Show "Incorrect password" to the user
            }
            else if (errorCode == "UserNotFound")
            {
                Debug.LogWarning($"Login Failed: No account found for email '{emailToLogin}'.");
                // This can happen if the username was found, but the linked email isn't in Firebase Auth
                // (which shouldn't happen) or if the user typed a non-existent email.
                // TODO: Show "User not found" to the user
            }
            else if (errorCode == "InvalidEmail") // This was the original error
            {
                Debug.LogError($"Login Failed (Invalid Email): {e.Message}. This shouldn't happen with the new logic.");
            }
            else
            {
                // Log the raw error for debugging
                Debug.LogError($"Login Failed (General Error): {e.Message} | Error Code: {errorCode}");
                // TODO: Show a generic "Login Failed" error to the user
            }
        }
    }


    // =======================================================
    // NEW MODAL/PROFILE METHODS
    // =======================================================

    /// <summary>
    /// Shows or hides the profile setup modal.
    /// </summary>
    /// <param name="show">True to show, false to hide.</param>
    private void ShowProfileSetupModal(bool show)
    {
        if (_profileSetupModal != null)
        {
            _profileSetupModal.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            if (show)
            {
                // Clear all fields and reset icons
                _usernameFieldModal.value = "";
                _newPasswordFieldModal.value = "";
                _confirmPasswordFieldModal.value = "";
                _statusMessageModal.text = "Set a unique username and password."; // Back to original text

                // --- ALWAYS Show and Reset Password Fields ---
                if (_passwordContainerNew != null) _passwordContainerNew.style.display = DisplayStyle.Flex;
                if (_passwordContainerConfirm != null) _passwordContainerConfirm.style.display = DisplayStyle.Flex;

                _newPasswordFieldModal.isPasswordField = true;
                _confirmPasswordFieldModal.isPasswordField = true;
                if (_showNewPasswordEye != null) _showNewPasswordEye.text = EYE_ICON;
                if (_showConfirmPasswordEye != null) _showConfirmPasswordEye.text = EYE_ICON;
            }
        }
    }

    /// <summary>
    /// Called when the "START GAME" button in the modal is clicked.
    /// </summary>
    private async void OnSubmitProfileClicked(ClickEvent evt)
    {
        _submitProfileButton.SetEnabled(false);
        _statusMessageModal.text = "Creating profile...";

        string username = _usernameFieldModal.value;
        string password = _newPasswordFieldModal.value;
        string confirmPassword = _confirmPasswordFieldModal.value;

        // This function will now run the full logic for everyone
        await LinkAccountAndSaveProfile(username, password, confirmPassword);

        // Re-enable button if an error occurred (success hides modal)
        _submitProfileButton.SetEnabled(true);
    }

    /// <summary>
    /// NEW: Validates a password based on specific rules.
    /// </summary>
    /// <param name="password">The password string to validate.</param>
    /// <returns>A tuple: (bool IsValid, string ErrorMessage)</returns>
    private (bool IsValid, string ErrorMessage) ValidatePassword(string password)
    {
        if (string.IsNullOrEmpty(password) || password.Length < 8)
        {
            return (false, "Password must be at least 8 characters.");
        }
        if (!password.Any(char.IsUpper))
        {
            return (false, "Password must contain at least one uppercase letter.");
        }
        if (!password.Any(c => !char.IsLetterOrDigit(c)))
        {
            return (false, "Password must contain at least one special character.");
        }
        return (true, string.Empty);
    }


    /// <summary>
    /// Core logic to link email/password, set username, and save to Firestore.
    /// </summary>
    private async Task LinkAccountAndSaveProfile(string username, string password, string confirmPassword)
    {
        try
        {
            // --- 1. Validation (Always runs) ---
            if (string.IsNullOrWhiteSpace(username))
            {
                _statusMessageModal.text = "Username cannot be empty.";
                return;
            }

            // --- UPDATED Password Validation ---
            var (isValid, errorMessage) = ValidatePassword(password);
            if (!isValid)
            {
                _statusMessageModal.text = errorMessage;
                return;
            }
            // -----------------------------------

            if (password != confirmPassword)
            {
                _statusMessageModal.text = "Passwords do not match.";
                return;
            }

            // --- 2. Check Username Uniqueness (Always runs) ---
            _statusMessageModal.text = "Checking username...";
            bool isTaken = await _firestoreManager.IsUsernameTaken(username);
            if (isTaken)
            {
                _statusMessageModal.text = "That username is already taken. Try another.";
                return;
            }

            // --- 3. Get Current User (Always runs) ---
            FirebaseUser user = auth.CurrentUser;
            if (user == null)
            {
                _statusMessageModal.text = "Error: No user is signed in.";
                return;
            }

            // --- 4. Link Password Credential (Always runs) ---
            _statusMessageModal.text = "Linking account...";
            Credential credential = EmailAuthProvider.GetCredential(user.Email, password);
            await user.LinkWithCredentialAsync(credential);
            Debug.Log("Password credential successfully linked to Google account.");

            // --- 5. Update Auth Profile (Always runs) ---
            _statusMessageModal.text = "Saving profile...";
            UserProfile profile = new UserProfile { DisplayName = username };
            await user.UpdateUserProfileAsync(profile);
            Debug.Log("Firebase Auth DisplayName updated.");

            // --- 6. Save Profile to Firestore (Always runs) ---
            await _firestoreManager.SaveUserProfile(user, username, user.Email);
            Debug.Log("User profile saved to Firestore.");

            // --- 7. Success & Navigation ---
            _statusMessageModal.text = "Success!";
            Debug.Log("Account linked and profile saved successfully!");
            ShowProfileSetupModal(false);

            GameDataManager.Instance.SetUserData(user, username, user.Email);
            LoadNextScene();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to link account and save profile: {e.Message}");
            _statusMessageModal.text = "Error: Could not create profile. Please try again.";
        }
        // We don't need the 'finally' block anymore
    }

    // =======================================================
    // ADDED: FORGOT PASSWORD METHODS
    // =======================================================

    /// <summary>
    /// Shows or hides the "Forgot Password" modal.
    /// </summary>
    private void ShowForgotPasswordModal(bool show)
    {
        if (_forgotPasswordModal != null)
        {
            _forgotPasswordModal.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;
            if (show)
            {
                // Reset fields
                _emailFieldForgot.value = "";
                _statusMessageForgot.text = "Enter your account's email address.";
                _statusMessageForgot.style.color = new StyleColor(new Color(0.95f, 0.90f, 0.56f)); // Reset to default color (e.g., your yellow)
                _sendResetButton.SetEnabled(true);
            }
        }
    }

    /// <summary>
    /// Called when the "Forgot Password?" link is clicked.
    /// </summary>
    private void OnForgotPasswordClicked(ClickEvent evt)
    {
        Debug.Log("Forgot Password link clicked.");
        ShowForgotPasswordModal(true);
    }

    /// <summary>
    /// Called when the modal's 'X' (close) button is clicked.
    /// </summary>
    private void OnCloseForgotPasswordClicked(ClickEvent evt)
    {
        ShowForgotPasswordModal(false);
    }

    /// <summary>
    /// Called when the "SEND RESET EMAIL" button is clicked.
    /// </summary>
    private async void OnSendResetEmailClicked(ClickEvent evt)
    {
        string email = _emailFieldForgot.value;

        // Simple email validation
        if (string.IsNullOrEmpty(email) || !email.Contains("@"))
        {
            _statusMessageForgot.text = "Please enter a valid email address.";
            _statusMessageForgot.style.color = new StyleColor(Color.red);
            return;
        }

        _sendResetButton.SetEnabled(false);
        _statusMessageForgot.text = "Sending reset email...";
        _statusMessageForgot.style.color = new StyleColor(new Color(0.95f, 0.90f, 0.56f)); // Back to default color

        try
        {
            await auth.SendPasswordResetEmailAsync(email);

            // Success
            Debug.Log($"Password reset email sent to {email}.");
            _statusMessageForgot.text = "Reset email sent. Check your inbox (and spam folder).";
            _statusMessageForgot.style.color = new StyleColor(Color.green);
            // Optionally, close the modal after a delay
            // await Task.Delay(3000);
            // ShowForgotPasswordModal(false);
        }
        catch (System.Exception e)
        {
            // Handle errors
            string errorCode = string.Empty;
            if (e.InnerException is Firebase.FirebaseException firebaseEx)
            {
                errorCode = ((AuthError)firebaseEx.ErrorCode).ToString();
            }

            if (errorCode == "UserNotFound")
            {
                Debug.LogWarning($"Password reset failed: No user found for email '{email}'.");
                _statusMessageForgot.text = "No account found with that email address.";
            }
            else
            {
                Debug.LogError($"Password reset failed: {e.Message} | Error Code: {errorCode}");
                _statusMessageForgot.text = "An error occurred. Please try again.";
            }
            _statusMessageForgot.style.color = new StyleColor(Color.red);
            _sendResetButton.SetEnabled(true);
        }
    }


    // =======================================================
    // AUTH METHODS (Modified)
    // =======================================================

    private async void OnGoogleClicked(ClickEvent evt)
    {
        Debug.Log("Starting Google SSO (PC Browser Flow) for Login/Registration...");

        try
        {
            // --- STEP 1: Get Authorization Code via Browser ---
            var oauthFlow = new OAuthFlow();
            string authorizationCode = await RunAuthorizationFlow(oauthFlow);

            if (string.IsNullOrEmpty(authorizationCode))
            {
                Debug.LogError("Authorization code not received from browser.");
                return;
            }

            // --- STEP 2: Exchange Code for ID Token (Requires Network Logic) ---
            string idToken = await ExchangeCodeForToken(authorizationCode);

            if (string.IsNullOrEmpty(idToken))
            {
                Debug.LogError("Failed to exchange authorization code for ID Token.");
                return;
            }

            // --- STEP 3: Sign in to Firebase (Handles Login/Registration) ---
            Credential credential = GoogleAuthProvider.GetCredential(idToken, null);
            AuthResult result = await auth.SignInAndRetrieveDataWithCredentialAsync(credential);

            FirebaseUser user = result.User;
            Debug.Log($"Google SSO Complete. User: {user.DisplayName} ({user.UserId})");

            // --- STEP 4: (REVISED) Check if User Profile is Complete ---
            Debug.Log($"Checking Firestore for profile for user: {user.UserId}");
            bool profileExists = await _firestoreManager.DoesUserProfileExist(user.UserId);

            if (!profileExists)
            {
                // --- NEW USER ---
                Debug.Log("User profile not found in Firestore. Showing profile setup modal.");
                // --- REMOVED ---
                // _isGoogleSignUp = true; 

                // Just show the modal. It will now ask for username AND password.
                ShowProfileSetupModal(true);
            }
            else
            {
                // --- EXISTING USER ---
                Debug.Log("User profile found in Firestore. Loading next scene.");
                GameDataManager.Instance.SetUserData(user, user.DisplayName, user.Email);
                LoadNextScene();
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Google SSO failed: {e.Message}");
            // --- REMOVED ---
            // _isGoogleSignUp = false;
        }
    }


    // =======================================================
    // Helper Methods for the Browser Flow (Need to be implemented inside LoginScreen.cs)
    // =======================================================

    private async Task<string> RunAuthorizationFlow(OAuthFlow oauthFlow)
    {
        // 1. Construct the Google Authorization URL
        string scope = "email%20profile";
        string authUrl = $"https://accounts.google.com/o/oauth2/v2/auth?" +
                         $"client_id={WebClientId}&" +
                         $"redirect_uri={OAuthFlow.RedirectUri}&" +
                         $"response_type=code&" +
                         $"scope={scope}&" +
                         $"prompt=select_account%20consent";

        // 2. Start the local listener
        Task<string> listenerTask = oauthFlow.StartListenerAndGetCode();

        // 3. Open the URL in the user's default browser
        Application.OpenURL(authUrl);

        // 4. Wait for the listener to receive the authorization code
        return await listenerTask;
    }

    private async Task<string> ExchangeCodeForToken(string code)
    {
        Debug.Log("Executing network request to exchange code for token...");

        // The endpoint to exchange the code for the token
        const string TokenEndpoint = "https://oauth2.googleapis.com/token";

        // Build the POST data
        WWWForm form = new WWWForm();
        form.AddField("code", code);
        form.AddField("client_id", WebClientId);
        form.AddField("client_secret", ClientSecret);
        form.AddField("redirect_uri", OAuthFlow.RedirectUri);
        form.AddField("grant_type", "authorization_code");

        // Send the POST request
        using (UnityWebRequest www = UnityWebRequest.Post(TokenEndpoint, form))
        {
            // Must use an Async operation wrapper for UnityWebRequest to work with async/await
            var operation = www.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield(); // Wait for the next frame
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Token Exchange Error: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler.text}");
                return null;
            }

            // Deserialize the JSON response
            string jsonResponse = www.downloadHandler.text;
            GoogleTokenResponse tokenResponse = JsonUtility.FromJson<GoogleTokenResponse>(jsonResponse);

            if (string.IsNullOrEmpty(tokenResponse.id_token))
            {
                Debug.LogError("Token Exchange failed: ID Token is missing in response.");
                return null;
            }

            Debug.Log("Successfully received ID Token.");
            return tokenResponse.id_token;
        }
    }

    private async void OnContinueAsGuestClicked(ClickEvent evt)
    {
        Debug.Log("Continuing as Guest (Anonymous Sign-in)...");

        try
        {
            AuthResult result = await auth.SignInAnonymouslyAsync();
            FirebaseUser user = result.User;
            Debug.Log($"Guest user signed in successfully. User ID: {user.UserId}");

            // --- NEW: Save guest data to manager ---
            GameDataManager.Instance.SetUserData(user, "Guest", null);

            LoadNextScene();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Guest Login (Anonymous Auth) failed: {e.Message}");
        }
    }

    // Unchanged Task for email/password registration (if you still need it)
    private async Task OnRegisterNewUserClicked(bool isAutoAttempt = false)
    {
        // This logic is now handled by the modal flow, but keeping
        // the original method in case you use it for a separate "Sign Up" button.
        string email = _emailField.value;
        string password = _passwordField.value;

        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(password) || password.Length < 6)
        {
            Debug.LogError("Registration failed: Invalid email or password (6+ chars).");
            return;
        }
        try
        {
            AuthResult result = await auth.CreateUserWithEmailAndPasswordAsync(email, password);
            FirebaseUser user = result.User;
            Debug.Log($"New user registered successfully: {user.Email} ({user.UserId})");
            // After registration, you might want to show the profile modal too
            // or just load the next scene.
            // For now, it loads the next scene.
            LoadNextScene();
        }
        catch (System.Exception e)
        {
            Debug.LogError($"User Registration failed: {e.Message}");
        }
    }


    /// <summary>
    /// REFACTORED: Toggles password visibility for any given password field and eye icon.
    /// </summary>
    /// <param name="field">The TextField to toggle.</param>
    /// <param name="eyeIcon">The Label icon to update.</param>
    private void OnTogglePasswordVisibility(TextField field, Label eyeIcon)
    {
        if (field != null)
        {
            // Toggle the password field's mask state
            field.isPasswordField = !field.isPasswordField;

            // Update the icon text based on the new state
            if (field.isPasswordField)
            {
                eyeIcon.text = EYE_ICON;
            }
            else
            {
                eyeIcon.text = EYE_SLASH_ICON;
            }
        }
    }

    private void LoadNextScene()
    {
        try
        {
            SceneManager.LoadScene(NextSceneName);
            Debug.Log($"Successfully loaded scene: {NextSceneName}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to load scene '{NextSceneName}'. Check Build Settings. Error: {e.Message}");
        }
    }
}
