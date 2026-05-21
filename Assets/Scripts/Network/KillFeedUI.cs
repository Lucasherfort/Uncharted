using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillFeedUI : MonoBehaviour
{
    public static KillFeedUI Instance;

    [Header("References")]
    [Tooltip("Le préfabriqué de ton KillItemUI (à créer depuis ta hiérarchie)")]
    public GameObject killItemPrefab;
    [Tooltip("Le panel parent qui possède le Vertical Layout Group")]
    public Transform container;

    [Header("Settings")]
    public float visibleTime = 5f;
    public int maxVisibleItems = 5; // Nombre max de kills affichés en même temps

    // Notre pool d'objets désactivés prêts à être réutilisés
    private Queue<KillItemUI> pool = new Queue<KillItemUI>();
    // La liste des kills actuellement visibles à l'écran
    private List<KillItemUI> activeItems = new List<KillItemUI>();

    void Awake()
    {
        Instance = this;
    }

    public void AddKill(string killer, string killed)
    {
        // 1. Sécurité : Si l'écran est saturé, on force le plus ancien à s'en aller immédiatement
        if (activeItems.Count >= maxVisibleItems)
        {
            KillItemUI oldest = activeItems[0];
            oldest.ForceRelease(); // Coupe son animation et le remet dans le pool
        }

        // 2. Récupération ou création d'un item
        KillItemUI item = GetItemFromPool();
        
        // 3. Configuration et positionnement
        item.transform.SetParent(container, false);
        item.gameObject.SetActive(true);
        item.Setup(killer, killed);
        
        // On le force à se mettre tout en bas du Vertical Layout Group
        item.transform.SetAsFirstSibling(); // Envoie tout en haut

        activeItems.Add(item);

        // 4. Lancement du décompte avant disparition
        StartCoroutine(AutoHide(item));
    }

    private KillItemUI GetItemFromPool()
    {
        if (pool.Count > 0)
        {
            return pool.Dequeue();
        }
        else
        {
            // Si le pool est vide, on instancie un nouveau préfabriqué en local (PAS de PhotonNetwork)
            GameObject newObj = Instantiate(killItemPrefab, container);
            return newObj.GetComponent<KillItemUI>();
        }
    }

    public void ReturnToPool(KillItemUI item)
    {
        if (activeItems.Contains(item))
        {
            activeItems.Remove(item);
        }

        item.gameObject.SetActive(false);
        pool.Enqueue(item);
    }

    IEnumerator AutoHide(KillItemUI item)
    {
        yield return new WaitForSeconds(visibleTime);

        // On vérifie que l'item n'a pas déjà été recyclé de force entre temps
        if (activeItems.Contains(item))
        {
            item.Release(); // Lance l'animation de sortie
        }
    }
}