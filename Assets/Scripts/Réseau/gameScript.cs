using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using TMPro;

public class gameScript : MonoBehaviour
{
    public TMP_Text TxtRoom;
    public GameObject playerPrefab;

    private void Start()
    {
        TxtRoom.text = "Room: " + Photon.Pun.PhotonNetwork.CurrentRoom.Name;
        Photon.Pun.PhotonNetwork.Instantiate(playerPrefab.name, Vector3.zero, Quaternion.identity, 0);
    }
}
