using Photon.Pun;
using Photon.Realtime;
using UnityEngine;

public class DeathmatchManager : MonoBehaviourPunCallbacks
{
    public static DeathmatchManager Instance;

    [Header("Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    private int playersReady = 0;

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        if (PhotonNetwork.IsConnected)
        {
            SpawnPlayer();
        }
    }

    // =========================
    // 👤 SPAWN
    // =========================

    void SpawnPlayer()
    {
        Transform spawn = spawnPoints[Random.Range(0, spawnPoints.Length)];

        PhotonNetwork.Instantiate(
            playerPrefab.name,
            spawn.position,
            spawn.rotation
        );

        photonView.RPC(nameof(RPC_PlayerReady), RpcTarget.MasterClient);
    }

    [PunRPC]
    void RPC_PlayerReady()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        playersReady++;

        if (playersReady >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            Debug.Log("Tous les joueurs sont prêts");

            // ❗ Démarrer la partie ici (ex: activer les spawners, etc.)
        }
    }

    // =========================
    // 🚪 PLAYER LEFT
    // =========================

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
