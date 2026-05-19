using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Realtime;
using Photon.Pun;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [Header("Panels")]
    public GameObject panelProfile;
    public GameObject panelMatchmaking;
    public GameObject panelLobby;

    [Header("UI Elements")]
    public TMP_InputField nicknameField;
    public Button searchButton;

    [Header("Game Mode")]
    public TMP_Dropdown gameModeDropdown; // 0 = Survie, 1 = Deathmatch

    [Header("Lobby List")]
    public List<PlayerLobbyInfo> playerLobbyInfos = new List<PlayerLobbyInfo>();

    public Button startGameButton;

    void Awake() => Instance = this;

    void Start()
    {
        panelProfile.SetActive(true);
        panelMatchmaking.SetActive(false);
        panelLobby.SetActive(false);
    }

    // 👉 bouton "Game Search"
    public void OnClickGameSearch()
    {
        panelProfile.SetActive(false);
        panelMatchmaking.SetActive(true);
    }

    // 👉 bouton "Find Game"
    public void OnClickFindGame()
    {
        if (string.IsNullOrEmpty(nicknameField.text))
            return;

        searchButton.interactable = false;

        int selectedMode = gameModeDropdown.value;

        NetworkManager.Instance.ConnectAndJoin(nicknameField.text, selectedMode);
    }

    public void SwitchToLobby()
    {
        panelProfile.SetActive(false);
        panelMatchmaking.SetActive(false);
        panelLobby.SetActive(true);

        // reset visuel
        foreach (var info in playerLobbyInfos)
        {
            info.playerNicknameText.text = "<color=#666666>En attente...</color>";
        }
    }

    public void UpdateLobbyUI(int current, int max, Player[] photonPlayers)
    {
        for (int i = 0; i < playerLobbyInfos.Count; i++)
        {
            if (i < photonPlayers.Length)
            {
                Player p = photonPlayers[i];
                string name = p.NickName;

                if (p.IsLocal) name += " <color=green>(Moi)</color>";
                if (p.IsMasterClient) name += " <color=yellow>[Hôte]</color>";

                Sprite avatar = Resources.Load<Sprite>("Level/" + (p.CustomProperties.ContainsKey("Level") ? p.CustomProperties["Level"] : 1)); 

                playerLobbyInfos[i].SetPlayerInfo(name, avatar);
            }
            else
            {
                playerLobbyInfos[i].SetSearchingState();
            }
        }

        if (startGameButton != null)
        {
            startGameButton.gameObject.SetActive(PhotonNetwork.IsMasterClient);
        }
    }

    public void OnClickStartGame()
    {
        if (PhotonNetwork.IsMasterClient)
        {
            NetworkManager.Instance.StartGame();
        }
    }
}