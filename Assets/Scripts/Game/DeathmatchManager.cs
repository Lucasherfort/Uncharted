using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;
using TMPro; // Requis si ton texte de fin utilise TextMeshPro. Sinon, utilise UnityEngine.UI;

public class DeathmatchManager : MonoBehaviourPunCallbacks
{
    public static DeathmatchManager Instance;

    [Header("Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;
    [SerializeField] private int goal = 10; // 20 kills pour gagner

    [Header("End Game UI UI")]
    [Tooltip("L'objet Panel/Pop-up de fin dans ton Canvas GameUI")]
    public GameObject endGamePanel; 
    [Tooltip("Le composant texte à l'intérieur du panel de fin")]
    public TMP_Text victoryText; // Remplace par 'public Text victoryText;' si UI classique

    private int playersReady = 0;
    private bool isMatchOver = false; // Sécurité pour éviter de déclencher la fin plusieurs fois
    
    // Liste pour suivre quels index de spawn ont déjà été attribués (Master Client uniquement)
    private List<int> usedSpawnIndexes = new List<int>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        // On s'assure que l'UI de fin est bien cachée au lancement
        if (endGamePanel != null) endGamePanel.SetActive(false);

        if (PhotonNetwork.IsConnected)
        {
            photonView.RPC(nameof(RPC_RequestSpawnIndex), RpcTarget.MasterClient, PhotonNetwork.LocalPlayer);
        }
    }

    // =========================================================================
    // 👤 SYSTEME DE DISPATCH DES SPAWNS (ANTI-DOUBLONS)
    // =========================================================================

    [PunRPC]
    void RPC_RequestSpawnIndex(Player requestingPlayer)
    {
        if (!PhotonNetwork.IsMasterClient) return;

        int chosenIndex = 0;
        bool foundUniqueSpawn = false;

        for (int i = 0; i < spawnPoints.Length; i++)
        {
            if (!usedSpawnIndexes.Contains(i))
            {
                chosenIndex = i;
                usedSpawnIndexes.Add(i);
                foundUniqueSpawn = true;
                break;
            }
        }

        if (!foundUniqueSpawn)
        {
            chosenIndex = Random.Range(0, spawnPoints.Length);
            Debug.LogWarning("[DeathmatchManager] Plus de points de spawn uniques disponibles ! Attribution aléatoire par défaut.");
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
            Debug.Log("<color=green>[DeathmatchManager]</color> Tous les joueurs ont spawn sur des points uniques et sont prêts !");
        }
    }

    // =========================================================================
    // 🚪 PLAYER LEFT
    // =========================================================================

    public void OnPlayerLeftGame(Player player)
    {
        Debug.Log($"[Survival] {player.NickName} a quitté");
    }

    [PunRPC]
    public void RPC_AddKillFeed(string killer, string killed)
    {
        KillFeedUI.Instance.AddKill(killer, killed);
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

        Debug.Log($"[MASTER] Kill updated for {killer.NickName} = {newKillsValue}");

        // 🔥 CHECK DE VICTOIRE : Est-ce que le tueur vient d'atteindre le Goal ?
        if (newKillsValue >= goal)
        {
            isMatchOver = true;
            Debug.Log($"[MASTER] {killer.NickName} a atteint le score limite ! Fin du match.");
            
            // On ordonne à TOUT LE MONDE d'arrêter la partie et d'afficher le gagnant
            photonView.RPC(nameof(RPC_TriggerEndGame), RpcTarget.All, killer.NickName);
        }
    }

    // =========================================================================
    // 🏆 ARRET ET RECEPTION DU GAGNANT (SUR TOUS LES PC)
    // =========================================================================

    [PunRPC]
    void RPC_TriggerEndGame(string winnerName)
    {
        isMatchOver = true; // Bloque aussi l'état en local

        Debug.Log($"[END GAME] Le joueur {winnerName} a gagné la partie !");

        // 1. Désactivation des contrôles de TOUS les joueurs présents dans la scène
        // (On cherche les scripts de mouvement/tir pour couper les actions)
        PlayerShooter[] allShooters = FindObjectsOfType<PlayerShooter>();
        foreach (PlayerShooter shooter in allShooters)
        {
            shooter.enabled = false; // Empêche de tirer
        }

        // 💡 Astuce : Si tu as un script "PlayerMovement", désactive-le ici de la même manière :

        PlayerController[] allMovements = FindObjectsOfType<PlayerController>();
        foreach (PlayerController move in allMovements) { move.enabled = false; }

        // 2. Affichage et mise à jour de l'UI de fin de partie
        if (endGamePanel != null)
        {
            endGamePanel.SetActive(true);
        }

        if (victoryText != null)
        {
            victoryText.text = $"Le joueur {winnerName} a gagné la partie !";
        }
        
        // 3. Optionnel : Libérer le curseur de la souris pour pouvoir cliquer sur un bouton "Quitter"
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }
}