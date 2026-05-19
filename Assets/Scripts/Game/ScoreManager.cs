using UnityEngine;
using TMPro;
using Photon.Pun;
using Photon.Realtime;

public class ScoreManager : MonoBehaviourPunCallbacks
{
    public static ScoreManager Instance;

    [Header("UI Elements")]
    public TextMeshProUGUI myScoreText;        // Le zéro de gauche
    public TextMeshProUGUI opponentScoreText;  // Le zéro de droite

    void Awake()
    {
        Instance = this;
    }

    void Start()
    {
        UpdateScoreUI();
    }

    // Ce callback de Photon se déclenche AUTOMATIQUEMENT dès qu'un joueur 
    // met à jour ses CustomProperties (comme ses Kills)
    public override void OnPlayerPropertiesUpdate(Player targetPlayer, ExitGames.Client.Photon.Hashtable changedProps)
    {
        if (changedProps.ContainsKey("Kills"))
        {
            UpdateScoreUI();
        }
    }

    public void UpdateScoreUI()
    {
        if (!PhotonNetwork.InRoom) return;

        int myScore = 0;
        int highestOpponentScore = 0;

        // 1. Récupérer mon score
        if (PhotonNetwork.LocalPlayer.CustomProperties.TryGetValue("Kills", out object myKills))
        {
            myScore = (int)myKills;
        }

        // 2. Parcourir les autres joueurs pour trouver le score le plus élevé parmi eux
        foreach (Player p in PhotonNetwork.PlayerList)
        {
            // On ignore le joueur local (nous-mêmes)
            if (p.IsLocal) continue;

            int opponentScore = 0;
            if (p.CustomProperties.TryGetValue("Kills", out object oppKills))
            {
                opponentScore = (int)oppKills;
            }

            // On garde le score le plus élevé trouvé chez un adversaire
            if (opponentScore > highestOpponentScore)
            {
                highestOpponentScore = opponentScore;
            }
        }

        // 3. Application de ta logique d'affichage
        myScoreText.text = myScore.ToString();
        opponentScoreText.text = highestOpponentScore.ToString();

        // Optionnel : Tu peux changer la couleur du texte à droite si l'adversaire te dépasse
        if (highestOpponentScore > myScore)
        {
            opponentScoreText.color = Color.red; // Danger, tu perds
        }
        else if (myScore > highestOpponentScore)
        {
            opponentScoreText.color = Color.gray; // Tu gagnes, le second est loin
        }
        else
        {
            opponentScoreText.color = Color.white; // Égalité
        }
    }
}