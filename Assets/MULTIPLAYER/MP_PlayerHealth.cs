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
            if (obj != null) healthBar = obj.GetComponent<Image>();
        }

        if (redFlash == null)
        {
            GameObject obj = GameObject.Find("hurt");
            if (obj != null) redFlash = obj.GetComponent<Image>();
        }

        if (greyFlash == null)
        {
            GameObject obj = GameObject.Find("InvincibleFlash");
            if (obj != null) greyFlash = obj.GetComponent<Image>();
        }

        UpdateHealthUI();

        if (redFlash != null) redFlash.enabled = false;
        if (greyFlash != null) greyFlash.enabled = false;

        playerMovement = GetComponent<MP_ThirdPersonMovement>();
    }

    public void TakeDamage(int damage)
    {
        if (!IsServer) return;
        TakeDamageClientRpc(damage);
    }

    [ClientRpc]
    void TakeDamageClientRpc(int damage)
    {
        if (!IsOwner) return;
        if (isInvincible) return;

        currentHealth -= damage;

        if (currentHealth <= 0)
        {
            currentHealth = 0;
            UpdateHealthUI();
            DieServerRpc();
            return;
        }

        if (SoundManager.Instance != null) SoundManager.Instance.PlayHurt();
        UpdateHealthUI();

        if (playerMovement != null)
            playerMovement.ResetStamina();

        if (redFlash != null)
            StartCoroutine(RedFlashCoroutine());

        StartCoroutine(InvincibilityCoroutine());
    }

    [ServerRpc(RequireOwnership = false)]
    void DieServerRpc()
    {
        DieClientRpc();
    }

    [ClientRpc]
    void DieClientRpc()
    {
        if (!IsOwner) return;

        HidePlayer();

        MP_GameManager.Instance.ShowYouDied(3f, () =>
        {
            StartCoroutine(RespawnCoroutine());
        });
    }

    void HidePlayer()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.enabled = false;

        if (playerMovement != null)
            playerMovement.enabled = false;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = false;
    }

    void ShowPlayer()
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (Renderer r in renderers)
            r.enabled = true;

        if (playerMovement != null)
            playerMovement.enabled = true;

        CharacterController cc = GetComponent<CharacterController>();
        if (cc != null) cc.enabled = true;
    }

    IEnumerator RespawnCoroutine()
    {
        if (SpawnManager.Instance != null)
        {
            Vector3 spawnPos = SpawnManager.Instance.GetRandomSpawnPoint();
            transform.position = spawnPos;
        }

        currentHealth = maxHealth;
        UpdateHealthUI();
        isInvincible = false;

        ShowPlayer();

        MP_GameManager.Instance.HideYouDied();

        StartCoroutine(InvincibilityCoroutine());
        yield return null; // ADD THIS
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

    public void ResetHealthUI()
    {
        UpdateHealthUI();
    }
}