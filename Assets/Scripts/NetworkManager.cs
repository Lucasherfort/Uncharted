using UnityEngine;
using Photon.Pun;
using Photon.Realtime;

public class NetworkManager : MonoBehaviourPunCallbacks
{
    public static NetworkManager Instance;
    
    [Header("Settings")]
    [SerializeField] private byte maxPlayersPerRoom = 10;
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
    if (PhotonNetwork.CurrentRoom == null) return;

    int current = PhotonNetwork.CurrentRoom.PlayerCount;
    int max = PhotonNetwork.CurrentRoom.MaxPlayers;
    
    // On passe la liste des joueurs connectés
    MenuManager.Instance.UpdateLobbyUI(current, max, PhotonNetwork.PlayerList);
}

private void CheckFullRoom()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            int current = PhotonNetwork.CurrentRoom.PlayerCount;
            
            if (current >= maxPlayersPerRoom)
            {
                Debug.Log("<color=green>[Network]</color> Salle pleine. Fermeture des entrées.");
                PhotonNetwork.CurrentRoom.IsOpen = false; 
                // PhotonNetwork.LoadLevel("Survival"); 
            }
            else
            {
                // IMPORTANT : Si on n'est plus assez, on réouvre la porte !
                if (!PhotonNetwork.CurrentRoom.IsOpen)
                {
                    Debug.Log("<color=yellow>[Network]</color> Place libérée. Réouverture de la salle.");
                    PhotonNetwork.CurrentRoom.IsOpen = true;
                }
            }
        }
    }

    // Appelé automatiquement par Photon quand UN AUTRE joueur quitte la room
    /// <summary>
    /// Called automatically by Photon when another player leaves the room.
    /// </summary>
    /// <param name="otherPlayer">The player who left the room.</param>
    public override void OnPlayerLeftRoom(Player otherPlayer)
    {
        Debug.Log($"<color=orange>[Network]</color> {otherPlayer.NickName} a quitté la salle.");
    
        // On rafraîchit l'affichage pour libérer le slot
        RefreshLobbyStatus();

        // On vérifie s'il faut réouvrir la salle maintenant qu'il y a de la place
        CheckFullRoom();
    }
}