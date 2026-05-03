using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    [Header("Settings")]
    [SerializeField] private byte maxPlayersPerRoom = 10;
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private string roomName = "SurvivalRoom_Alpha";

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);

            PhotonNetwork.AutomaticallySyncScene = true;
            PhotonNetwork.GameVersion = gameVersion;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    // =========================
    // 🔌 CONNEXION
    // =========================

    public void ConnectAndJoin(string nickname)
    {
        PhotonNetwork.NickName = nickname;

        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("[Network] Déjà connecté");
            JoinOrCreateRoom();
        }
        else
        {
            Debug.Log("[Network] Connexion...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("[Network] Connecté au Master");
        JoinOrCreateRoom();
    }

    void JoinOrCreateRoom()
    {
        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true
        };

        Debug.Log("[Network] JoinOrCreate Room");
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    // =========================
    // 🏠 ROOM
    // =========================

public override void OnJoinedRoom()
{
    Debug.Log($"[Network] Room: {PhotonNetwork.CurrentRoom.Name}");

    // 🔥 D'abord afficher le lobby
    if (MenuManager.Instance != null)
    {
        MenuManager.Instance.SwitchToLobby();
    }

    // 🔥 Ensuite mettre à jour
    RefreshLobbyStatus();
    CheckRoomState();
}

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"[Network] {newPlayer.NickName} joined");

        RefreshLobbyStatus();
        CheckRoomState();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"[Network] {otherPlayer.NickName} left");

        RefreshLobbyStatus();
        CheckRoomState();

        // 🔥 GAME LOGIC uniquement (pas de Destroy réseau)
        if (SurvivalManager.Instance != null)
        {
            SurvivalManager.Instance.OnPlayerLeftGame(otherPlayer);
        }
    }

    // =========================
    // 🎮 GAME START
    // =========================

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Debug.Log("[Network] Start Game");

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        PhotonNetwork.LoadLevel("Survival");
    }

    // =========================
    // 🧠 UTILS
    // =========================

    void RefreshLobbyStatus()
    {
        if (PhotonNetwork.CurrentRoom == null)
            return;

        if (MenuManager.Instance == null)
            return;

        MenuManager.Instance.UpdateLobbyUI(
            PhotonNetwork.CurrentRoom.PlayerCount,
            PhotonNetwork.CurrentRoom.MaxPlayers,
            PhotonNetwork.PlayerList
        );
    }

    void CheckRoomState()
    {
        if (!PhotonNetwork.IsMasterClient || PhotonNetwork.CurrentRoom == null)
            return;

        int current = PhotonNetwork.CurrentRoom.PlayerCount;

        if (current >= maxPlayersPerRoom)
        {
            PhotonNetwork.CurrentRoom.IsOpen = false;
        }
        else
        {
            PhotonNetwork.CurrentRoom.IsOpen = true;
        }
    }
}