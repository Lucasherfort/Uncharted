using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMP_Text = TMPro.TMP_Text;

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

    // --- ADAPTATION : On passe désormais l'ID de l'attaquant (ActorNumber) ---
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
        }

        UpdateUI();

        // Détection de la mort
        if (currentHealth <= 0 && !isDead)
        {
            isDead = true;

            // On récupère le profil de l'attaquant via son ID Photon
            Player attacker = PhotonNetwork.CurrentRoom.GetPlayer(attackerActorNumber);
            string attackerName = attacker != null ? attacker.NickName : "Environnement";

            // Envoi au Killfeed (Uniquement par celui qui meurt pour éviter les doublons d'affichage)
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
            // Seul le MasterClient gère l'attribution des points pour éviter la triche et les doublons
            if (PhotonNetwork.IsMasterClient && attacker != null && attacker != photonView.Owner)
            {
                int currentKills = 0;
                
                // Si l'attaquant avait déjà des kills, on récupère sa valeur
                if (attacker.CustomProperties.TryGetValue("Kills", out object killsObj))
                {
                    currentKills = (int)killsObj;
                }

                // On incrémente et on renvoie à Photon pour la synchronisation automatique
                ExitGames.Client.Photon.Hashtable props = new ExitGames.Client.Photon.Hashtable();
                props["Kills"] = currentKills + 1;
                attacker.SetCustomProperties(props);
            }

            // On informe tout le monde du décès pour couper les visuels
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

        if (SurvivalManager.Instance != null && SurvivalManager.Instance.spawnPoints.Length > 0)
        {
            Transform[] spawns = SurvivalManager.Instance.spawnPoints;
            
            // On récupère tous les joueurs présents dans la partie
            PlayerHealth[] allPlayers = FindObjectsOfType<PlayerHealth>();

            float maxScorePosition = -1f;
            Transform chosenSpawn = spawns[0]; // Par défaut, le premier si le calcul échoue

            // On va analyser CHAQUE point de spawn
            foreach (Transform spawn in spawns)
            {
                float minDistanceForThisSpawn = float.MaxValue;

                // On calcule la distance entre CE point de spawn et CHAQUE joueur
                foreach (PlayerHealth p in allPlayers)
                {
                    // On ignore le joueur qui est mort (puisqu'il n'est plus physiquement sur la map)
                    if (p == this) continue; 
                    
                    // On peut aussi ignorer les joueurs déjà morts si besoin
                    // if (p.isDead) continue; 

                    float distance = Vector3.Distance(spawn.position, p.transform.position);
                    
                    // On cherche le joueur le plus PROCHE de ce spawn
                    if (distance < minDistanceForThisSpawn)
                    {
                        minDistanceForThisSpawn = distance;
                    }
                }

                // Plus la "distance minimale" est grande, plus ce spawn est sécurisé (loin de tout le monde)
                if (minDistanceForThisSpawn > maxScorePosition)
                {
                    maxScorePosition = minDistanceForThisSpawn;
                    chosenSpawn = spawn;
                }
            }

            // Une fois le meilleur point trouvé, on extrait ses coordonnées
            bestSpawnPos = chosenSpawn.position;
            bestSpawnRot = chosenSpawn.rotation;
        }
        else
        {
            // Sécurité si aucun point de spawn n'est configuré
            bestSpawnPos = transform.position;
            bestSpawnRot = transform.rotation;
        }
        
        // On envoie la position idéale validée à tout le monde
        photonView.RPC(nameof(RPC_Respawn), RpcTarget.All, bestSpawnPos, bestSpawnRot);
    }

[PunRPC]
    void RPC_Respawn(Vector3 targetPosition, Quaternion targetRotation)
    {
        // 1. On cherche le CharacterController
        CharacterController cc = GetComponent<CharacterController>();
        
        // 2. On le coupe S'IL EXISTE (très important)
        if (cc != null) cc.enabled = false;

        // 3. On applique la position
        transform.position = targetPosition;
        transform.rotation = targetRotation;

        // 4. On le réactive immédiatement après
        if (cc != null) cc.enabled = true;

        // Reset des variables de vie
        currentHealth = maxHealth;
        isDead = false;

        // Réactivation des scripts/visuels
        SetPlayerState(true);
        UpdateUI();
        
        Debug.Log($"<color=green>[Health]</color> Respawn forcé de {photonView.Owner.NickName} à la position {targetPosition}");
    }

    void UpdateUI()
    {
        if (healthText == null) return;
        healthText.text = Mathf.CeilToInt(currentHealth) + " / " + maxHealth;
    }
}