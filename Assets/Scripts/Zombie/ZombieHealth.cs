using UnityEngine;
using System;
using System.Collections;
using UnityEngine.AI;
using Photon.Pun;

[RequireComponent(typeof(Animator))]
[RequireComponent(typeof(PhotonView))]
public class ZombieHealth : MonoBehaviourPun
{
    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Audio")]
    public AudioSource audioSource;
    public AudioClip deathSound;

    private Animator animator;
    private bool isDead = false;

    public Action onDeath;

    void Start()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    // 🔥 Appelé par n'importe quel client
    public void TakeDamage(float amount)
    {
        if (isDead)
            return;

        // 📡 On envoie la demande au MasterClient
        photonView.RPC(nameof(RPC_TakeDamage), RpcTarget.MasterClient, amount);
    }

    // 🔥 Exécuté uniquement sur le MasterClient
    [PunRPC]
    void RPC_TakeDamage(float amount)
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (isDead)
            return;

        currentHealth -= amount;

        if (currentHealth <= 0f)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead)
            return;

        isDead = true;

        // 📡 On synchronise la mort sur tous les clients
        photonView.RPC(nameof(RPC_Die), RpcTarget.All);
    }

    [PunRPC]
    void RPC_Die()
    {
        // ⚠️ éviter double exécution
        if (isDead && animator.GetBool("Dead")) return;

        isDead = true;

        // 🎬 animation
        if (animator != null)
        {
            animator.SetTrigger("Die");
            animator.SetBool("Dead", true);
        }

        // 🧠 stop IA
        if (TryGetComponent<NavMeshAgent>(out var agent))
            agent.enabled = false;

        if (TryGetComponent<EnemyController>(out var ai))
            ai.enabled = false;

        // 🚫 collider off
        var col = GetComponent<Collider>();
        if (col != null)
            col.enabled = false;

        // 🔊 son
        PlayDeathSound();

        // 📢 seulement le Master gère les vagues
        if (PhotonNetwork.IsMasterClient)
        {
            onDeath?.Invoke();
        }

        // ⏳ destruction réseau
        StartCoroutine(DestroyAfterDelay());
    }

    IEnumerator DestroyAfterDelay()
    {
        yield return new WaitForSeconds(2f);

        // 🔥 IMPORTANT : seul le Master détruit
        if (PhotonNetwork.IsMasterClient && photonView != null)
        {
            PhotonNetwork.Destroy(gameObject);
        }
    }

    void PlayDeathSound()
    {
        if (audioSource != null && deathSound != null)
        {
            audioSource.pitch = UnityEngine.Random.Range(0.95f, 1.1f);
            audioSource.PlayOneShot(deathSound);
        }
    }
}