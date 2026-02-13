using UnityEngine;
using TMPro;
using Firebase.Auth;
using Firebase.Firestore;
using Firebase.Extensions;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

public class ForgotPassManager : MonoBehaviour
{
    [Header("Step Objects")]
    [SerializeField] private GameObject forgotStep1;// dsad
    [SerializeField] private GameObject forgotMainObject;
    [SerializeField] private GameObject loginPanelObject;

    [Header("Inputs")]
    [SerializeField] private TMP_InputField emailInput;
    [Header("Dependencies")]
    [SerializeField] private ResentButtonTimer resentButtonTimer;

    private FirebaseFirestore db;
    private FirebaseAuth auth;
    private string currentEmail;

    void Start()
    {
        db = FirebaseFirestore.DefaultInstance;
        auth = FirebaseAuth.DefaultInstance;
        ShowStep(forgotStep1);

        if (resentButtonTimer != null)
        {
            resentButtonTimer.onResendRequested.AddListener(SendResetEmail);
        }
    }

    public void OnForgotClicked()
    {
        ShowStep(forgotStep1);
    }

    public void OnBackToLogin()
    {
        DisableAllSteps();
        if (forgotMainObject != null)
        {
            forgotMainObject.SetActive(false);
        }
        if (loginPanelObject != null)
        {
            loginPanelObject.SetActive(true);
        }
    }

    public void OnEmailSubmit()
    {
        string email = emailInput != null ? emailInput.text.Trim() : string.Empty;
        if (string.IsNullOrEmpty(email))
        {
            Debug.LogError("Forgot password: email is empty.");
            return;
        }

        currentEmail = email;

        Query query = db.Collection("users").WhereEqualTo("email", email).Limit(1);
        query.GetSnapshotAsync().ContinueWithOnMainThread((Task<QuerySnapshot> task) =>
        {
            if (task.IsFaulted || task.IsCanceled)
            {
                Debug.LogError("Forgot password email check failed: " + task.Exception);
                return;
            }

            if (task.Result.Count == 0)
            {
                Debug.LogError("Forgot password: email not found.");
                return;
            }

            SendResetEmail();
        });
    }


    private void SendResetEmail()
    {
        if (string.IsNullOrEmpty(currentEmail))
        {
            Debug.LogError("Forgot password: no email to send reset to.");
            return;
        }

        auth.SendPasswordResetEmailAsync(currentEmail).ContinueWithOnMainThread(resetTask =>
        {
            if (resetTask.IsFaulted || resetTask.IsCanceled)
            {
                Debug.LogError("Forgot password: reset email failed: " + resetTask.Exception);
                return;
            }

            Debug.Log("Forgot password: reset email sent.");
            DisableAllSteps();
            if (forgotMainObject != null)
            {
                forgotMainObject.SetActive(false);
            }
            if (loginPanelObject != null)
            {
                loginPanelObject.SetActive(true);
            }
        });
    }


    private void ShowStep(GameObject step)
    {
        if (forgotStep1 != null) forgotStep1.SetActive(step == forgotStep1);
    }

    private void DisableAllSteps()
    {
        if (forgotStep1 != null) forgotStep1.SetActive(false);
    }
}
