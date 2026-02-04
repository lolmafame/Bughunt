using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections;

public class ResentButtonTimer : MonoBehaviour
{
    [Header("UI")]
    public Button resendButton;
    public TextMeshProUGUI buttonText; 
    public string defaultButtonText = "Resend OTP";

    [Header("Cooldown Settings")]
    public float baseCooldown = 30f;
    public float cooldownIncrease = 30f;
    public float maxCooldown = 120f;

    [Header("Attempt Settings")]
    public int maxAttempts = 5;

    private int currentAttempts = 0;
    private float currentCooldown;
    private bool isCooldownRunning = false;
    private Coroutine cooldownCoroutine;

    void Start()
    {
        if (resendButton == null || buttonText == null)
        {
            Debug.LogError("Assign both Resend Button and Button TMP_Text in the inspector!");
            return;
        }

        currentCooldown = baseCooldown;

        // Start the initial cooldown immediately
        StartCooldown();
    }

    public void OnResendClicked()
    {
        if (isCooldownRunning) return;

        if (currentAttempts >= maxAttempts)
        {
            Debug.Log("Max OTP attempts reached.");
            return;
        }

        Debug.Log("OTP Resent");

        currentAttempts++;

        // Increase cooldown for this next run
        currentCooldown = Mathf.Min(currentCooldown + cooldownIncrease, maxCooldown);

        StartCooldown();
    }

    private void StartCooldown()
    {
        // Stop any running cooldown
        if (cooldownCoroutine != null)
            StopCoroutine(cooldownCoroutine);

        cooldownCoroutine = StartCoroutine(CooldownRoutine());
    }

    private IEnumerator CooldownRoutine()
    {
        isCooldownRunning = true;
        resendButton.interactable = false;

        float timer = currentCooldown;

        while (timer > 0)
        {
            buttonText.text = $"Resend in {Mathf.Ceil(timer)}s"; 
            yield return new WaitForSeconds(1f);
            timer -= 1f;
        }

        buttonText.text = defaultButtonText;
        resendButton.interactable = true;
        isCooldownRunning = false;
    }

    public void ResetAttempts()
    {
        currentAttempts = 0;
        currentCooldown = baseCooldown;
        buttonText.text = defaultButtonText;
        resendButton.interactable = true;

        if (cooldownCoroutine != null)
        {
            StopCoroutine(cooldownCoroutine);
            cooldownCoroutine = null;
        }

        isCooldownRunning = false;
    }
}
