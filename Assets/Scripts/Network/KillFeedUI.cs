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
        // 1. Si trop de kills, on dégage le plus ancien (qui est maintenant en bas de la liste active)
        if (activeItems.Count >= maxVisibleItems)
        {
            KillItemUI oldest = activeItems[0];
            oldest.ForceRelease();
        }

        // 2. Récupération depuis le Pool
        KillItemUI item = GetItemFromPool();
        
        // 3. Activation et placement
        item.transform.SetParent(container, false);
        item.gameObject.SetActive(true);
        item.Setup(killer, killed);
        
        // 🔥 L'ASTUCE ICI : On le force à être le PREMIER enfant. 
        // Le Vertical Layout Group le placera donc automatiquement tout en haut.
        item.transform.SetAsFirstSibling(); 

        activeItems.Add(item);

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