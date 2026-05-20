using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections; // Nécessaire pour les Coroutines

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

    [Header("UI")]
    public TMP_Text ammoTxt;
    public Image hitmarkerImage;
    public Color hitColor = Color.red; // Couleur quand on touche
    public float hitmarkerDuration = 0.5f;

    private float nextTimeToFire;
    private bool isReloading;
    private bool isFiring;
    private Coroutine hitmarkerCoroutine; // Pour éviter les conflits de couleurs

    private PlayerInputActions input;

    [Header("Kill")]
    public AudioClip killSound;

    [Header("UI Audio")]
public AudioSource uiAudioSource;

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
        UpdateAmmoUI();
        
        // Initialiser le hitmarker en blanc au départ
        if (hitmarkerImage != null)
            hitmarkerImage.color = Color.white;
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
        photonView.RPC(nameof(RPC_ShootSound), RpcTarget.All);

        Ray ray = new Ray(fpsCamera.transform.position, fpsCamera.transform.forward);

        if (Physics.Raycast(ray, out RaycastHit hit, range))
        {
            // --- CIBLE : ZOMBIE ---
            if (hit.transform.TryGetComponent<ZombieHealth>(out var zombie))
            {
                zombie.TakeDamage(damage);
                
                // Effet visuel local
                ShowHitmarker();
                return;
            }

            // --- CIBLE : JOUEUR ---
            PhotonView targetView = hit.transform.GetComponent<PhotonView>();
            if (targetView != null)
            {
                // On envoie : l'ID de la victime, les dégâts, et l'ID de l'attaquant (nous)
                photonView.RPC(nameof(RPC_DealDamagePlayer), RpcTarget.All, targetView.Owner.ActorNumber, damage, photonView.Owner.ActorNumber);
    
                // Effet visuel local
                ShowHitmarker();
            }
        }
    }

    // --- LOGIQUE DU HITMARKER ---
    void ShowHitmarker()
    {
        if (hitmarkerImage == null) return;

        // Si une coroutine tourne déjà (on tire vite), on l'arrête pour recommencer le chrono
        if (hitmarkerCoroutine != null)
            StopCoroutine(hitmarkerCoroutine);

        hitmarkerCoroutine = StartCoroutine(HitmarkerFeedback());
    }

    IEnumerator HitmarkerFeedback()
    {
        hitmarkerImage.color = hitColor; // Passe au rouge
        yield return new WaitForSeconds(hitmarkerDuration); // Attend 0.5s
        hitmarkerImage.color = Color.white; // Revient au blanc
    }

    [PunRPC] 
    void RPC_DealDamagePlayer(int victimActorNumber, float amount, int attackerActorNumber)
    {
        // 1. On cherche tous les scripts de vie dans la scène
        PlayerHealth[] players = FindObjectsOfType<PlayerHealth>();
        
        foreach (var p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            
            // 2. On cherche le joueur qui a le VICTIM Actor Number (celui qui s'est pris la balle)
            if (pv != null && pv.Owner.ActorNumber == victimActorNumber)
            {
                // 3. Seule la victime applique les dégâts localement sur sa machine
                if (pv.IsMine)
                {
                    // Elle s'applique les dégâts en enregistrant bien l'ID de l'attaquant pour le score
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

    void UpdateAmmoUI()
    {
        if (ammoTxt)
            ammoTxt.text = infiniteAmmo ? "∞ / ∞" : currentAmmo + " / " + totalAmmo;
    }

public void PlayKillSound()
{
    Debug.Log("Tentative de jouer le son de kill...");
    if (killSound && uiAudioSource)
    {
        Debug.Log("Son de kill joué !");
        uiAudioSource.PlayOneShot(killSound);
    }
}
}