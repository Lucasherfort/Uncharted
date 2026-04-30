using UnityEngine;
using TMPro;
using UnityEngine.UI;
using Photon.Pun;
using System.Collections; // Nécessaire pour les Coroutines

public class PlayerShooter : MonoBehaviourPun
{
    [Header("References")]
    public Camera fpsCamera;
    public AudioSource audioSource;

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
                photonView.RPC(nameof(RPC_HitSound), RpcTarget.All);
                
                // Effet visuel local
                ShowHitmarker();
                return;
            }

            // --- CIBLE : JOUEUR ---
            PhotonView targetView = hit.transform.GetComponent<PhotonView>();
            if (targetView != null)
            {
                photonView.RPC(nameof(RPC_DealDamagePlayer), RpcTarget.All, targetView.Owner.ActorNumber, damage);
                
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

    [PunRPC] void RPC_DealDamagePlayer(int actorNumber, float dmg)
    {
        PlayerHealth[] players = FindObjectsOfType<PlayerHealth>();
        foreach (var p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();
            if (pv != null && pv.Owner.ActorNumber == actorNumber)
            {
                if (pv.IsMine)
                    p.ApplyDamageLocal(dmg);
            }
        }
    }

    [PunRPC] void RPC_ShootSound() => Play(shootSound);
    [PunRPC] void RPC_HitSound() => Play(hitSound);
    [PunRPC] void RPC_Empty() => Play(emptySound);

    void Play(AudioClip clip)
    {
        if (clip && audioSource)
            audioSource.PlayOneShot(clip);
    }

    void UpdateAmmoUI()
    {
        if (ammoTxt)
            ammoTxt.text = infiniteAmmo ? "∞ / ∞" : currentAmmo + " / " + totalAmmo;
    }
}