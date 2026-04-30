using UnityEngine;
using Photon.Pun;

public class SurvivalManager : MonoBehaviourPunCallbacks
{
    public static SurvivalManager Instance;

    [Header("Settings")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;
    
    [Header("Managers")]
    public WaveManager waveManager; // Glisse ton WaveManager ici dans l'inspecteur

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

    void SpawnPlayer()
    {
        Transform randomSpawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];
        // On instancie le joueur
        GameObject myPlayer = PhotonNetwork.Instantiate(playerPrefab.name, randomSpawnPoint.position, Quaternion.identity);
        
        // --- ÉTAPE CRUCIALE ---
        // On prévient le MasterClient que NOTRE joueur est bien apparu
        photonView.RPC(nameof(RPC_IReadiedUp), RpcTarget.MasterClient);
    }

    [PunRPC]
    void RPC_IReadiedUp()
    {
        // Seul le MasterClient exécute ce code
        if (!PhotonNetwork.IsMasterClient) return;

        playersReady++;

        // Si le nombre de joueurs prêts correspond au nombre de joueurs dans la room
        if (playersReady >= PhotonNetwork.CurrentRoom.PlayerCount)
        {
            Debug.Log("Tous les joueurs sont là. Lancement de la survie !");
            waveManager.StartFirstWave();
        }
    }
}