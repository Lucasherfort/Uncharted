using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;
using Photon.Pun;

[RequireComponent(typeof(Photon.Pun.PhotonView))]
public class DamageEffect : MonoBehaviourPun
{
    [Header("Post Processing")]
    public Volume volume;

    private Vignette vignette;

    [Header("Effect Settings")]
    public float maxIntensity = 0.4f;
    public float fadeSpeed = 2f;

    private float currentIntensity = 0f;

    void Start()
    {
        // 🔥 IMPORTANT : uniquement le joueur local gère les effets visuels
        if (!photonView.IsMine)
        {
            enabled = false;
            return;
        }

        volume = FindObjectOfType<Volume>();

        if (volume == null)
        {
            Debug.LogWarning("Aucun Volume trouvé dans la scène !");
            return;
        }

        if (volume.profile != null && volume.profile.TryGet(out vignette))
        {
            Debug.Log("Vignette récupérée avec succès");
        }
        else
        {
            Debug.LogWarning("Vignette introuvable dans le Volume Profile");
        }
    }

    void Update()
    {
        if (vignette == null)
            return;

        // 🌫 fade progressif vers 0
        currentIntensity = Mathf.Lerp(currentIntensity, 0f, Time.deltaTime * fadeSpeed);

        vignette.intensity.value = currentIntensity;
    }

    // 💥 Appelé quand le joueur LOCAL prend des dégâts
    public void OnDamage()
    {
        if (!photonView.IsMine)
            return;

        currentIntensity = maxIntensity;
    }
}