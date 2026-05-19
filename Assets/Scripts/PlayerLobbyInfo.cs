using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class PlayerLobbyInfo : MonoBehaviour
{
    public TMP_Text playerNicknameText;
    public Image playerAvatarImage;

    public Sprite defaultAvatar; // assigner un avatar par défaut dans l'inspecteur 

    [Header("Rotation")]
    private float rotationSpeed = 200f;

    private bool isSearching = true;

    void Update()
    {
        if (isSearching && playerAvatarImage != null)
        {
            float zRotation = -rotationSpeed * Time.time;
            playerAvatarImage.transform.rotation = Quaternion.Euler(0f, 0f, zRotation);
        }
    }
    
    public void SetPlayerInfo(string nickname, Sprite avatar)
    {
        isSearching = false;

        playerNicknameText.text = nickname;
        playerAvatarImage.sprite = avatar;

        // reset rotation
        playerAvatarImage.transform.rotation = Quaternion.identity;
    }

    public void SetSearchingState()
    {
        isSearching = true;

        playerAvatarImage.sprite = defaultAvatar;

        playerNicknameText.text = "<color=#666666>Recherche...</color>";

        // reset rotation
        playerAvatarImage.transform.rotation = Quaternion.identity;
    }
}