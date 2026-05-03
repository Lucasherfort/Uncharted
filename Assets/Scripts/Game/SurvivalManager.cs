using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class SurvivalManager : MonoBehaviourPunCallbacks
{
    public static SurvivalManager Instance;

    [Header("Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    [Header("Managers")]
    public WaveManager waveManager;

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

            if (waveManager != null)
            {
                waveManager.StartFirstWave();
            }
        }
    }

    // =========================
    // 🚪 PLAYER LEFT
    // =========================

    public void OnPlayerLeftGame(Player player)
    {
        Debug.Log($"[Survival] {player.NickName} a quitté");

        // ❗ NE PAS détruire les objets Photon ici

        // 🔥 Nettoyage logique seulement
        ResetZombieTargets(player);
    }

    void ResetZombieTargets(Player player)
    {
        EnemyController[] zombies = FindObjectsOfType<EnemyController>();

        foreach (var z in zombies)
        {
            if (z.player == null)
                continue;

            PhotonView pv = z.player.GetComponent<PhotonView>();

            if (pv != null && pv.Owner == player)
            {
                z.player = null;
            }
        }
    }
}