using UnityEngine;
using UnityEngine.UIElements;
using Firebase.Auth; 
using UnityEngine.SceneManagement; 
using Firebase.Firestore;
using System.Threading.Tasks;
using UnityEngine.Networking;
using System.Linq; 

// You can delete the "Popup.UI" namespace
// using Popup.UI; 

[RequireComponent(typeof(UIDocument))]
public class MainMenuController : MonoBehaviour
{
    private VisualElement m_Root;

    // --- Main Menu Buttons ---
    private Button m_CampaignButton;
    private Button m_ArcadeButton;
    private Button m_SettingsButton;
    private Button m_LeaderboardButton;
    private Button m_CreditsButton;
    private Button m_QuitButton;
    private Button m_ShopButton;
    private Button m_UserButton;

    // --- Profile Popup Elements (NEW) ---
    private VisualElement m_ProfilePopup;
    private Button m_ProfileCloseButton;
    private Button m_ProfileLogOutButton;
    private TextField m_ProfileUsernameField;
    private Label m_ProfileEmailLabel;
    private VisualElement m_ProfileEmailIcon;

    // --- NEW: Guest Sign-in Button ---
    private Button m_GuestSignInButton;

    // --- NEW: Firebase Auth/Firestore ---
    private FirebaseAuth auth;
    private FirestoreManager _firestoreManager;
    private const string WebClientId = "1022881527607-vlffirg46mkbvev5gh119i4eb0mslfgb.apps.googleusercontent.com"; // From LoginScreen.cs
    private const string ClientSecret = "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"; // From LoginScreen.cs

    // --- NEW: Modal UI Variables ---
    private VisualElement _profileSetupModal;
    private TextField _usernameFieldModal;
    private TextField _newPasswordFieldModal;
    private TextField _confirmPasswordFieldModal;
    private Label _statusMessageModal;
    private Button _submitProfileButton;

    // --- NEW: Eye icons for modal ---
    private Label _showNewPasswordEye;
    private Label _showConfirmPasswordEye;
    private const string EYE_ICON = "\uf06e"; // fa-eye
    private const string EYE_SLASH_ICON = "\uf070"; // fa-eye-slash

    // --- MODIFIED: Use Awake to initialize Firebase ---
    private void Awake()
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

    private void OnEnable()
    {
        m_Root = GetComponent<UIDocument>().rootVisualElement;

        if (m_Root == null)
        {
            Debug.LogError("MainMenuController: Could not find root visual element.");
            return;
        }

        // --- Query Main Menu Buttons ---
        m_CampaignButton = m_Root.Q<Button>("CampaignButton");
        m_ArcadeButton = m_Root.Q<Button>("ArcadeButton");
        m_SettingsButton = m_Root.Q<Button>("SettingsButton");
        m_LeaderboardButton = m_Root.Q<Button>("LeaderboardButton");
        m_CreditsButton = m_Root.Q<Button>("CreditsButton");
        m_QuitButton = m_Root.Q<Button>("QuitButton");
        m_ShopButton = m_Root.Q<Button>("ShopButton");
        m_UserButton = m_Root.Q<Button>("UserButton");

        // --- Query Popup Elements (NEW) ---
        // 1. Find the popup container itself
        m_ProfilePopup = m_Root.Q<VisualElement>("ProfilePopup");
        if (m_ProfilePopup == null)
        {
            Debug.LogError("MainMenuController: Could not find 'ProfilePopup' element. Check UXML name.");
            return;
        }

        // 2. Find the buttons *inside* the popup
        m_ProfileCloseButton = m_ProfilePopup.Q<Button>("CloseButton");
        m_ProfileLogOutButton = m_ProfilePopup.Q<Button>("LogOutButton");
        m_GuestSignInButton = m_ProfilePopup.Q<Button>("GuestSignInButton");


        // Find the containers named in the UXML
        var usernameContainer = m_ProfilePopup.Q<VisualElement>("username-field-container");
        var emailContainer = m_ProfilePopup.Q<VisualElement>("email-field-container");
        // Find the labels *within* those containers by their class
        if (usernameContainer != null)
        {

            m_ProfileUsernameField = usernameContainer.Q<TextField>("username-text-field");
        }
        if (emailContainer != null)
        {
            m_ProfileEmailLabel = emailContainer.Q<Label>(className: "input-label");
            m_ProfileEmailIcon = emailContainer.Q<VisualElement>("email-provider-icon");
        }

        // --- NEW: Query for Modal Elements (relative to m_ProfilePopup) ---
        _profileSetupModal = m_ProfilePopup.Q<VisualElement>("profile-setup-modal");
        _usernameFieldModal = m_ProfilePopup.Q<TextField>("username-field-modal");
        _newPasswordFieldModal = m_ProfilePopup.Q<TextField>("new-password-field-modal");
        _confirmPasswordFieldModal = m_ProfilePopup.Q<TextField>("confirm-password-field-modal");
        _statusMessageModal = m_ProfilePopup.Q<Label>("status-message-modal");
        _submitProfileButton = m_ProfilePopup.Q<Button>("submit-profile-button");
        _showNewPasswordEye = m_ProfilePopup.Q<Label>("eye-icon-new");
        _showConfirmPasswordEye = m_ProfilePopup.Q<Label>("eye-icon-confirm");

        // --- Register Main Menu Callbacks ---
        m_CampaignButton?.RegisterCallback<ClickEvent>(OnCampaignClicked);
        m_ArcadeButton?.RegisterCallback<ClickEvent>(OnArcadeClicked);
        m_SettingsButton?.RegisterCallback<ClickEvent>(OnSettingsClicked);
        m_LeaderboardButton?.RegisterCallback<ClickEvent>(OnLeaderboardClicked);
        m_CreditsButton?.RegisterCallback<ClickEvent>(OnCreditsClicked);
        m_QuitButton?.RegisterCallback<ClickEvent>(OnQuitClicked);
        m_ShopButton?.RegisterCallback<ClickEvent>(OnShopClicked);
        m_UserButton?.RegisterCallback<ClickEvent>(OnUserClicked);

        // --- Register Popup Callbacks (NEW) ---
        m_ProfileCloseButton?.RegisterCallback<ClickEvent>(OnProfileCloseClicked);
        m_ProfileLogOutButton?.RegisterCallback<ClickEvent>(OnProfileLogOutClicked);
        m_ProfileUsernameField?.RegisterCallback<FocusOutEvent>(OnUsernameChanged);
        // --- NEW: Register Modal & Guest Button Callbacks ---
        m_GuestSignInButton?.RegisterCallback<ClickEvent>(OnGuestSignInClicked);
        _submitProfileButton?.RegisterCallback<ClickEvent>(OnSubmitProfileClicked);
        _showNewPasswordEye?.RegisterCallback<ClickEvent>(evt => OnTogglePasswordVisibility(_newPasswordFieldModal, _showNewPasswordEye));
        _showConfirmPasswordEye?.RegisterCallback<ClickEvent>(evt => OnTogglePasswordVisibility(_confirmPasswordFieldModal, _showConfirmPasswordEye));


        // --- Populate Profile Data (NEW) ---
        PopulateProfilePopup();
        // --- Hide Popup By Default (IMPORTANT) ---
        // Your UXML has it as "visibility: visible", so we must hide it on start.
        HideProfilePopup();
        ShowProfileSetupModal(false);
    }

    private void OnDisable()
    {
        // --- Unregister Main Menu Callbacks ---
        m_CampaignButton?.UnregisterCallback<ClickEvent>(OnCampaignClicked);
        m_ArcadeButton?.UnregisterCallback<ClickEvent>(OnArcadeClicked);
        m_SettingsButton?.UnregisterCallback<ClickEvent>(OnSettingsClicked);
        m_LeaderboardButton?.UnregisterCallback<ClickEvent>(OnLeaderboardClicked);
        m_CreditsButton?.UnregisterCallback<ClickEvent>(OnCreditsClicked);
        m_QuitButton?.UnregisterCallback<ClickEvent>(OnQuitClicked);
        m_ShopButton?.UnregisterCallback<ClickEvent>(OnShopClicked);
        m_UserButton?.UnregisterCallback<ClickEvent>(OnUserClicked);

        // --- Unregister Popup Callbacks (NEW) ---
        m_ProfileCloseButton?.UnregisterCallback<ClickEvent>(OnProfileCloseClicked);
        m_ProfileLogOutButton?.UnregisterCallback<ClickEvent>(OnProfileLogOutClicked);
        m_ProfileUsernameField?.UnregisterCallback<FocusOutEvent>(OnUsernameChanged);

        // --- NEW: Unregister Modal & Guest Button Callbacks ---
        m_GuestSignInButton?.UnregisterCallback<ClickEvent>(OnGuestSignInClicked);
        _submitProfileButton?.UnregisterCallback<ClickEvent>(OnSubmitProfileClicked);
        _showNewPasswordEye?.UnregisterCallback<ClickEvent>(evt => OnTogglePasswordVisibility(_newPasswordFieldModal, _showNewPasswordEye));
        _showConfirmPasswordEye?.UnregisterCallback<ClickEvent>(evt => OnTogglePasswordVisibility(_confirmPasswordFieldModal, _showConfirmPasswordEye));
    }

    // --- New Popup Show/Hide Methods (NEW) ---
    private void ShowProfilePopup()
    {
        if (m_ProfilePopup != null)
        {
            m_ProfilePopup.style.display = DisplayStyle.Flex;
        }
    }

    private void HideProfilePopup()
    {
        if (m_ProfilePopup != null)
        {
            m_ProfilePopup.style.display = DisplayStyle.None;
        }
    }

    // --- New Popup Click Handlers (NEW) ---
    private void OnProfileCloseClicked(ClickEvent evt)
    {
        Debug.Log("Profile Close button clicked");
        HideProfilePopup();
    }

    private void OnProfileLogOutClicked(ClickEvent evt)
    {
        Debug.Log("Profile Log Out button clicked!");

        // --- NEW: Logout Logic ---
        // 1. Sign out of Firebase
        FirebaseAuth.DefaultInstance.SignOut();

        // 2. Clear the persistent data
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.ClearData();
        }

        // 3. Load the Login scene
        // (Ensure "LoginScreen" is in your File > Build Settings)
        SceneManager.LoadScene("Login");
    }

    private void OnCampaignClicked(ClickEvent evt)
    {
        Debug.Log("Campaign Button Clicked! Loading Campaign scene...");
    }

    private void OnArcadeClicked(ClickEvent evt)
    {
        Debug.Log("Arcade Button Clicked! Loading Arcade scene...");
    }

    private void OnSettingsClicked(ClickEvent evt)
    {
        Debug.Log("Settings Button Clicked! Opening settings panel...");
    }

    private void OnLeaderboardClicked(ClickEvent evt)
    {
        Debug.Log("Leaderboard Button Clicked! Opening leaderboard panel...");
    }

    private void OnCreditsClicked(ClickEvent evt)
    {
        Debug.Log("Credits Button Clicked! Loading Credits scene...");
    }

    private void OnQuitClicked(ClickEvent evt)
    {
        Debug.Log("Quit Button Clicked! Quitting application...");

#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#else
        Application.Quit();
#endif
    }

    private void OnShopClicked(ClickEvent evt)
    {
        Debug.Log("Shop Button Clicked! Opening shop panel...");
    }

    // --- UPDATED User Button Handler ---
    private void OnUserClicked(ClickEvent evt)
    {
        Debug.Log("User Button Clicked! Opening profile panel.");
        // --- NEW: Refresh data just in case ---
        PopulateProfilePopup();
        // This is much simpler now:
        ShowProfilePopup();
    }


    // --- NEW: Method to populate profile data ---
    private void PopulateProfilePopup()
    {
        // 1. Check if the manager exists
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("GameDataManager not found. Cannot populate profile.");
            return;
        }

        // 2. Get the user data
        FirebaseUser user = GameDataManager.Instance.CurrentUser;
        if (user == null)
        {
            // --- Handle No User ---
            if (m_ProfileUsernameField != null)
            {
                m_ProfileUsernameField.value = "Guest";
                m_ProfileUsernameField.isReadOnly = true;
            }
            if (m_ProfileEmailLabel != null) m_ProfileEmailLabel.text = "Not signed in";
            if (m_ProfileEmailIcon != null) m_ProfileEmailIcon.style.display = DisplayStyle.None;
            // --- NEW: Hide both buttons if no user ---
            if (m_ProfileLogOutButton != null) m_ProfileLogOutButton.style.display = DisplayStyle.None;
            if (m_GuestSignInButton != null) m_GuestSignInButton.style.display = DisplayStyle.None;
            return;
        }

        // 3. Handle Anonymous "Guest" user
        if (user.IsAnonymous)
        {
            if (m_ProfileUsernameField != null)
            {
                m_ProfileUsernameField.value = "Guest";
                m_ProfileUsernameField.isReadOnly = true;
            }
            if (m_ProfileEmailLabel != null) m_ProfileEmailLabel.text = "Not signed in";
            if (m_ProfileEmailIcon != null) m_ProfileEmailIcon.style.display = DisplayStyle.None;

            if (m_ProfileLogOutButton != null) m_ProfileLogOutButton.style.display = DisplayStyle.None;
            if (m_GuestSignInButton != null) m_GuestSignInButton.style.display = DisplayStyle.Flex;
        }
        // 4. Handle regular (Email/Google) user
        else
        {
            if (m_ProfileUsernameField != null)
            {
                m_ProfileUsernameField.value = GameDataManager.Instance.Username ?? "No Username Set";
                m_ProfileUsernameField.isReadOnly = false;
            }
            if (m_ProfileEmailLabel != null)
            {
                m_ProfileEmailLabel.text = GameDataManager.Instance.Email ?? "No Email";
            }


            // 5. Set the icon based on the provider
            if (m_ProfileEmailIcon != null)
            {
                m_ProfileEmailIcon.style.display = DisplayStyle.Flex; // Show icon
                bool isGoogle = false;

                foreach (var provider in user.ProviderData)
                {
                    if (provider.ProviderId == GoogleAuthProvider.ProviderId)
                    {
                        isGoogle = true;
                        break;
                    }
                }

                // --- MODIFIED: Use USS classes to set icon ---
                if (isGoogle)
                {
                    // You defined '.google-icon' in your USS to have the image
                    m_ProfileEmailIcon.AddToClassList("google-icon");
                }
                else
                {
                    // Not Google, so remove the class (icon will be blank container)
                    m_ProfileEmailIcon.RemoveFromClassList("google-icon");
                    // Or you could add an "email-icon" class here
                }
            }
            if (m_ProfileLogOutButton != null) m_ProfileLogOutButton.style.display = DisplayStyle.Flex;
            if (m_GuestSignInButton != null) m_GuestSignInButton.style.display = DisplayStyle.None;
        }
    }

    // --- NEW: Method to handle saving the username ---
    private void OnUsernameChanged(FocusOutEvent evt)
    {
        if (GameDataManager.Instance == null || GameDataManager.Instance.CurrentUser == null || GameDataManager.Instance.CurrentUser.IsAnonymous)
        {
            // Don't save if it's a guest or data is missing
            return;
        }

        string newUsername = m_ProfileUsernameField.value;
        string oldUsername = GameDataManager.Instance.Username;

        // Trim whitespace and check if value actually changed
        if (!string.IsNullOrWhiteSpace(newUsername) && newUsername.Trim() != oldUsername)
        {
            newUsername = newUsername.Trim();
            Debug.Log($"Username change requested from '{oldUsername}' to '{newUsername}'.");

            // --- IMPORTANT: SAVE LOGIC REQUIRED ---
            // This only logs the change. You must now save it to Firebase.
            // You will need access to your FirestoreManager.
            //
            // 1. Check if newUsername is already taken in Firestore.
            // 2. Update Firebase Auth profile: 
            //    auth.CurrentUser.UpdateUserProfileAsync(new UserProfile { DisplayName = newUsername });
            // 3. Update Firestore 'users' collection with new username.
            // 4. *Delete* old username from 'usernames' collection and *add* the new one.
            // 5. Update GameDataManager: 
            //    GameDataManager.Instance.SetUserData(user, newUsername, user.Email);

            // For now, we just update the local GameDataManager instance
            GameDataManager.Instance.SetUserData(GameDataManager.Instance.CurrentUser, newUsername, GameDataManager.Instance.Email);
        }
        else
        {
            // Revert to old username if it's empty or unchanged
            m_ProfileUsernameField.value = oldUsername;
        }
    }

    private void OnGuestSignInClicked(ClickEvent evt)
    {
        // This just starts the same Google flow from the Login screen.
        ShowProfileSetupModal(true);
    }
    private void ShowProfileSetupModal(bool show)
    {
        if (_profileSetupModal != null)
        {
            _profileSetupModal.style.display = show ? DisplayStyle.Flex : DisplayStyle.None;

            if (show)
            {
                _usernameFieldModal.value = "";
                _newPasswordFieldModal.value = "";
                _confirmPasswordFieldModal.value = "";
                _statusMessageModal.text = "";
                _newPasswordFieldModal.isPasswordField = true;
                _confirmPasswordFieldModal.isPasswordField = true;
                if (_showNewPasswordEye != null) _showNewPasswordEye.text = EYE_ICON;
                if (_showConfirmPasswordEye != null) _showConfirmPasswordEye.text = EYE_ICON;
            }
        }
    }
    private async void OnSubmitProfileClicked(ClickEvent evt)
    {
        _submitProfileButton.SetEnabled(false);
        _statusMessageModal.text = "Creating profile...";

        string username = _usernameFieldModal.value;
        string password = _newPasswordFieldModal.value;
        string confirmPassword = _confirmPasswordFieldModal.value;

        // This function now contains all the new logic
        await LinkAccountAndSaveProfile(username, password, confirmPassword);

        _submitProfileButton.SetEnabled(true);
    }

    // --- ADDED: Password validation function ---
    /// <summary>
    /// Validates a password based on specific rules.
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

    private async Task LinkAccountAndSaveProfile(string username, string password, string confirmPassword)
    {
        try
        {
            // --- 1. Validation (NOW INCLUDES STRONG PASSWORD CHECK) ---
            if (string.IsNullOrWhiteSpace(username))
            {
                _statusMessageModal.text = "Username cannot be empty.";
                return;
            }

            // --- UPDATED Strong Password Validation ---
            var (isValid, errorMessage) = ValidatePassword(password);
            if (!isValid)
            {
                _statusMessageModal.text = errorMessage;
                return;
            }
            // ------------------------------------

            if (password != confirmPassword)
            {
                _statusMessageModal.text = "Passwords do not match.";
                return;
            }

            // --- 2. Check Username Uniqueness ---
            _statusMessageModal.text = "Checking username...";
            bool isTaken = await _firestoreManager.IsUsernameTaken(username);
            if (isTaken)
            {
                _statusMessageModal.text = "That username is already taken. Try another.";
                return;
            }

            // --- 3. NEW: Run Google SSO Flow ---
            _statusMessageModal.text = "Please log in with Google to link your account...";
            var oauthFlow = new OAuthFlow();
            string authorizationCode = await RunAuthorizationFlow(oauthFlow);

            if (string.IsNullOrEmpty(authorizationCode))
            {
                Debug.LogError("Authorization code not received from browser.");
                _statusMessageModal.text = "Google login cancelled or failed.";
                return;
            }

            _statusMessageModal.text = "Authenticating with Google...";
            string idToken = await ExchangeCodeForToken(authorizationCode);

            if (string.IsNullOrEmpty(idToken))
            {
                Debug.LogError("Failed to exchange authorization code for ID Token.");
                _statusMessageModal.text = "Google authentication failed.";
                return;
            }

            Credential googleCredential = GoogleAuthProvider.GetCredential(idToken, null);

            // --- 4. NEW: Link Guest User to Google Credential ---
            _statusMessageModal.text = "Linking Google account...";
            FirebaseUser guestUser = auth.CurrentUser;
            if (guestUser == null || !guestUser.IsAnonymous)
            {
                Debug.LogError("LinkAccount: No Guest user is currently logged in. Aborting.");
                _statusMessageModal.text = "Error: Could not find guest user to link.";
                return;
            }

            AuthResult result = await guestUser.LinkWithCredentialAsync(googleCredential);
            FirebaseUser linkedUser = result.User; // This is now the permanent, linked user
            Debug.Log($"Google SSO Link Complete. User: {linkedUser.DisplayName} ({linkedUser.UserId})");

            // --- 5. Link Password Credential (Modified to use linkedUser) ---
            _statusMessageModal.text = "Linking password...";
            // We get the email from the newly linked Google account
            Credential passwordCredential = EmailAuthProvider.GetCredential(linkedUser.Email, password);
            await linkedUser.LinkWithCredentialAsync(passwordCredential);
            Debug.Log("Password credential successfully linked to Google account.");

            // --- 6. Update Auth Profile (Display Name) (Modified to use linkedUser) ---
            _statusMessageModal.text = "Saving profile...";
            UserProfile profile = new UserProfile { DisplayName = username };
            await linkedUser.UpdateUserProfileAsync(profile);
            Debug.Log("Firebase Auth DisplayName updated.");

            // --- 7. Save Profile to Firestore (Modified to use linkedUser) ---
            await _firestoreManager.SaveUserProfile(linkedUser, username, linkedUser.Email);
            Debug.Log("User profile saved to Firestore.");

            // --- 8. Success & Navigation ---
            _statusMessageModal.text = "Success!";
            Debug.Log("Account linked and profile saved successfully!");

            // --- 9. Update GameDataManager (This is what you asked for) ---
            GameDataManager.Instance.SetUserData(linkedUser, username, linkedUser.Email);

            // --- 10. Hide modal and refresh profile ---
            ShowProfileSetupModal(false);
            PopulateProfilePopup(); // This will now show the new user's data
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Failed to link account and save profile: {e.Message}");
            // TODO: Add more specific error handling (e.g., if Google link fails, if password link fails)
            // For example, if the password link fails, you may need to unlink the Google credential
            // or ask the user to try setting a password again.
            // For now, a generic error is shown.
            _statusMessageModal.text = "Error: Could not create profile. Please try again.";
        }
    }

    /* private async void OnGoogleClicked(ClickEvent evt)
    {
        Debug.Log("Starting Google SSO (PC Browser Flow) for Account Linking...");

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

            // --- STEP 3: Get the credential ---
            Credential credential = GoogleAuthProvider.GetCredential(idToken, null);

            // --- STEP 4: Link Guest User to Google Credential ---
            FirebaseUser guestUser = auth.CurrentUser;
            if (guestUser == null || !guestUser.IsAnonymous)
            {
                Debug.LogError("Google Sign-in: No Guest user is currently logged in. Aborting.");
                return;
            }

            Debug.Log($"Linking Guest user {guestUser.UserId} to Google account...");
            AuthResult result = await guestUser.LinkWithCredentialAsync(credential);

            FirebaseUser linkedUser = result.User; // This is now the permanent, linked user
            Debug.Log($"Google SSO Link Complete. User: {linkedUser.DisplayName} ({linkedUser.UserId})");

            // --- STEP 5: Show the profile setup modal ---
            // We successfully linked Google, now we need their username/password
            ShowProfileSetupModal(true);
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Google SSO Link failed: {e.Message}");
        }
    } */
    // --- Helper Methods (Copied from LoginScreen.cs) ---

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
        const string TokenEndpoint = "https://oauth2.googleapis.com/token";

        WWWForm form = new WWWForm();
        form.AddField("code", code);
        form.AddField("client_id", WebClientId);
        form.AddField("client_secret", ClientSecret);
        form.AddField("redirect_uri", OAuthFlow.RedirectUri);
        form.AddField("grant_type", "authorization_code");

        using (UnityWebRequest www = UnityWebRequest.Post(TokenEndpoint, form))
        {
            var operation = www.SendWebRequest();
            while (!operation.isDone)
            {
                await Task.Yield();
            }

            if (www.result != UnityWebRequest.Result.Success)
            {
                Debug.LogError($"Token Exchange Error: {www.error}");
                Debug.LogError($"Response: {www.downloadHandler.text}");
                return null;
            }

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

    /// <summary>
    /// REFACTORED: Toggles password visibility for any given password field and eye icon.
    /// </summary>
    private void OnTogglePasswordVisibility(TextField field, Label eyeIcon)
    {
        if (field != null)
        {
            field.isPasswordField = !field.isPasswordField;
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
}
