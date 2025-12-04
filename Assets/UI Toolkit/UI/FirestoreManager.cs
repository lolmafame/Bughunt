using Firebase.Auth;
using Firebase.Firestore;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;

/// <summary>
/// A non-MonoBehaviour class to handle all Firestore database operations.
/// </summary>
public class FirestoreManager
{
    private readonly FirebaseFirestore db;

    public FirestoreManager()
    {
        db = FirebaseFirestore.DefaultInstance;
        if (db == null)
        {
            Debug.LogError("Firebase Firestore instance is null. Ensure Firebase is initialized.");
        }
    }

    /// <summary>
    /// Checks if a custom username is already taken in the 'usernames' collection.
    /// </summary>
    /// <param name="username">The username to check.</param>
    /// <returns>True if the username document exists, false otherwise.</returns>
    public async Task<bool> IsUsernameTaken(string username)
    {
        if (db == null)
        {
            Debug.LogError("Firestore database is not initialized.");
            return true; // Fail safe
        }

        try
        {
            DocumentReference docRef = db.Collection("usernames").Document(username.ToLower());
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            return snapshot.Exists;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking username: {e.Message}");
            return true; // Fail safe, assume it's taken if an error occurs
        }
    }

    /// <summary>
    /// Saves the user's profile data to two collections:
    /// 1. 'usernames/{username}' -> { "uid": "user.UserId" } (for quick lookup)
    /// 2. 'users/{userId}' -> { "username": "username", "email": "email", ... } (for main profile data)
    /// </summary>
    /// <param name="user">The FirebaseUser object.</param>
    /// <param name="username">The new custom username.</param>
    /// <param name="email">The user's email.</param>
    public async Task SaveUserProfile(FirebaseUser user, string username, string email)
    {
        if (db == null)
        {
            Debug.LogError("Firestore database is not initialized.");
            return;
        }

        // 1. Create the username mapping document
        DocumentReference usernameRef = db.Collection("usernames").Document(username.ToLower());
        var usernameData = new Dictionary<string, object>
        {
            { "uid", user.UserId },
            { "email", email }
        };

        // 2. Create the main user profile document
        DocumentReference userRef = db.Collection("users").Document(user.UserId);
        var profileData = new Dictionary<string, object>
        {
            { "username", username },
            { "email", email },
            { "createdAt", FieldValue.ServerTimestamp }
            // Add any other default profile data here (e.g., level: 1, score: 0)
        };

        try
        {
            // Write both documents in parallel
            Task task1 = usernameRef.SetAsync(usernameData);
            Task task2 = userRef.SetAsync(profileData, SetOptions.MergeAll); // MergeAll to avoid overwriting existing data if any

            await Task.WhenAll(task1, task2);

            Debug.Log($"Successfully saved profile data for user {user.UserId} with username {username}");
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error saving user profile to Firestore: {e.Message}");
            // Handle error (e.g., you might want to try to roll back the username creation)
        }
    }

    /// <summary>
    /// NEW METHOD: Finds a user's email by querying their username.
    /// </summary>
    /// <param name="username">The username to look up.</param>
    /// <returns>The user's email string if found, otherwise null.</returns>
    public async Task<string> GetEmailFromUsername(string username)
    {
        if (db == null)
        {
            Debug.LogError("Firestore database is not initialized.");
            return null;
        }

        try
        {
            // 1. Check the 'usernames' collection
            DocumentReference usernameRef = db.Collection("usernames").Document(username.ToLower());
            DocumentSnapshot snapshot = await usernameRef.GetSnapshotAsync();

            if (!snapshot.Exists)
            {
                Debug.LogWarning($"Username '{username}' not found in 'usernames' collection.");
                return null;
            }

            // 2. Get the EMAIL directly from this document
            if (snapshot.TryGetValue("email", out string email))
            {
                return email;
            }
            else
            {
                Debug.LogError($"Username document '{username}' does not contain an 'email' field. (Run Step 3 Fix)");
                return null;
            }
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error getting email from username: {e.Message}");
            return null;
        }
    }

    /// Checks if a user profile document exists in the 'users' collection.
    public async Task<bool> DoesUserProfileExist(string uid)
    {
        if (db == null)
        {
            Debug.LogError("Firestore database is not initialized.");
            return false; // Fail safe
        }
        try
        {
            DocumentReference docRef = db.Collection("users").Document(uid);
            DocumentSnapshot snapshot = await docRef.GetSnapshotAsync();
            return snapshot.Exists;
        }
        catch (System.Exception e)
        {
            Debug.LogError($"Error checking user profile existence: {e.Message}");
            return false; // Fail safe, assume it doesn't exist
        }
    }
} 

