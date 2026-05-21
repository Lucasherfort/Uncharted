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
    private RectTransform rectTransform;

    [Header("Animation Settings")]
    public float animationDuration = 0.25f;
    public float startXOffset = -150f;

    private Vector2 targetAnchoredPosition;
    private Coroutine currentAnimCoroutine;

    void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null) canvasGroup = gameObject.AddComponent<CanvasGroup>();
        
        targetAnchoredPosition = rectTransform.anchoredPosition;
    }

    public void Setup(string killer, string killed)
    {
        string myName = PhotonNetwork.LocalPlayer.NickName;
        killerText.text = killer;
        killedText.text = killed;

        // Couleurs Vert/Rouge selon ta logique stricte
        killerText.color = (killer == myName) ? Color.green : Color.red;
        killedText.color = (killed == myName) ? Color.green : Color.red;

        if (currentAnimCoroutine != null) StopCoroutine(currentAnimCoroutine);
        currentAnimCoroutine = StartCoroutine(AnimateIn());
    }

    private IEnumerator AnimateIn()
    {
        float elapsedTime = 0f;
        Vector2 startPosition = new Vector2(targetAnchoredPosition.x + startXOffset, targetAnchoredPosition.y);
        rectTransform.anchoredPosition = startPosition;
        canvasGroup.alpha = 0f;

        while (elapsedTime < animationDuration)
        {
            elapsedTime += Time.deltaTime;
            float t = elapsedTime / animationDuration;
            t = t * t * (3f - 2f * t); // SmoothStep

            rectTransform.anchoredPosition = Vector2.Lerp(startPosition, targetAnchoredPosition, t);
            canvasGroup.alpha = Mathf.Lerp(0f, 1f, t);
            yield return null;
        }

        rectTransform.anchoredPosition = targetAnchoredPosition;
        canvasGroup.alpha = 1f;
    }

    public void Release()
    {
        if (currentAnimCoroutine != null) StopCoroutine(currentAnimCoroutine);
        currentAnimCoroutine = StartCoroutine(AnimateOut());
    }

    // Utilisé pour recycler instantanément sans attendre le fondu si l'écran est inondé
    public void ForceRelease()
    {
        if (currentAnimCoroutine != null) StopCoroutine(currentAnimCoroutine);
        KillFeedUI.Instance.ReturnToPool(this);
    }

    private IEnumerator AnimateOut()
    {
        float elapsedTime = 0f;
        float startAlpha = canvasGroup.alpha;

        while (elapsedTime < 0.15f)
        {
            elapsedTime += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(startAlpha, 0f, elapsedTime / 0.15f);
            yield return null;
        }

        // Au lieu de juste faire un SetActive(false), on se renvoie dans le pool général
        KillFeedUI.Instance.ReturnToPool(this);
    }
}