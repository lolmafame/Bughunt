using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;

public class AccountManager : MonoBehaviour
{
    [Header("UI Display Elements")]
    [SerializeField] private TMP_Text displayUsernameText;
    [SerializeField] private TMP_Text displayEmailText;
    [SerializeField] private GameObject accountPanel;

    [Header("Tier Display")]
    // Assign the FreeTier and PremTier GameObjects from your Account panel here
    [SerializeField] private GameObject freeTierObject;
    [SerializeField] private GameObject premiumTierObject;

    [Header("Scene Transitions")]
    [SerializeField] private GameObject loginPanelRoot;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    // ─────────────────────────────────────────────
    // PUBLIC OWNERSHIP API
    // Populated after every RefreshUserData() call.
    // MayaPaymentController reads these to disable already-owned buttons.
    // ─────────────────────────────────────────────

    /// <summary>True once the Firestore fetch completes and the user owns premium_plan.</summary>
    public bool IsPremium { get; private set; }

    /// <summary>Returns true if the player has purchased the given itemId.</summary>
    public bool OwnsItem(string itemId) => _ownedItems.Contains(itemId);

    /// <summary>Fired on the main thread after every successful Firestore refresh.</summary>
    public event System.Action OnDataRefreshed;

    private readonly System.Collections.Generic.HashSet<string> _ownedItems =
        new System.Collections.Generic.HashSet<string>();

    void Awake()
    {
        InitializeFirebase();
    }

    private void InitializeFirebase()
    {
        if (auth == null) auth = FirebaseAuth.DefaultInstance;
        if (db == null) db = FirebaseFirestore.DefaultInstance;
    }

    // ─────────────────────────────────────────────
    // OPEN PANEL
    // ─────────────────────────────────────────────

    public void OpenAccountPanel()
    {
        InitializeFirebase();

        Debug.Log(">>> ACCOUNT MANAGER: OpenAccountPanel called!");

        if (auth.CurrentUser == null)
        {
            Debug.LogError(">>> ERROR: No user is logged in.");
            return;
        }

        if (loginPanelRoot != null) loginPanelRoot.SetActive(false);

        if (accountPanel != null)
            accountPanel.SetActive(true);
        else
            Debug.LogError(">>> ERROR: Account Panel not assigned in Inspector!");

        RefreshUserData();
    }

    // ─────────────────────────────────────────────
    // REFRESH  (username + email + tier)
    // ─────────────────────────────────────────────

    public void RefreshUserData()
    {
        InitializeFirebase();

        FirebaseUser user = auth.CurrentUser;
        if (user == null) return;

        Debug.Log($">>> ACCOUNT MANAGER: Loading data for user: {user.UserId}");

        // --- Email (immediate, no Firestore needed) ---
        if (displayEmailText != null)
        {
            displayEmailText.text = string.IsNullOrEmpty(user.Email)
                ? "Guest ID: " + user.UserId.Substring(0, 6)
                : user.Email;
        }

        // Start in a safe "locked-down" tier state while the fetch is in-flight
        ShowTier(isPremium: false);

        // --- Username + Premium status (single Firestore fetch) ---
        string expectedUserId = user.UserId;

        db.Collection("users").Document(expectedUserId)
          .GetSnapshotAsync()
          .ContinueWithOnMainThread(task =>
          {
              // Guard: bail out if the user changed while the request was in-flight
              if (auth.CurrentUser == null || auth.CurrentUser.UserId != expectedUserId)
              {
                  Debug.LogWarning(">>> ACCOUNT MANAGER: Discarding stale fetch — user changed.");
                  return;
              }

              if (task.IsFaulted)
              {
                  Debug.LogError(">>> ACCOUNT MANAGER: Failed to fetch user document.");
                  if (displayUsernameText != null) displayUsernameText.text = "Error";
                  return;
              }

              DocumentSnapshot snapshot = task.Result;

              // ── Username ──────────────────────────────────────
              if (displayUsernameText != null)
              {
                  bool hasUsername = snapshot.Exists && snapshot.ContainsField("username");
                  displayUsernameText.text = hasUsername
                      ? snapshot.GetValue<string>("username")
                      : "Player_" + expectedUserId.Substring(0, 4);

                  Debug.Log(">>> ACCOUNT MANAGER: Username set to: " + displayUsernameText.text);
              }

              // ── Premium tier check ────────────────────────────
              // The server writes: users/{uid}.purchases.{itemId} = true  on payment
              // and users/{uid}.isPremium = true  specifically for premium_plan.
              bool isPremium = false;
              _ownedItems.Clear();

              if (snapshot.Exists)
              {
                  // Fast path: top-level isPremium flag written by the server
                  if (snapshot.ContainsField("isPremium"))
                      isPremium = snapshot.GetValue<bool>("isPremium");

                  // Full purchases map — populate the owned-items set for ALL items
                  if (snapshot.ContainsField("purchases"))
                  {
                      var purchases = snapshot.GetValue<System.Collections.Generic.Dictionary<string, object>>("purchases");
                      if (purchases != null)
                      {
                          foreach (var kv in purchases)
                          {
                              if (System.Convert.ToBoolean(kv.Value))
                                  _ownedItems.Add(kv.Key);
                          }

                          // Fallback: derive isPremium from the purchases map too
                          if (!isPremium && _ownedItems.Contains("premium_plan"))
                              isPremium = true;
                      }
                  }
              }

              IsPremium = isPremium;
              ShowTier(isPremium);
              Debug.Log($">>> ACCOUNT MANAGER: Premium={isPremium} | OwnedItems=[{string.Join(", ", _ownedItems)}]");

              // Notify listeners (e.g. MayaPaymentController) that fresh data is ready
              OnDataRefreshed?.Invoke();
          });
    }

    // ─────────────────────────────────────────────
    // TIER DISPLAY HELPER
    // ─────────────────────────────────────────────

    /// <summary>
    /// Toggles FreeTier vs PremTier GameObjects based on subscription status.
    /// </summary>
    private void ShowTier(bool isPremium)
    {
        if (freeTierObject != null) freeTierObject.SetActive(!isPremium);
        if (premiumTierObject != null) premiumTierObject.SetActive(isPremium);
    }

    // ─────────────────────────────────────────────
    // SIGN OUT
    // ─────────────────────────────────────────────

    public void SignOutFirebaseOnly()
    {
        InitializeFirebase();

        if (auth?.CurrentUser != null)
        {
            auth.SignOut();
            Debug.Log(">>> ACCOUNT MANAGER: Firebase SignOut complete.");
        }

        // Reset tier display and ownership state on logout
        IsPremium = false;
        _ownedItems.Clear();
        ShowTier(isPremium: false);
    }
}