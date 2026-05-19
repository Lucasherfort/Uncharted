using Photon.Pun;
using Photon.Realtime;
using UnityEngine;
using System.Collections.Generic;

public class DeathmatchManager : MonoBehaviourPunCallbacks
{
    public static DeathmatchManager Instance;

    [Header("Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    private int playersReady = 0;
    
    // Liste pour suivre quels index de spawn ont déjà été attribués (Master Client uniquement)
    private List<int> usedSpawnIndexes = new List<int>();

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            // Au lieu de spawn au hasard localement, on demande au Master Client quel spawn utiliser
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

        // Mélange ou recherche d'un index non utilisé
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

        // Sécurité : Si on a plus de joueurs que de spawnPoints, on recommence à attribuer au hasard
        if (!foundUniqueSpawn)
        {
            chosenIndex = Random.Range(0, spawnPoints.Length);
            Debug.LogWarning("[DeathmatchManager] Plus de points de spawn uniques disponibles ! Attribution aléatoire par défaut.");
        }

        // Le Master Client renvoie l'index validé UNIQUEMENT au joueur demandeur
        photonView.RPC(nameof(RPC_ReceiveSpawnIndex), requestingPlayer, chosenIndex);
    }

    [PunRPC]
    void RPC_ReceiveSpawnIndex(int spawnIndex)
    {
        // Sécurité au cas où l'index reçu dépasse le tableau local
        if (spawnIndex >= spawnPoints.Length) spawnIndex = 0;

        Transform uniqueSpawn = spawnPoints[spawnIndex];

        // On instancie enfin le joueur sur son point réservé et sécurisé
        PhotonNetwork.Instantiate(
            playerPrefab.name,
            uniqueSpawn.position,
            uniqueSpawn.rotation
        );

        // On prévient le Master Client que nous sommes prêts
        photonView.RPC(nameof(RPC_PlayerReady), RpcTarget.MasterClient);
    }

    // =========================================================================
    // 👤 ETAT DE PREPARATION
    // =========================================================================

    [PunRPC]
    void RPC_PlayerReady()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        playersReady++;

        if (playersReady >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            Debug.Log("<color=green>[DeathmatchManager]</color> Tous les joueurs ont spawn sur des points uniques et sont prêts !");
            // ❗ Démarrer la partie ici (ex: activer les spawner de zombies, lancer le timer, etc.)
        }
    }

    // =========================================================================
    // 🚪 PLAYER LEFT
    // =========================================================================

    public void OnPlayerLeftGame(Player player)
    {
        Debug.Log($"[Survival] {player.NickName} a quitté");
        // ❗ NE PAS détruire les objets Photon ici
    }

    [PunRPC]
    public void RPC_AddKillFeed(string killer, string killed)
    {
        KillFeedUI.Instance.AddKill(killer, killed);
        Debug.Log($"{killer} a tué {killed}");
    }
}