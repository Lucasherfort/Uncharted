using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;
using TMPro;

public class DeathmatchManager : MonoBehaviourPunCallbacks
{
    public static DeathmatchManager Instance;

    [Header("Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;
    [SerializeField] private int goal = 10; // 10 kills pour gagner

    [Header("End Game UI")]
    [Tooltip("L'objet Panel/Pop-up de fin dans ton Canvas GameUI")]
    public GameObject endGamePanel; 
    [Tooltip("Le composant texte à l'intérieur du panel de fin")]
    public TMP_Text victoryText;

    private int playersReady = 0;
    private bool isMatchOver = false; 
    
    // 🔥 Liste des index de spawn encore disponibles (mélangée dynamiquement)
    private List<int> availableSpawnIndexes = new List<int>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (endGamePanel != null) endGamePanel.SetActive(false);

        // 🔥 Seul le Master Client initialise et mélange les spawns au démarrage de la scène
        if (PhotonNetwork.IsConnected && PhotonNetwork.IsMasterClient)
        {
            GenerateRandomizedSpawnList();
        }

        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC(nameof(RPC_RequestSpawnIndex), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
        }
    }

    // =========================================================================
    // 🎲 GENERATEUR DE SPAWNS ALEATOIRES (ANTI-DOUBLONS & SHUFFLE)
    // =========================================================================

    private void GenerateRandomizedSpawnList()
    {
        availableSpawnIndexes.Clear();
        
        // 1. On remplit la liste avec tous les index existants
        for (int i = 0; i < spawnPoints.Length; i++)
        {
            availableSpawnIndexes.Add(i);
        }

        // 2. Algorithme de Fisher-Yates pour mélanger aléatoirement la liste
        for (int i = availableSpawnIndexes.Count - 1; i > 0; i--)
        {
            int randomIndex = Random.Range(0, i + 1);
            int temp = availableSpawnIndexes[i];
            availableSpawnIndexes[i] = availableSpawnIndexes[randomIndex];
            availableSpawnIndexes[randomIndex] = temp;
        }
        
        Debug.Log("<color=cyan>[DeathmatchManager]</color> Liste des points de spawn mélangée avec succès !");
    }

    [PunRPC]
    void RPC_RequestSpawnIndex(Player requestingPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int chosenIndex = 0;

        // Si on a encore des spawns uniques mélangés dans notre liste
        if (availableSpawnIndexes.Count > 0)
        {
            // On pioche le premier index de la liste mélangée
            chosenIndex = availableSpawnIndexes[0];
            // On le retire pour qu'aucun autre joueur ne puisse le piocher
            availableSpawnIndexes.RemoveAt(0);
        }
        else
        {
            // Sécurité : Si plus de spawns que de places, on prend de l'aléatoire pur
            chosenIndex = Random.Range(0, spawnPoints.Length);
            Debug.LogWarning("[DeathmatchManager] Plus de points de spawn uniques ! Attribution aléatoire brute.");
        }

        photonView.RPC(nameof(RPC_ReceiveSpawnIndex), requestingPlayer, chosenIndex);
    }

    [PunRPC]
    void RPC_ReceiveSpawnIndex(int spawnIndex)
    {
        if (spawnIndex >= spawnPoints.Length) spawnIndex = 0;

        Transform uniqueSpawn = spawnPoints[spawnIndex];

        PhotonNetwork.Instantiate(
            playerPrefab.name,
            uniqueSpawn.position,
            uniqueSpawn.rotation
        );

        photonView.RPC(nameof(RPC_PlayerReady), RpcTarget.MasterClient);
    }

    // =========================================================================
    // 👤 ETAT DE PREPARATION
    // =========================================================================

    [PunRPC]
    void RPC_PlayerReady()
    {
        if (!PhotonNetwork.IsMasterClient) return;

        playersReady++;

        if (playersReady >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            Debug.Log("<color=green>[DeathmatchManager]</color> Tous les joueurs ont spawn de manière aléatoire et unique !");
        }
    }

    // =========================================================================
    // 🚪 PLAYER LEFT
    // =========================================================================

    // Correction de la callback Photon pour correspondre à MonoBehaviourPunCallbacks
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Deathmatch] {otherPlayer.NickName} a quitté la partie.");
    }

    [PunRPC]
    public void RPC_AddKillFeed(string killer, string killed)
    {
        if (KillFeedUI.Instance != null)
        {
            KillFeedUI.Instance.AddKill(killer, killed);
        }
        Debug.Log($"{killer} a tué {killed}");
    }

    // =========================================================================
    // 📊 ENREGISTREMENT DES KILLS & DETECTION DE FIN
    // =========================================================================

    [PunRPC]
    public void RPC_RegisterKill(int killerActorNumber)
    {
        if (!PhotonNetwork.IsMasterClient || isMatchOver)
            return;

        Player killer = PhotonNetwork.CurrentRoom.GetPlayer(killerActorNumber);

        if (killer == null)
        {
            Debug.LogWarning("[MASTER] Killer NULL");
            return;
        }

        int currentKills = 0;

        if (killer.CustomProperties != null &&
            killer.CustomProperties.TryGetValue("Kills", out object obj))
        {
            currentKills = (int)obj;
        }

        int newKillsValue = currentKills + 1;

        var props = new ExitGames.Client.Photon.Hashtable();
        props["Kills"] = newKillsValue;
        killer.SetCustomProperties(props);

        Debug.Log($"[MASTER] Kill mis à jour pour {killer.NickName} = {newKillsValue}");

        if (newKillsValue >= goal)
        {
            isMatchOver = true;
            photonView.RPC(nameof(RPC_TriggerEndGame), RpcTarget.All, killer.NickName);
        }
    }

    // =========================================================================
    // 🏆 ARRET ET RECEPTION DU GAGNANT
    // =========================================================================

    [PunRPC]
    void RPC_TriggerEndGame(string winnerName)
    {
        isMatchOver = true; 

        Debug.Log($"[END GAME] Le joueur {winnerName} a gagné la partie !");

        // Désactivation des tirs
        PlayerShooter[] allShooters = FindObjectsOfType<PlayerShooter>();
        foreach (PlayerShooter shooter in allShooters)
        {
            shooter.enabled = false;
        }

        // Désactivation des mouvements
        PlayerController[] allMovements = FindObjectsOfType<PlayerController>();
        foreach (PlayerController move in allMovements) 
        { 
            move.enabled = false; 
        }

        // Affichage UI
        if (endGamePanel != null) endGamePanel.SetActive(true);

        if (victoryText != null)
        {
            victoryText.text = $"Le joueur {winnerName} a gagné la partie !";
        }
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}