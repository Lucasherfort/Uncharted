using UnityEngine;
using Photon.Pun;
using TMPro;

public class PlayerHealth : MonoBehaviourPun
{
    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("UI")]
    public TMP_Text healthText;

    [Header("Respawn")]
    public float respawnDelay = 3f;

    [Header("Effects")]
    public DamageEffect damageEffect;

    private bool isDead = false;

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

    public void ApplyDamageLocal(float amount)
    {
        if (!photonView.IsMine) return;
        if (isDead) return;

        photonView.RPC(nameof(RPC_TakeDamage), RpcTarget.All, amount);
    }

    [PunRPC]
    void RPC_TakeDamage(float amount)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (photonView.IsMine)
        {
            damageEffect?.OnDamage();
        }

        UpdateUI();

        // Seul celui qui possède le script (Owner) ou le Master peut décider de la mort
        // Mais ici, on laisse chaque client vérifier pour son propre personnage pour plus de réactivité
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            // On informe tout le monde que ce joueur est mort
            photonView.RPC(nameof(RPC_Die), RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_Die()
    {
        SetPlayerState(false);

        if (photonView.IsMine)
        {
            StartCoroutine(RespawnRoutine());
        }
    }

    // Fonction centralisée pour activer/désactiver le joueur (plus propre)
    void SetPlayerState(bool state)
    {
        // Scripts de gameplay
        if (TryGetComponent<PlayerShooter>(out var shooter)) shooter.enabled = state;
        if (TryGetComponent<PlayerController>(out var controller)) controller.enabled = state;

        // Visuels et Physique
        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = state;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = state;

        // UI
        if (healthText != null) healthText.gameObject.SetActive(state);

        // Caméra locale
        if (photonView.IsMine)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam) cam.enabled = state;
        }
    }

    System.Collections.IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);
        
        // On demande le respawn via RPC pour synchroniser la position chez tout le monde
        photonView.RPC(nameof(RPC_Respawn), RpcTarget.All);
    }

    [PunRPC]
    void RPC_Respawn()
    {
        // --- CHANGEMENT ICI ---
        // On utilise le SurvivalManager au lieu du Launcher
        if (SurvivalManager.Instance != null && SurvivalManager.Instance.spawnPoints.Length > 0)
        {
            // On choisit un point aléatoire pour éviter que les joueurs respawn les uns sur les autres
            Transform[] spawns = SurvivalManager.Instance.spawnPoints;
            Transform randomSpawn = spawns[Random.Range(0, spawns.Length)];

            transform.position = randomSpawn.position;
            transform.rotation = randomSpawn.rotation;
        }

        // Reset des variables
        currentHealth = maxHealth;
        isDead = false;

        // Réactivation
        SetPlayerState(true);
        UpdateUI();
        
        Debug.Log($"<color=green>[Health]</color> Respawn de {photonView.Owner.NickName}");
    }

    void UpdateUI()
    {
        if (healthText == null) return;
        healthText.text = Mathf.CeilToInt(currentHealth) + " / " + maxHealth;
    }
}