using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class PlayerHealth : MonoBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public Image healthBar;

    [Header("Stamina")]
    public ThirdPersonMovement playerMovement;

    [Header("Damage Effects")]
    public Image redFlash;       // flash when hit
    public Image greyFlash;      // invincible overlay
    public float invincibleTime = 3f;

    private bool isInvincible = false;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateHealthUI();

        if (redFlash != null) redFlash.enabled = false;
        if (greyFlash != null) greyFlash.enabled = false;
    }

    public void TakeDamage(int damage)
    {
        if (isInvincible) return;

        // ===== 1. Apply Damage =====
        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            UpdateHealthUI();
            GameManager.Instance.GameOver();
            return;
        }

        UpdateHealthUI();

        // ===== 2. Refill stamina =====
        if (playerMovement != null)
        {
            playerMovement.ResetStamina();
        }

        // ===== 3. Show red flash =====
        if (redFlash != null)
            StartCoroutine(RedFlashCoroutine());

        // ===== 4. Start invincibility =====
        StartCoroutine(InvincibilityCoroutine());
    }

    IEnumerator RedFlashCoroutine()
    {
        if (redFlash == null) yield break;

        redFlash.enabled = true;
        redFlash.color = new Color(1, 0, 0, 0.5f); // semi-transparent red
        yield return new WaitForSeconds(0.2f);     // short flash
        redFlash.enabled = false;
    }

    IEnumerator InvincibilityCoroutine()
    {
        isInvincible = true;

        if (greyFlash != null)
        {
            greyFlash.enabled = true;
            float timer = 0f;

            while (timer < invincibleTime)
            {
                // make it slightly blink
                greyFlash.color = new Color(0.5f, 0.5f, 0.5f, Mathf.PingPong(timer * 2f, 0.5f) + 0.25f);
                timer += Time.deltaTime;
                yield return null;
            }

            greyFlash.enabled = false;
        }

        isInvincible = false;
    }

    void UpdateHealthUI()
    {
        if (healthBar != null)
            healthBar.fillAmount = (float)currentHealth / maxHealth;
    }
}
