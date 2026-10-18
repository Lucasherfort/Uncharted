using UnityEngine;
using Photon.Pun;
using System.Collections;

public class PlayerShooter : MonoBehaviourPun
{
    [Header("References")]
    public Camera fpsCamera;
    public AudioSource weaponAudioSource;

    [Header("Shooting")]
    public float range = 100f;
    public float damage = 5f;
    public float fireRate = 10f;

    [Header("Ammo Settings")]
    public bool infiniteAmmo = false;
    public int maxAmmo = 30;
    public int currentAmmo;
    public int totalAmmo = 90;
    public float reloadTime = 1.5f;

    [Header("Audio")]
    public AudioClip shootSound;
    public AudioClip hitSound;
    public AudioClip reloadSound;
    public AudioClip emptySound;
    public AudioClip killSound;

    [Header("UI Feedback Settings")]
    public Color hitColor = Color.red; // Couleur du hitmarker lors d'un coup réussi
    public float hitmarkerDuration = 0.5f;

    [Header("UI Audio")]
    public AudioSource uiAudioSource;

    private float nextTimeToFire;
    private bool isReloading;
    private bool isFiring;

    private PlayerInputActions input;

    void Awake()
    {
        input = new PlayerInputActions();
    }

    void OnEnable()
    {
        input.Player.Enable();
        input.Player.Fire.performed += _ => isFiring = true;
        input.Player.Fire.canceled += _ => isFiring = false;
    }

    void OnDisable()
    {
        input.Player.Disable();
    }

    void Start()
    {
        currentAmmo = maxAmmo;

        if (photonView.IsMine)
        {
            UpdateAmmoUI();
        }
    }

    void Update()
    {
        if (!photonView.IsMine)
            return;

        if (isFiring)
            TryShoot();
    }

    void TryShoot()
    {
        if (isReloading) return;
        if (Time.time < nextTimeToFire) return;

        if (!infiniteAmmo)
        {
            if (currentAmmo <= 0)
            {
                photonView.RPC(nameof(RPC_Empty), RpcTarget.All);
                return;
            }
            currentAmmo--;
        }

        nextTimeToFire = Time.time + 1f / fireRate;
        Shoot();
        UpdateAmmoUI();
    }

    void Shoot()
    {
        // Le son du tir est envoyé à tout le monde
        photonView.RPC(nameof(RPC_ShootSound), RpcTarget.All);

        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {

            // ==========================================
            // --- MODE DEATHMATCH (JOUEURS) ---
            // ==========================================

            PhotonView targetView = hit.transform.GetComponent<PhotonView>();
                
            if (targetView != null && targetView.Owner != null && !targetView.IsMine)
            {
                Debug.Log($"[SHOOT] Hit player: {targetView.Owner.NickName}");

                targetView.RPC(
                    "RPC_TakeDamage", 
                    targetView.Owner, 
                    damage, 
                    PhotonNetwork.LocalPlayer.ActorNumber
                );

                // Effet visuel et sonore local (Hitmarker via UI centrale)
                ShowHitmarker();
            }
        }
    }

    // --- LOGIQUE DU HITMARKER DÉLÉGUÉE À L'UI ---
    void ShowHitmarker()
    {
        if (!photonView.IsMine) return;

        // Appel de la méthode d'affichage du Hitmarker dans le HUD local
        if (MatchHUDManager.Instance != null && MatchHUDManager.Instance.LocalPlayerHUD != null)
        {
            MatchHUDManager.Instance.LocalPlayerHUD.ShowHitmarker(hitColor, hitmarkerDuration);
        }

        PlayHitSound();
    }

    [PunRPC] 
    void RPC_DealDamagePlayer(int victimActorNumber, float amount, int attackerActorNumber)
    {
        PlayerHealth[] players = FindObjectsOfType<PlayerHealth>();
        
        foreach (var p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            
            if (pv != null && pv.Owner.ActorNumber == victimActorNumber)
            {
                if (pv.IsMine)
                {
                    p.ApplyDamageLocal(amount, attackerActorNumber);
                }
            }
        }
    }

    [PunRPC] void RPC_ShootSound() => PlayWeapon(shootSound);
    [PunRPC] void RPC_HitSound() => PlayKillSound();
    [PunRPC] void RPC_Empty() => PlayWeapon(emptySound);

    void PlayWeapon(AudioClip clip)
    {
        if (clip && weaponAudioSource)
            weaponAudioSource.PlayOneShot(clip);
    }

    void PlayHitSound()
    {
        if (hitSound && uiAudioSource)
            uiAudioSource.PlayOneShot(hitSound);
    }

    public void PlayKillSound()
    {
        if (killSound && uiAudioSource)
        {
            uiAudioSource.PlayOneShot(killSound);
        }
    }

    private void UpdateAmmoUI()
    {
        if (!photonView.IsMine) return;

        if (MatchHUDManager.Instance != null && MatchHUDManager.Instance.LocalPlayerHUD != null)
        {
            MatchHUDManager.Instance.LocalPlayerHUD.UpdateAmmoUI(infiniteAmmo, currentAmmo, totalAmmo);
        }
    }
}