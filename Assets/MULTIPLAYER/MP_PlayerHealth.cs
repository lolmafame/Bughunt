using UnityEngine;
using UnityEngine.UI;
using System.Collections;
using Unity.Netcode;

public class MP_PlayerHealth : NetworkBehaviour
{
    [Header("Health")]
    public int maxHealth = 100;
    public int currentHealth;
    public Image healthBar;

    [Header("Stamina")]
    public MP_ThirdPersonMovement playerMovement;

    [Header("Damage Effects")]
    public Image redFlash;
    public Image greyFlash;
    public float invincibleTime = 3f;

    private bool isInvincible = false;

    public override void OnNetworkSpawn()
    {
        if (!IsOwner) return;

        currentHealth = maxHealth;

        if (healthBar == null)
        {
            GameObject obj = GameObject.Find("HealthBarFill");
            Debug.Log("HealthBarFill found: " + (obj != null));
            if (obj != null)
                healthBar = obj.GetComponent<Image>();
        }

        if (redFlash == null)
        {
            GameObject obj = GameObject.Find("hurt");
            Debug.Log("hurt found: " + (obj != null));
            if (obj != null)
                redFlash = obj.GetComponent<Image>();
        }

        if (greyFlash == null)
        {
            GameObject obj = GameObject.Find("InvincibleFlash");
            Debug.Log("InvincibleFlash found: " + (obj != null));
            if (obj != null)
                greyFlash = obj.GetComponent<Image>();
        }

        Debug.Log("healthBar assigned: " + (healthBar != null));
        Debug.Log("redFlash assigned: " + (redFlash != null));
        Debug.Log("greyFlash assigned: " + (greyFlash != null));

        UpdateHealthUI();
        if (redFlash != null) redFlash.enabled = false;
        if (greyFlash != null) greyFlash.enabled = false;

        playerMovement = GetComponent<MP_ThirdPersonMovement>();
    }

    public void TakeDamage(int damage)
    {
        if (!IsOwner) return;
        if (isInvincible) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            UpdateHealthUI();
            MP_GameManager.Instance.TriggerGameOverServerRpc();
            return;
        }

        SoundManager.Instance.PlayHurt();
        UpdateHealthUI();

        if (playerMovement != null)
            playerMovement.ResetStamina();

        if (redFlash != null)
            StartCoroutine(RedFlashCoroutine());

        StartCoroutine(InvincibilityCoroutine());
    }

    IEnumerator RedFlashCoroutine()
    {
        if (redFlash == null) yield break;
        redFlash.enabled = true;
        redFlash.color = new Color(1, 0, 0, 0.5f);
        yield return new WaitForSeconds(0.2f);
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
                greyFlash.color = new Color(0.5f, 0.5f, 0.5f,
                    Mathf.PingPong(timer * 2f, 0.5f) + 0.25f);
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