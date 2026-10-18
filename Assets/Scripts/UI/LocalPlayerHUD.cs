using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections;

public class LocalPlayerHUD : MonoBehaviour
{
    [Header("Santé")]
    [SerializeField] private TMP_Text healthText;
    [SerializeField] private Image healthBar;

    [Header("Armes & Munitions")]
    [SerializeField] private TMP_Text ammoText;
    [SerializeField] private Image hitmarkerImage;

    private Coroutine hitmarkerCoroutine;

    private void Awake()
    {
        // Par défaut, le hitmarker est blanc et actif
        SetHitmarkerColor(Color.white);
    }

    // --- SANTÉ ---
    public void UpdateHealthUI(float currentHealth, float maxHealth)
    {
        if (healthText != null)
            healthText.text = $"{Mathf.Ceil(currentHealth)} / {maxHealth}";

        if (healthBar != null)
            healthBar.fillAmount = currentHealth / maxHealth;
    }

    // --- MUNITIONS ---
public void UpdateAmmoUI(bool infiniteAmmo, int currentAmmo, int totalAmmo)
{
    if (ammoText != null)
    {
        ammoText.text = infiniteAmmo ? "∞ / ∞" : $"{currentAmmo} / {totalAmmo}";
    }
}

    // --- HITMARKER ---
public void ShowHitmarker(Color hitColor, float duration)
{
    if (hitmarkerImage == null) return;

    // 1. On annule l'éventuel retour au blanc en cours pour ne pas couper net
    CancelInvoke(nameof(ResetHitmarkerColor));

    // 2. On passe en rouge (ou la couleur du hit)
    SetHitmarkerColor(hitColor);

    // 3. On programme le retour au blanc après la durée
    Invoke(nameof(ResetHitmarkerColor), duration);
}

private void ResetHitmarkerColor()
{
    SetHitmarkerColor(Color.white);
}

private void SetHitmarkerColor(Color color)
{
    if (hitmarkerImage == null) return;
    hitmarkerImage.color = color;
}
}