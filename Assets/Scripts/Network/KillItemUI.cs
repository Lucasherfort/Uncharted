using TMPro;
using UnityEngine;

public class KillItemUI : MonoBehaviour
{
    public TMP_Text killerText;
    public TMP_Text killedText;

    public void Setup(string killer, string killed)
    {
        killerText.text = killer;
        killedText.text = killed;
    }
}
