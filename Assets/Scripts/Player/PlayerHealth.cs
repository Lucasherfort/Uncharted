using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using System.Collections;
using TMP_Text = TMPro.TMP_Text;

public class PlayerHealth : MonoBehaviourPun
{
    [Header("Health")]
    public float maxHealth = 100f;
    private float currentHealth;

    [Header("Realistic Regeneration")]
    [Tooltip("Temps en secondes à attendre après un dégât avant de commencer à régénérer.")]
    public float regenDelay = 5f;
    [Tooltip("Quantité de points de vie récupérés à chaque cycle.")]
    public float regenAmount = 2f;
    [Tooltip("Intervalle de temps (en secondes) entre chaque cycle de soin.")]
    public float regenInterval = 1f;

    [Header("UI")]
    public TMP_Text healthText;

    [Header("Respawn")]
    public float respawnDelay = 3f;

    [Header("Effects")]
    public DamageEffect damageEffect;

    private bool isDead = false;
    private Coroutine regenCoroutine; // Permet de suivre et stopper la régénération en cours

    void Start()
    {
        currentHealth = maxHealth;
        UpdateUI();
    }

public void ApplyDamageLocal(float amount, int attackerActorNumber)
{
    // Seule la victime locale gère sa vie
    if (!photonView.IsMine)
        return;

    if (isDead)
        return;

    // =========================
    // ANTI SELF DAMAGE
    // =========================
    if (photonView.Owner.ActorNumber == attackerActorNumber)
        return;

    // IMPORTANT :
    // On appelle directement la fonction
    // PAS de RPC ici
    RPC_TakeDamage(amount, attackerActorNumber);
}
[PunRPC]
    void RPC_TakeDamage(float amount, int attackerActorNumber)
    {
        Debug.Log($"[DAMAGE] Received | Amount={amount} | Attacker={attackerActorNumber} | IsMine={photonView.IsMine}");

        if (isDead)
        {
            Debug.Log("[DAMAGE] Ignored - already dead");
            return;
        }

        // =========================
        // DAMAGE APPLY
        // =========================
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        Debug.Log($"[DAMAGE] Health now: {currentHealth}");

        if (damageEffect != null)
            damageEffect.OnDamage();

        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);

        if (currentHealth > 0)
            regenCoroutine = StartCoroutine(RegenerationRoutine());

        UpdateUI();

        // =========================
        // DEATH CHECK
        // =========================
        if (currentHealth > 0)
            return;

        isDead = true;

        Debug.Log("[DEATH] Player died");

        if (regenCoroutine != null)
            StopCoroutine(regenCoroutine);

        // =========================
        // ATTACKER RESOLUTION
        // =========================
        Player attacker = PhotonNetwork.CurrentRoom.GetPlayer(attackerActorNumber);

        if (attacker == null)
        {
            Debug.LogWarning("[DEATH] Attacker is NULL !");
        }
        else
        {
            Debug.Log($"[DEATH] Attacker found: {attacker.NickName} | Actor={attacker.ActorNumber}");
        }

        string attackerName = attacker != null ? attacker.NickName : "Environment";
        string killedName = photonView.Owner.NickName;

        // =========================
        // KILLFEED
        // =========================
        Debug.Log("[KILLFEED] Sending RPC_AddKillFeed");

        DeathmatchManager.Instance.photonView.RPC(
            nameof(DeathmatchManager.RPC_AddKillFeed),
            RpcTarget.All,
            attackerName,
            killedName
        );

        // =========================
        // SCORE & KILL NOTIFICATION
        // =========================
        if (attacker != null && attacker != photonView.Owner)
        {
            // 1. On envoie le score au Master Client
            Debug.Log("[KILL] Sending kill to Master");
            DeathmatchManager.Instance.photonView.RPC(
                nameof(DeathmatchManager.RPC_RegisterKill),
                RpcTarget.MasterClient,
                attackerActorNumber
            );

            // 2. On envoie l'ordre de jouer le son UNIQUEMENT au tueur via son profil Photon Player
            photonView.RPC(nameof(RPC_SendKillNotification), attacker);
        }

        // =========================
        // DEATH VISUAL
        // =========================
        photonView.RPC(nameof(RPC_Die), RpcTarget.All);
    }

    /// <summary>
    /// Ce RPC est reçu exclusivement par le joueur qui a fait le kill.
    /// </summary>
    [PunRPC]
    void RPC_SendKillNotification()
    {
        Debug.Log("[SOUND] I am the killer! Searching for my local PlayerShooter to play the sound.");

        // On cherche parmi tous les PlayerShooter de la scène celui qui m'appartient (IsMine)
        PlayerShooter[] allShooters = FindObjectsOfType<PlayerShooter>();
        bool soundPlayed = false;

        foreach (PlayerShooter shooter in allShooters)
        {
            if (shooter.photonView != null && shooter.photonView.IsMine)
            {
                Debug.Log("[SOUND] Local PlayerShooter found. Playing kill sound!");
                shooter.PlayKillSound();
                soundPlayed = true;
                break; // On a trouvé notre joueur, on arrête la recherche
            }
        }

        if (!soundPlayed)
        {
            Debug.LogWarning("[SOUND] Could not play kill sound: No local PlayerShooter (IsMine) was found in the scene.");
        }
    }

    // Coroutine locale gérant l'attente et l'effet de soin par tics physiologiques
    private IEnumerator RegenerationRoutine()
    {
        // 1. Période de choc : On attend le délai imposé sans rien faire
        yield return new WaitForSeconds(regenDelay);

        // 2. Boucle de soin : Tant que la vie n'est pas pleine et qu'on est en vie
        while (currentHealth < maxHealth && !isDead)
        {
            currentHealth += regenAmount;
            currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);
            
            UpdateUI();

            // 3. Pause réaliste entre deux pulsations cardiaques / cycles de récupération
            yield return new WaitForSeconds(regenInterval);
        }

        // Nettoyage de la référence une fois le travail fini
        regenCoroutine = null;
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

    void SetPlayerState(bool state)
    {
        if (TryGetComponent<PlayerShooter>(out var shooter)) shooter.enabled = state;
        if (TryGetComponent<PlayerController>(out var controller)) controller.enabled = state;

        foreach (Renderer r in GetComponentsInChildren<Renderer>()) r.enabled = state;
        foreach (Collider c in GetComponentsInChildren<Collider>()) c.enabled = state;

        if (healthText != null) healthText.gameObject.SetActive(state);

        if (photonView.IsMine)
        {
            Camera cam = GetComponentInChildren<Camera>();
            if (cam) cam.enabled = state;
        }
    }

    System.Collections.IEnumerator RespawnRoutine()
    {
        yield return new WaitForSeconds(respawnDelay);

        Vector3 bestSpawnPos = Vector3.zero;
        Quaternion bestSpawnRot = Quaternion.identity;

        if (DeathmatchManager.Instance != null && DeathmatchManager.Instance.spawnPoints.Length > 0)
        {
            Transform[] spawns = DeathmatchManager.Instance.spawnPoints;            
            PlayerHealth[] allPlayers = FindObjectsOfType<PlayerHealth>();

            float maxScorePosition = -1f;
            Transform chosenSpawn = spawns[0]; 

            for (int i = 0; i < spawns.Length; i++)
            {
                Transform spawn = spawns[i];
                float minDistanceForThisSpawn = float.MaxValue;

                foreach (PlayerHealth p in allPlayers)
                {
                    if (p == this) continue; 
                    if (p.isDead) continue; 

                    float distance = Vector3.Distance(spawn.position, p.transform.position);
                    
                    if (distance < minDistanceForThisSpawn)
                    {
                        minDistanceForThisSpawn = distance;
                    }
                }

                if (minDistanceForThisSpawn > maxScorePosition)
                {
                    maxScorePosition = minDistanceForThisSpawn;
                    chosenSpawn = spawn;
                }
            }

            bestSpawnPos = chosenSpawn.position;
            bestSpawnRot = chosenSpawn.rotation;
            
            // Sécurité anti-collision sol : on surélève légèrement le point choisi
            bestSpawnPos += new Vector3(0, 0.5f, 0);
        }
        else
        {
            bestSpawnPos = transform.position;
            bestSpawnRot = transform.rotation;
        }

        photonView.RPC(nameof(RPC_Respawn), RpcTarget.All, bestSpawnPos, bestSpawnRot);
    }

    [PunRPC]
    void RPC_Respawn(Vector3 targetPosition, Quaternion targetRotation)
    {
        CharacterController cc = GetComponent<CharacterController>();
        
        if (cc != null)
        {
            cc.enabled = false;
        }

        if (TryGetComponent<PlayerController>(out var controller))
        {
            controller.ResetVerticalVelocity();
        }

        // On applique la position
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        if (cc != null) 
        {
            cc.enabled = true;
        }

        // Reset des variables de vie
        currentHealth = maxHealth;
        isDead = false;

        // Réactivation des scripts/visuels
        SetPlayerState(true);
        UpdateUI();
    }

    void UpdateUI()
    {
        if (healthText == null) return;
        healthText.text = Mathf.CeilToInt(currentHealth) + " / " + maxHealth;
    }
}