using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System;

// ── One entry per language in the Inspector ───────────────────────────────────
[Serializable]
public class LanguageCertEntry
{
    public LevelLanguage language;

    [Tooltip("The certificate button/panel to show when this language is fully completed.")]
    public GameObject certificateButton;

    [Tooltip("TMP_Text that displays e.g. 'PYTHON - FUNDAMENTALS'. Leave null to skip.")]
    public TMP_Text languageTitleText;
}

public class Cert : MonoBehaviour
{
    [Header("Certificate Info")]
    [SerializeField] private TMP_Text displayUsernameText;
    [SerializeField] private TMP_Text displayDateText;

    [Header("Per-Language Certificate Buttons")]
    [Tooltip("Add one entry per language. Assign the matching certificate button and title text for each.")]
    [SerializeField] private LanguageCertEntry[] languageCerts;

    [Header("Fallback UI")]
    [Tooltip("Shown when NO language has been fully completed yet.")]
    [SerializeField] private GameObject incompleteTextObject;

    private FirebaseAuth auth;
    private FirebaseFirestore db;

    private string lastLoadedUserId = null;

    // ─────────────────────────────────────────────
    // LIFECYCLE
    // ─────────────────────────────────────────────

    void Start()
    {
        InitializeFirebase();
        SetCurrentDate();
    }

    void OnDestroy()
    {
        if (auth != null)
            auth.StateChanged -= OnAuthStateChanged;
    }

    private void InitializeFirebase()
    {
        if (auth == null) auth = FirebaseAuth.DefaultInstance;
        if (db == null) db = FirebaseFirestore.DefaultInstance;

        auth.StateChanged += OnAuthStateChanged;
    }

    // ─────────────────────────────────────────────
    // AUTH STATE
    // ─────────────────────────────────────────────

    private void OnAuthStateChanged(object sender, EventArgs e)
    {
        FirebaseUser currentUser = auth.CurrentUser;

        if (currentUser == null || currentUser.UserId != lastLoadedUserId)
        {
            Debug.Log($">>> CERT: Auth changed. Resetting UI. New user: {currentUser?.UserId ?? "none"}");
            ResetUIToSafeDefault();
        }

        if (currentUser != null)
        {
            lastLoadedUserId = currentUser.UserId;
            SetCurrentDate();
            CheckCertificateStatus(currentUser);
        }
        else
        {
            lastLoadedUserId = null;
        }
    }

    // ─────────────────────────────────────────────
    // UI HELPERS
    // ─────────────────────────────────────────────

    private void ResetUIToSafeDefault()
    {
        if (displayUsernameText != null) displayUsernameText.text = "";
        if (displayDateText != null) displayDateText.text = "";

        if (languageCerts != null)
        {
            foreach (var entry in languageCerts)
            {
                if (entry.certificateButton != null)
                    entry.certificateButton.SetActive(false);

                if (entry.languageTitleText != null)
                    entry.languageTitleText.text = "";
            }
        }

        if (incompleteTextObject != null) incompleteTextObject.SetActive(true);
    }

    private void SetCurrentDate()
    {
        if (displayDateText != null)
            displayDateText.text = DateTime.Now.ToString("MMMM dd, yyyy");
    }

    // ─────────────────────────────────────────────
    // LANGUAGE TITLE HELPER
    // ─────────────────────────────────────────────

    /// <summary>
    /// Returns the all-caps certificate title for a language.
    /// e.g. Python      → "PYTHON - FUNDAMENTALS"
    ///      CSharp      → "C# - FUNDAMENTALS"
    ///      CPlusPlus   → "C++ - FUNDAMENTALS"
    ///      Javascript  → "JAVASCRIPT - FUNDAMENTALS"
    ///      Java        → "JAVA - FUNDAMENTALS"
    /// </summary>
    private string GetLanguageTitle(LevelLanguage language)
    {
        string displayName = language switch
        {
            LevelLanguage.Python => "PYTHON",
            LevelLanguage.Javascript => "JAVASCRIPT",
            LevelLanguage.CSharp => "C#",
            LevelLanguage.Java => "JAVA",
            LevelLanguage.CPlusPlus => "C++",
            _ => language.ToString().ToUpper()
        };

        return $"{displayName} - FUNDAMENTALS";
    }

    // ─────────────────────────────────────────────
    // FIREBASE FETCH
    // ─────────────────────────────────────────────

    private void CheckCertificateStatus(FirebaseUser user)
    {
        string expectedUserId = user.UserId;

        user.ReloadAsync().ContinueWithOnMainThread(reloadTask =>
        {
            if (reloadTask.IsFaulted)
                Debug.LogWarning(">>> CERT: Could not reload user profile. Using cached data.");

            FirebaseUser refreshedUser = auth.CurrentUser;

            if (refreshedUser == null || refreshedUser.UserId != expectedUserId)
            {
                Debug.LogWarning(">>> CERT: User changed during reload. Discarding.");
                return;
            }

            // Set display name from Google profile — no Firestore needed for this
            if (displayUsernameText != null)
            {
                displayUsernameText.text = !string.IsNullOrEmpty(refreshedUser.DisplayName)
                    ? refreshedUser.DisplayName
                    : "Player_" + refreshedUser.UserId.Substring(0, 4);
            }

            // Fetch Firestore for per-language completion status
            db.Collection("users").Document(expectedUserId)
              .GetSnapshotAsync()
              .ContinueWithOnMainThread(task =>
              {
                  if (auth.CurrentUser == null || auth.CurrentUser.UserId != expectedUserId)
                  {
                      Debug.LogWarning(">>> CERT: Discarding stale fetch — user changed mid-flight.");
                      return;
                  }

                  if (task.IsFaulted)
                  {
                      Debug.LogError(">>> CERT: Failed to fetch completion status.");
                      ResetUIToSafeDefault();
                      return;
                  }

                  ApplyCertificateUI(task.Result, expectedUserId);
              });
        });
    }

    // ─────────────────────────────────────────────
    // APPLY UI PER LANGUAGE
    // ─────────────────────────────────────────────

    private void ApplyCertificateUI(DocumentSnapshot snapshot, string userId)
    {
        if (languageCerts == null || languageCerts.Length == 0)
        {
            Debug.LogWarning(">>> CERT: No LanguageCertEntry entries assigned in Inspector.");
            return;
        }

        int completedCount = 0;

        foreach (var entry in languageCerts)
        {
            string langKey = entry.language.ToString().ToLower();  // e.g. "python"
            string firestoreKey = $"{langKey}_all_levels_completed";    // e.g. "python_all_levels_completed"

            bool isComplete = snapshot.Exists
                              && snapshot.ContainsField(firestoreKey)
                              && snapshot.GetValue<bool>(firestoreKey);

            // ── Certificate button ────────────────────────────────────────────
            if (entry.certificateButton != null)
                entry.certificateButton.SetActive(isComplete);

            // ── Language title text ───────────────────────────────────────────
            if (entry.languageTitleText != null)
                entry.languageTitleText.text = isComplete ? GetLanguageTitle(entry.language) : "";

            if (isComplete) completedCount++;

            Debug.Log($">>> CERT: [{langKey}] complete={isComplete} | title='{GetLanguageTitle(entry.language)}'");
        }

        // incompleteTextObject only shows if zero languages are done
        if (incompleteTextObject != null)
            incompleteTextObject.SetActive(completedCount == 0);

        Debug.Log($">>> CERT: {completedCount}/{languageCerts.Length} language(s) completed for [{userId}].");
    }
}