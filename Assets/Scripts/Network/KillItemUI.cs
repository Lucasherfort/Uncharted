using UnityEngine;
using System.Collections;
using TMPro;
using Photon.Pun;

public class KillItemUI : MonoBehaviour
{
    [Header("Components")]
    public TMP_Text killerText;
    public TMP_Text killedText;
    
    private CanvasGroup canvasGroup;

    void Awake()
    {
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
    }

    public void Setup(string killer, string killed)
    {
        string myName = PhotonNetwork.LocalPlayer.NickName;
        killerText.text = killer;
        killedText.text = killed;

        // Vos couleurs strictes
        killerText.color = (killer == myName) ? Color.green : Color.red;
        killedText.color = (killed == myName) ? Color.green : Color.red;

        // Lance le fondu d'apparition
        StartCoroutine(FadeIn());
    }

    private IEnumerator FadeIn()
    {
        float elapsedTime = 0f;
        canvasGroup.alpha = 0f;

        while (elapsedTime < 0.2f)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsedTime / 0.2f);
            yield return null;
        }
        canvasGroup.alpha = 1f;
    }

    public void Release()
    {
        StartCoroutine(FadeOut());
    }

    public void ForceRelease()
    {
        KillFeedUI.Instance.ReturnToPool(this);
    }

    private IEnumerator FadeOut()
    {
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsedTime < 0.15f)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / 0.15f);
            yield return null;
        }

        KillFeedUI.Instance.ReturnToPool(this);
    }
}