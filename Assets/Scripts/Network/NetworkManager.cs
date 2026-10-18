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

    private GameMode selectedGameMode;

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

    // Méthode pour se connecter au serveur Photon et rejoindre une salle
    public void ConnectAndJoin(string nickname, GameMode gameMode)
    {
        PhotonNetwork.NickName = nickname;
        selectedGameMode = gameMode;

        // Si je suis déjà connecté, je peux directement rejoindre ou créer une salle
        if (PhotonNetwork.IsConnected)
        {
            JoinOrCreateRoom();
        }
        // Sinon, je me connecte au serveur Photon
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
        // On récupère le nombre max de joueurs selon le mode sélectionné
        byte currentMaxPlayers = GetMaxPlayersForMode(selectedGameMode);

        Hashtable props = new Hashtable
        {
            { "gm", (int)selectedGameMode } // On sauvegarde l'enum sous forme d'entier
        };

        RoomOptions options = new RoomOptions
        {
            MaxPlayers = currentMaxPlayers,
            IsVisible = true,
            CustomRoomProperties = props,
            CustomRoomPropertiesForLobby = new string[] { "gm" }
        };

        Debug.Log($"[Network] Matchmaking mode = {selectedGameMode} | Max Players = {currentMaxPlayers}");

        PhotonNetwork.JoinRandomOrCreateRoom(
            props,
            currentMaxPlayers, // On filtre aussi le matchmaking avec cette limite
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
            PhotonNetwork.LoadLevel("Warehouse");
        }
        else if (gm == 1)
        {
            PhotonNetwork.LoadLevel("Warehouse");
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

    private byte GetMaxPlayersForMode(GameMode mode)
    {
        switch (mode)
        {
            case GameMode.TeamDeathmatch:
                return 10; // 10 joueurs max pour le TDM
            case GameMode.FreeForAll:
                return 8;  // 8 joueurs max pour le FFA
            default:
                return 10; // Valeur de sécurité par défaut
        }
    }
}