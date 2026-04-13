using System.Collections.Generic;
using UnityEngine;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class AchievementManager : MonoBehaviour
{
    public static AchievementManager Instance;

    // ─────────────────────────────────────────────
    // CONSTANTS
    // ─────────────────────────────────────────────

    /// <summary>Top-level Firestore collection that stores per-user achievement data.</summary>
    private const string COLLECTION = "user_achievements";

    // ─────────────────────────────────────────────
    // STATE
    // ─────────────────────────────────────────────

    private readonly Dictionary<string, int> progress = new Dictionary<string, int>();
    private readonly HashSet<string> unlocked = new HashSet<string>();

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    // ─────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────

    void Awake()
    {
        Instance = this;

        auth = FirebaseAuth.DefaultInstance;
        db = FirebaseFirestore.DefaultInstance;

        // Listen for sign-in so achievements re-sync even if the user logs in after Awake.
        auth.StateChanged += OnAuthStateChanged;

        LoadAchievements();
    }

    void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthStateChanged;
    }

    /// <summary>Re-syncs achievements whenever the signed-in user changes (login / logout).</summary>
    private void OnAuthStateChanged(object sender, System.EventArgs e)
    {
        LoadAchievements();
    }

    // ─────────────────────────────────────────────
    // LOAD  (Firebase-first, local fallback / import)
    // ─────────────────────────────────────────────

    /// <summary>
    /// Priority order:
    ///   1. Fetch from Firestore (source of truth).
    ///   2. Collect any locally-saved (PlayerPrefs) IDs not yet in Firestore.
    ///   3. Merge and apply — local extras are pushed back up to Firestore.
    ///   4. If no user is signed in, fall back to local-only.
    /// </summary>
    private void LoadAchievements()
    {
        // Always start clean so a logout/login cycle doesn't carry stale state.
        unlocked.Clear();

        // ── Collect local achievements ────────────────────────────────────────
        HashSet<string> localUnlocked = new HashSet<string>();
        foreach (var a in AchievementDatabase.GetAll())
        {
            if (PlayerPrefs.GetInt(a.id, 0) == 1)
                localUnlocked.Add(a.id);
        }

        // ── No signed-in user → use local only ───────────────────────────────
        if (auth.CurrentUser == null)
        {
            Debug.Log(">>> ACHIEVEMENT MANAGER: No user signed in — loading local achievements only.");
            ApplyUnlockedSet(localUnlocked);
            return;
        }

        string userId = auth.CurrentUser.UserId;
        Debug.Log($">>> ACHIEVEMENT MANAGER: Fetching achievements for user {userId}");

        // ── Fetch from Firestore ──────────────────────────────────────────────
        db.Collection(COLLECTION)
          .Document(userId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              // Guard: user may have changed while the request was in-flight.
              if (auth.CurrentUser == null || auth.CurrentUser.UserId != userId)
              {
                  Debug.LogWarning(">>> ACHIEVEMENT MANAGER: Discarding stale fetch — user changed.");
                  return;
              }

              HashSet<string> firebaseUnlocked = new HashSet<string>();

              if (task.IsFaulted)
              {
                  Debug.LogError(">>> ACHIEVEMENT MANAGER: Firestore fetch failed — falling back to local data.");
                  ApplyUnlockedSet(localUnlocked);
                  return;
              }

              DocumentSnapshot snapshot = task.Result;

              // ── Parse existing Firestore document ─────────────────────────
              if (snapshot.Exists && snapshot.ContainsField("unlocked"))
              {
                  var map = snapshot.GetValue<Dictionary<string, object>>("unlocked");
                  if (map != null)
                      foreach (var kv in map)
                          if (System.Convert.ToBoolean(kv.Value))
                              firebaseUnlocked.Add(kv.Key);
              }

              // ── Import local-only achievements into the Firebase set ───────
              bool hasNewLocalData = false;
              foreach (string id in localUnlocked)
              {
                  if (!firebaseUnlocked.Contains(id))
                  {
                      firebaseUnlocked.Add(id);
                      hasNewLocalData = true;
                      Debug.Log($">>> ACHIEVEMENT MANAGER: Importing local achievement '{id}' → Firebase.");
                  }
              }

              // ── Apply the merged result locally ───────────────────────────
              ApplyUnlockedSet(firebaseUnlocked);

              // ── Push merged set back to Firestore if local had extras ──────
              if (hasNewLocalData || !snapshot.Exists)
                  SaveAllToFirebase(userId);
          });
    }

    /// <summary>
    /// Marks a set of achievement IDs as unlocked in memory, updates their
    /// data flags, and refreshes PlayerPrefs.
    /// </summary>
    private void ApplyUnlockedSet(HashSet<string> ids)
    {
        foreach (string id in ids)
        {
            unlocked.Add(id);
            PlayerPrefs.SetInt(id, 1);

            Achievement a = AchievementDatabase.GetAll().Find(x => x.id == id);
            if (a != null) a.isUnlocked = true;
        }

        PlayerPrefs.Save();
        Debug.Log($">>> ACHIEVEMENT MANAGER: Applied {ids.Count} unlocked achievement(s).");
    }

    // ─────────────────────────────────────────────
    // PROGRESS & UNLOCK
    // ─────────────────────────────────────────────

    /// <summary>Increments progress toward an achievement and checks for unlock.</summary>
    public void AddProgress(string id, int amount)
    {
        if (!progress.ContainsKey(id))
            progress[id] = 0;

        progress[id] += amount;
        CheckUnlock(id);
    }

    private void CheckUnlock(string id)
    {
        if (unlocked.Contains(id)) return;

        Achievement a = AchievementDatabase.GetAll().Find(x => x.id == id);
        if (a == null) return;
        if (!progress.ContainsKey(id)) return;
        if (progress[id] < a.targetValue) return;

        // ── Unlock! ───────────────────────────────────────────────────────────
        unlocked.Add(id);
        a.isUnlocked = true;

        // Save locally first (instant, offline-safe).
        PlayerPrefs.SetInt(id, 1);
        PlayerPrefs.Save();

        // Then persist to Firestore.
        SaveUnlockToFirebase(id);

        Debug.Log($">>> ACHIEVEMENT MANAGER: Unlocked '{a.title}'");
        AchievementUI.Instance.ShowUnlocked(a);
    }

    // ─────────────────────────────────────────────
    // FIREBASE WRITE HELPERS
    // ─────────────────────────────────────────────

    /// <summary>
    /// Merges a single newly-unlocked achievement into the user's Firestore document.
    /// Uses SetOptions.MergeAll so it never overwrites other fields.
    /// </summary>
    private void SaveUnlockToFirebase(string id)
    {
        if (auth.CurrentUser == null)
        {
            Debug.LogWarning(">>> ACHIEVEMENT MANAGER: Skipping Firebase save — no user signed in.");
            return;
        }

        string userId = auth.CurrentUser.UserId;

        var data = new Dictionary<string, object>
        {
            { "unlocked", new Dictionary<string, object> { { id, true } } }
        };

        db.Collection(COLLECTION)
          .Document(userId)
          .SetAsync(data, SetOptions.MergeAll)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
                  Debug.LogError($">>> ACHIEVEMENT MANAGER: Failed to save unlock '{id}' to Firebase.");
              else
                  Debug.Log($">>> ACHIEVEMENT MANAGER: Saved unlock '{id}' to Firebase.");
          });
    }

    /// <summary>
    /// Writes the full unlocked map to Firestore (merge).
    /// Used when importing local-only data on first sync or when the document is new.
    /// </summary>
    private void SaveAllToFirebase(string userId)
    {
        var unlockedMap = new Dictionary<string, object>();
        foreach (string id in unlocked)
            unlockedMap[id] = true;

        var data = new Dictionary<string, object> { { "unlocked", unlockedMap } };

        db.Collection(COLLECTION)
          .Document(userId)
          .SetAsync(data, SetOptions.MergeAll)
          .ContinueWithOnMainThread(task =>
          {
              if (task.IsFaulted)
                  Debug.LogError(">>> ACHIEVEMENT MANAGER: Failed to sync local achievements to Firebase.");
              else
                  Debug.Log($">>> ACHIEVEMENT MANAGER: Synced {unlocked.Count} achievement(s) to Firebase.");
          });
    }

    // ─────────────────────────────────────────────
    // PUBLIC QUERY API
    // ─────────────────────────────────────────────

    public bool IsUnlocked(string id) => unlocked.Contains(id);
}