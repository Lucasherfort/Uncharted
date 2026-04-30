using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;
    
    [Header("Settings")]
    [SerializeField] private byte maxPlayersPerRoom = 2;
    [SerializeField] private string gameVersion = "1.0";
    [SerializeField] private string roomName = "SurvivalRoom_Alpha"; // Nom fixe pour forcer la rencontre

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

    public void ConnectAndJoin(string nickname)
    {
        PhotonNetwork.NickName = nickname;

        if (PhotonNetwork.IsConnected)
        {
            Debug.Log("<color=cyan>[Network]</color> Déjà connecté, accès à la salle...");
            JoinOrCreateSurvivalRoom();
        }
        else
        {
            Debug.Log("<color=cyan>[Network]</color> Connexion aux serveurs Photon...");
            PhotonNetwork.ConnectUsingSettings();
        }
    }

    public override void OnConnectedToMaster()
    {
        Debug.Log("<color=green>[Network]</color> Connecté au Master Server. Région : " + PhotonNetwork.CloudRegion);
        JoinOrCreateSurvivalRoom();
    }

    private void JoinOrCreateSurvivalRoom()
    {
        RoomOptions options = new RoomOptions();
        options.MaxPlayers = maxPlayersPerRoom;
        // Propriétés pour le matchmaking (au cas où)
        options.CustomRoomProperties = new ExitGames.Client.Photon.Hashtable { { "gm", 0 } };
        options.CustomRoomPropertiesForLobby = new string[] { "gm" };

        Debug.Log("<color=cyan>[Network]</color> Tentative JoinOrCreate de la salle : " + roomName);
        PhotonNetwork.JoinOrCreateRoom(roomName, options, TypedLobby.Default);
    }

    public override void OnJoinedRoom()
    {
        Debug.Log($"<color=green>[Network]</color> Salle rejointe : {PhotonNetwork.CurrentRoom.Name}. Joueurs : {PhotonNetwork.CurrentRoom.PlayerCount}");
        MenuManager.Instance.SwitchToLobby();
        RefreshLobbyStatus();
        CheckFullRoom();
    }

    public override void OnPlayerEnteredRoom(Player newPlayer)
    {
        Debug.Log($"<color=yellow>[Network]</color> {newPlayer.NickName} est entré dans la salle.");
        RefreshLobbyStatus();
        CheckFullRoom();
    }

    private void RefreshLobbyStatus()
    {
        int current = PhotonNetwork.CurrentRoom.PlayerCount;
        int max = PhotonNetwork.CurrentRoom.MaxPlayers;
        MenuManager.Instance.UpdateLobbyUI(current, max);
    }

    private void CheckFullRoom()
    {
        // Seul le Master décide du lancement
        if (PhotonNetwork.IsMasterClient)
        {
            int current = PhotonNetwork.CurrentRoom.PlayerCount;
            if (current >= maxPlayersPerRoom)
            {
                Debug.Log("<color=green>[Network]</color> Salle pleine. Lancement imminent !");
                PhotonNetwork.CurrentRoom.IsOpen = false; // On ferme la porte
                PhotonNetwork.LoadLevel("Survival"); // NOM DE TA SCÈNE DE JEU
            }
        }
    }
}