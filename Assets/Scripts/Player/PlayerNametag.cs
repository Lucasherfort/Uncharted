using UnityEngine;
using TMPro; // Enlève cette ligne et utilise 'using UnityEngine.UI;' si tu utilises le Text classique
using Photon.Pun;

public class PlayerNametag : MonoBehaviourPun
{
    [Header("References")]
    [SerializeField] private TMP_Text nameText; // Glisse ton composant Text/TMP ici dans l'inspecteur

    private Transform mainCameraTransform;

    void Start()
    {
        // 1. On récupère la caméra principale pour que le texte la regarde
        if (Camera.main != null)
        {
            mainCameraTransform = Camera.main.transform;
        }

        // 2. Récupération et affichage du pseudo via Photon
        if (photonView != null && photonView.Owner != null)
        {
            // NickName est le pseudo configuré par le joueur lors de sa connexion à Photon
            nameText.text = photonView.Owner.NickName;
        }
        else
        {
            nameText.text = "Local Player";
        }

        // Optionnel : Si tu veux cacher ton propre pseudo pour ne pas qu'il te gâche la vue
        if (photonView.IsMine)
        {
            // On désactive le texte ou le canvas entier uniquement pour le joueur local
            nameText.gameObject.SetActive(false); 
        }
    }

    void LateUpdate()
    {
        // 3. Le texte doit toujours faire face à la caméra (Billboarding)
        if (mainCameraTransform != null)
        {
            // LookAt fait tourner le texte vers la caméra
            transform.LookAt(transform.position + mainCameraTransform.forward);
        }
        else
        {
            // Sécurité au cas où la caméra change/respawn pendant la partie
            if (Camera.main != null) mainCameraTransform = Camera.main.transform;
        }
    }
}