using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using TMPro;

public class Launcher : MonoBehaviourPunCallbacks
{
    [Header("UI")]
    public TMP_InputField playerNameInput;
    public GameObject Lobby;
    public GameObject Waiting;
    public GameObject Wave;

    public TMP_Text TxtRoom;
    public TMP_Text TxtAttente;

    [Header("Game")]
    public GameObject playerPrefab;
    public Transform[] spawnPoints;

    void Start()
    {
        PhotonNetwork.ConnectUsingSettings();
    }

    public override void OnConnectedToMaster()
    {
        PhotonNetwork.JoinLobby();
    }

    public override void OnJoinedLobby()
    {
        Debug.Log("Joined Lobby");
        Lobby.SetActive(true);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log("Joined Room: " + PhotonNetwork.CurrentRoom.Name);

        CheckStartGame();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log("Player joined: " + newPlayer.NickName);

        // 🔥 IMPORTANT : seul le Master décide de lancer la game
        if (PhotonNetwork.IsMasterClient)
        {
            CheckStartGame();
        }
    }

    void CheckStartGame()
    {
        if (PhotonNetwork.CurrentRoom.PlayerCount < PhotonNetwork.CurrentRoom.MaxPlayers)
        {
            Waiting.SetActive(true);
            TxtAttente.text = "En attente d'autres joueurs...";
            return;
        }

        // 🔥 Tous les joueurs sont là → lancer la partie
        if (PhotonNetwork.IsMasterClient)
        {
            photonView.RPC(nameof(RPC_StartGame), RpcTarget.All);
        }
    }

    [PunRPC]
    void RPC_StartGame()
    {
        // 🔥 IMPORTANT : désactiver le Waiting pour tout le monde
        Waiting.SetActive(false);

        TxtRoom.text = "Room: " + PhotonNetwork.CurrentRoom.Name;

        // 🔥 Spawn unique basé sur ActorNumber
        int spawnIndex = (PhotonNetwork.LocalPlayer.ActorNumber - 1) % spawnPoints.Length;

        Transform spawnPoint = spawnPoints[spawnIndex];

        PhotonNetwork.Instantiate(
            playerPrefab.name,
            spawnPoint.position,
            spawnPoint.rotation
        );

        Wave.SetActive(true);

        // 🔥 Master lance les vagues
        if (PhotonNetwork.IsMasterClient)
        {
            WaveManager waveManager = FindObjectOfType<WaveManager>();

            if (waveManager != null)
            {
                //waveManager.StartFirstWave();
            }
            else
            {
                Debug.LogWarning("WaveManager introuvable !");
            }
        }
    }

    public override void OnJoinRandomFailed(short returnCode, string message)
    {
        Debug.Log("Failed to join random room: " + message);
    }

    public void JoindreRoomPrincipal()
    {
        Lobby.SetActive(false);

        PhotonNetwork.NickName = string.IsNullOrEmpty(playerNameInput.text)
            ? "Player" + Random.Range(0, 1000)
            : playerNameInput.text;

        RoomOptions roomOptions = new RoomOptions
        {
            MaxPlayers = 2,
            IsVisible = true,
        };

        PhotonNetwork.JoinRandomOrCreateRoom(
            null,
            2,
            MatchmakingMode.FillRoom,
            TypedLobby.Default,
            null,
            null,
            roomOptions
        );
    }
}