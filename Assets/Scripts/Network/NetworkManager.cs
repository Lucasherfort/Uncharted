using UnityEngine;
using Photon.Pun;
using Photon.Realtime;
using ExitGames.Client.Photon;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;

    [Header("Settings")]
    [SerializeField] private byte maxPlayersPerRoom = 10;
    [SerializeField] private string gameVersion = "1.0";

    private int selectedGameMode = 0; // 0 = Survie, 1 = Deathmatch

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

    public void ConnectAndJoin(string nickname, int gameMode)
    {
        PhotonNetwork.NickName = nickname;
        selectedGameMode = gameMode;

        // 👇 propriétés joueur
        Hashtable playerProps = new()
        {
            { "Level", 1 } 
        };

        PhotonNetwork.LocalPlayer.SetCustomProperties(playerProps);

        if (PhotonNetwork.IsConnected)
        {
            JoinOrCreateRoom();
        }
        else
        {
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        JoinOrCreateRoom();
    }

    void JoinOrCreateRoom()
    {
        Hashtable props = new Hashtable
        {
            { "gm", selectedGameMode }
        };

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = maxPlayersPerRoom,
            IsVisible = true,
            CustomRoomProperties = props,
            CustomRoomPropertiesForLobby = new string[] { "gm" }
        };

        Debug.Log($"[Network] Matchmaking mode = {selectedGameMode}");

        PhotonNetwork.JoinRandomOrCreateRoom(
            props,
            maxPlayersPerRoom,
            MatchmakingMode.FillRoom,
            TypedLobby.Default,
            null,
            null,
            options
        );
    }

    // =========================
    // 🏠 ROOM
    // =========================

    public override void OnJoinedRoom()
    {
        Debug.Log($"Joined Room ({PhotonNetwork.CurrentRoom.PlayerCount})");

        if (MenuManager.Instance != null)
        {
            MenuManager.Instance.SwitchToLobby();
        }

        RefreshLobbyStatus();
        CheckRoomState();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        RefreshLobbyStatus();
        CheckRoomState();
    }

    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        RefreshLobbyStatus();
        CheckRoomState();

        if (SurvivalManager.Instance != null)
        {
            SurvivalManager.Instance.OnPlayerLeftGame(otherPlayer);
        }
    }

    // =========================
    // 🎮 START GAME
    // =========================

    public void StartGame()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        PhotonNetwork.CurrentRoom.IsOpen = false;
        PhotonNetwork.CurrentRoom.IsVisible = false;

        int gm = (int)PhotonNetwork.CurrentRoom.CustomProperties["gm"];

        if (gm == 0)
        {
            PhotonNetwork.LoadLevel("Survival");
        }
        else if (gm == 1)
        {
            PhotonNetwork.LoadLevel("Deathmatch2");
        }
    }

    // =========================
    // 🧠 UTILS
    // =========================

    void RefreshLobbyStatus()
    {
        if (PhotonNetwork.CurrentRoom == null) return;
        if (MenuManager.Instance == null) return;

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

        PhotonNetwork.CurrentRoom.IsOpen = current < maxPlayersPerRoom;
    }
}