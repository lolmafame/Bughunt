using UnityEngine;
using Firebase.Auth; // We need this to hold the user object

/// <summary>
/// A persistent singleton that carries user data between scenes.
/// It holds the logged-in Firebase user and their profile data.
/// </summary>
public class GameDataManager : MonoBehaviour
{
    // The static instance that all other scripts will access.
    public static GameDataManager Instance { get; private set; }

    // Public properties to hold the user's data
    public FirebaseUser CurrentUser { get; private set; }
    public string Username { get; private set; }
    public string Email { get; private set; }
    private FirebaseAuth auth;

    private void Awake()
    {
        // This is the singleton pattern.
        // If an instance already exists, destroy this new one.
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        // This is the first or only instance.
        Instance = this;

        // Tell Unity not to destroy this object when loading a new scene.
        DontDestroyOnLoad(gameObject);

        // 1. Get the auth instance
        auth = FirebaseAuth.DefaultInstance;

        // 2. Register the listener
        auth.StateChanged += OnAuthStateChanged;
        // -----------------------
    }


    // 3. This is the new method that gets called by the listener
    private void OnAuthStateChanged(object sender, System.EventArgs e)
    {
        FirebaseUser user = auth.CurrentUser;

        // If the user becomes null (logged out OR deleted),
        // we must clear our local data.
        if (user == null && CurrentUser != null) // Check if we *thought* we had a user
        {
            Debug.LogWarning("Auth State Changed: User became null. (Logged out or Deleted). Clearing local data.");
            ClearData();

            // Optional: Force the user back to the Login screen
            // UnityEngine.SceneManagement.SceneManager.LoadScene("Login");
        }
    }

    // 4. Remember to unsubscribe when this object is destroyed
    private void OnDestroy()
    {
        if (auth != null)
        {
            auth.StateChanged -= OnAuthStateChanged;
        }
    }
    /// <summary>
    /// Stores the user's data. Call this right before loading the MainMenu.
    /// </summary>
    public void SetUserData(FirebaseUser user, string username, string email)
    {
        CurrentUser = user;
        Username = username;
        Email = email;

        Debug.Log($"GameDataManager: Data set. User: {Username}, Email: {Email}, UID: {CurrentUser.UserId}");
    }

    /// <summary>
    /// Clears the user's data. Call this on logout.
    /// </summary>
    public void ClearData()
    {
        CurrentUser = null;
        Username = null;
        Email = null;
        Debug.Log("GameDataManager: Data cleared.");
    }
}