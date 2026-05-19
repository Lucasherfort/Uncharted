using TMPro;
using UnityEngine;
using Photon.Pun;

public class KillItemUI : MonoBehaviour
{
    public TMP_Text killerText;
    public TMP_Text killedText;

    public bool isBusy;

    public void Setup(string killer, string killed)
    {
        isBusy = true;

        string local = PhotonNetwork.LocalPlayer.NickName;

        killerText.text = killer;
        killedText.text = killed;

        killerText.color = (killer == local) ? Color.green : Color.red;
        killedText.color = (killed == local) ? Color.green : Color.red;
    }

    public void Release()
    {
        isBusy = false;
        gameObject.SetActive(false);
    }
}