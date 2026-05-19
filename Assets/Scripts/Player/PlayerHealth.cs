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
        if (!photonView.IsMine) return;
        if (isDead) return;

        photonView.RPC(nameof(RPC_TakeDamage), RpcTarget.All, amount, attackerActorNumber);
    }

    [PunRPC]
    void RPC_TakeDamage(float amount, int attackerActorNumber)
    {
        currentHealth -= amount;
        currentHealth = Mathf.Clamp(currentHealth, 0, maxHealth);

        if (photonView.IsMine)
        {
            damageEffect?.OnDamage();

            // --- LOGIQUE RÉALISTE DE RÉGÉNÉRATION ---
            // Si on subit des dégâts, on stoppe immédiatement la régénération en cours
            if (regenCoroutine != null)
            {
                StopCoroutine(regenCoroutine);
            }

            // Si le joueur est toujours en vie, on relance le processus (attente du choc + soin)
            if (currentHealth > 0 && !isDead)
            {
                regenCoroutine = StartCoroutine(RegenerationRoutine());
            }
        }

        UpdateUI();

        // Détection de la mort
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;

            // On stoppe définitivement la régénération si on meurt
            if (photonView.IsMine && regenCoroutine != null)
            {
                StopCoroutine(regenCoroutine);
            }

            // On récupère le profil de l'attaquant via son ID Photon
            Player attacker = PhotonNetwork.CurrentRoom.GetPlayer(attackerActorNumber);
            string attackerName = attacker != null ? attacker.NickName : "Environnement";

            // Envoi au Killfeed
            if (photonView.IsMine)
            {
                DeathmatchManager.Instance.photonView.RPC(
                    nameof(DeathmatchManager.RPC_AddKillFeed), 
                    RpcTarget.All, 
                    attackerName, 
                    photonView.Owner.NickName
                );
            }

            // --- GESTION DU SCORE (CUSTOM PROPERTIES) ---
            if (PhotonNetwork.IsMasterClient && attacker != null && attacker != photonView.Owner)
            {
                int currentKills = 0;
                
                if (attacker.CustomProperties.TryGetValue("Kills", out object killsObj))
                {
                    currentKills = (int)killsObj;
                }

                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props["Kills"] = currentKills + 1;
                attacker.SetCustomProperties(props);
            }

            photonView.RPC(nameof(RPC_Die), RpcTarget.All);
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
        Debug.Log($"<color=cyan>[RESPAWN DEBUT]</color> Début du calcul de spawn pour {photonView.Owner.NickName}");
        yield return new WaitForSeconds(respawnDelay);

        Vector3 bestSpawnPos = Vector3.zero;
        Quaternion bestSpawnRot = Quaternion.identity;

        if (DeathmatchManager.Instance != null && DeathmatchManager.Instance.spawnPoints.Length > 0)
        {
            Transform[] spawns = DeathmatchManager.Instance.spawnPoints;
            Debug.Log($"[RESPAWN LOGIQUE] Nombre de points de spawn trouvés dans DeathmatchManager : {spawns.Length}");
            
            PlayerHealth[] allPlayers = FindObjectsOfType<PlayerHealth>();
            Debug.Log($"[RESPAWN LOGIQUE] Nombre total de joueurs détectés sur la map : {allPlayers.Length}");

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

                Debug.Log($"[RESPAWN ANALYSE] Point {i} ({spawn.name}) -> Distance du joueur le plus proche : {minDistanceForThisSpawn}m");

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