using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillFeedUI : MonoBehaviour
{
    public static KillFeedUI Instance;

    [Header("References")]
    public GameObject killItemPrefab;
    public Transform container;

    [Header("Settings")]
    public float visibleTime = 5f;
    public int maxVisibleItems = 5;

    private Queue<KillItemUI> pool = new Queue<KillItemUI>();
    private List<KillItemUI> activeItems = new List<KillItemUI>();

    void Awake()
    {
        Instance = this;
    }

public void AddKill(string killer, string killed)
{
    // 1. Sécurité : Si trop de kills, on recycle immédiatement le plus ancien
    if (activeItems.Count >= maxVisibleItems)
    {
        KillItemUI oldest = activeItems[0];
        oldest.ForceRelease();
    }

    // 2. Récupération de l'item depuis le Pool
    KillItemUI item = GetItemFromPool();
    
    // 3. PLACEMENT : On l'ajoute au container AVANT toute chose pour que le Layout s'initialise
    item.transform.SetParent(container, false);
    item.gameObject.SetActive(true);
    
    // 4. HIÉRARCHIE : On le force à se mettre tout en haut dans le Vertical Layout Group
    item.transform.SetAsFirstSibling(); 

    // 5. ENREGISTREMENT : On l'ajoute à notre liste de suivi des kills actifs
    activeItems.Add(item);

    // 6. ANIMATION : Maintenant que sa place est sécurisée par Unity, on lance les textes et la glissade
    item.Setup(killer, killed);

    // 7. CHRONO : Lancement du compte à rebours avant la disparition automatique
    StartCoroutine(AutoHide(item));
}

    private KillItemUI GetItemFromPool()
    {
        if (pool.Count > 0) return pool.Dequeue();
        
        GameObject newObj = Instantiate(killItemPrefab, container);
        return newObj.GetComponent<KillItemUI>();
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
        if (activeItems.Contains(item))
        {
            item.Release();
        }
    }
}