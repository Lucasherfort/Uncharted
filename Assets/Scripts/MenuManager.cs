using UnityEngine;
using TMPro;
using UnityEngine.UI;

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
    }

    public void UpdateLobbyUI(int current, int max)
    {
        lobbyStatusText.text = $"En attente de joueurs... ({current}/{max})";
    }
}