using UnityEngine;

public class MatchHUDManager : MonoBehaviour
{
    public static MatchHUDManager Instance;

    [Header("HUD Joueur Local")]
    [SerializeField] private LocalPlayerHUD localPlayerHUD;

    public LocalPlayerHUD LocalPlayerHUD => localPlayerHUD;

    private void Awake()
    {
        Instance = this;
    }
}
