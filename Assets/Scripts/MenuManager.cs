using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using Photon.Pun;
using Photon.Realtime;

public class MenuManager : MonoBehaviour
{
    public static MenuManager Instance;

    [Header("Panels")]
    public GameObject panelProfile;
    public GameObject panelLobby;

    [Header("UI Elements")]
    public TMP_InputField nicknameField;
    public TextMeshProUGUI lobbyStatusText;
    public Button searchButton;

    [Header("Lobby List")]
    // Liste de tes scripts/objets attachés à tes slots UI
    public List<PlayerLobbyInfo> playerLobbyInfos = new List<PlayerLobbyInfo>();

    void Awake() => Instance = this;

    void Start()
    {
        panelProfile.SetActive(true);
        panelLobby.SetActive(false);
    }

    public void OnClickSearch()
    {
        if (!string.IsNullOrEmpty(nicknameField.text))
        {
            searchButton.interactable = false;
            NetworkManager.Instance.ConnectAndJoin(nicknameField.text);
        }
    }

    public void SwitchToLobby()
    {
        panelProfile.SetActive(false);
        panelLobby.SetActive(true);

        // Reset visuel à l'entrée
        foreach (var info in playerLobbyInfos)
        {
            info.playerNicknameText.text = "<color=#666666>En attente...</color>";
        }
    }

    // On ajoute le paramètre Player[] pour recevoir la liste de Photon
    public void UpdateLobbyUI(int current, int max, Player[] photonPlayers)
    {
        lobbyStatusText.text = $"En attente de joueurs... ({current}/{max})";

        // On parcourt tous les slots disponibles dans ton interface
        for (int i = 0; i < playerLobbyInfos.Count; i++)
        {
            // Si on a un joueur correspondant à cet index dans la liste Photon
            if (i < photonPlayers.Length)
            {
                Player p = photonPlayers[i];
                string displayName = p.NickName;

                if (p.IsLocal) displayName += " <color=green>(Moi)</color>";
                if (p.IsMasterClient) displayName += " <color=yellow>[Hôte]</color>";

                playerLobbyInfos[i].playerNicknameText.text = displayName;
            }
            else
            {
                // Slot vide si moins de joueurs que de slots
                playerLobbyInfos[i].playerNicknameText.text = "<color=#666666>Recherche de joueur...</color>";
            }
        }
    }
}