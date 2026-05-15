using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class KillFeedUI : MonoBehaviour
{
    public static KillFeedUI Instance;

    [Header("References")]
    public Transform container;
    public KillItemUI[] items;

    [Header("Settings")]
    public float visibleTime = 6f;

    private Queue<KillItemUI> availableItems = new Queue<KillItemUI>();

    void Awake()
    {
        Instance = this;

        foreach (var item in items)
        {
            item.gameObject.SetActive(false);
            availableItems.Enqueue(item);
        }
    }

    public void AddKill(string killer, string killed)
    {
        StartCoroutine(ShowKillRoutine(killer, killed));
    }

    IEnumerator ShowKillRoutine(string killer, string killed)
    {
        KillItemUI item;

        // Si plus de place → recycle le plus ancien
        if (availableItems.Count > 0)
        {
            item = availableItems.Dequeue();
        }
        else
        {
            item = container.GetChild(0).GetComponent<KillItemUI>();
            StopAllCoroutines(); // optionnel
        }

        item.gameObject.SetActive(true);

        item.Setup(killer, killed);

        // Met le nouveau en bas
        item.transform.SetAsLastSibling();

        yield return new WaitForSeconds(visibleTime);

        item.gameObject.SetActive(false);

        availableItems.Enqueue(item);
    }
}