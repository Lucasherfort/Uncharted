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

        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;
            photonView.RPC(nameof(RPC_Die), RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_Die()
    {
        DisablePlayerVisual();

        if (photonView.IsMine)
        {
            StartCoroutine(Respawn());
        }
    }

    void DisablePlayerVisual()
    {
        // input + gameplay
        GetComponent<PlayerShooter>().enabled = false;
        GetComponent<PlayerController>().enabled = false;

        // mesh + armes
        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = false;

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = false;

        // UI
        if (healthText != null)
            healthText.gameObject.SetActive(false);

        // caméra locale uniquement
        if (photonView.IsMine)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam) cam.enabled = false;
        }
    }

    System.Collections.IEnumerator Respawn()
    {
        yield return new WaitForSeconds(respawnDelay);

        photonView.RPC(nameof(RPC_Respawn), RpcTarget.All);
    }

    [PunRPC]
    void RPC_Respawn()
    {
        Launcher launcher = FindObjectOfType<Launcher>();

        int index = (photonView.Owner.ActorNumber - 1) % launcher.spawnPoints.Length;
        Transform spawn = launcher.spawnPoints[index];

        transform.position = spawn.position;
        transform.rotation = spawn.rotation;

        // reset state
        currentHealth = maxHealth;
        isDead = false;

        // réactivation globale
        GetComponent<PlayerShooter>().enabled = true;
        GetComponent<PlayerController>().enabled = true;

        foreach (Renderer r in GetComponentsInChildren<Renderer>())
            r.enabled = true;

        foreach (Collider c in GetComponentsInChildren<Collider>())
            c.enabled = true;

        if (healthText != null)
            healthText.gameObject.SetActive(true);

        if (photonView.IsMine)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam) cam.enabled = true;
        }

        UpdateUI();
    }

    void UpdateUI()
    {
        if (healthText == null) return;
        healthText.text = Mathf.CeilToInt(currentHealth) + " / " + maxHealth;
    }
}