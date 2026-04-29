using UnityEngine;
using System.Collections;
using TMPro;
using Photon.Pun;

public class WaveManager : MonoBehaviourPun
{
    [Header("Enemy")]
    public GameObject zombiePrefab;

    [Header("Spawn Points")]
    public Transform[] spawnPoints;

    [Header("Waves")]
    public int baseZombieCount = 5;
    public float timeBetweenWaves = 5f;

    [Header("UI")]
    public TMP_Text waveText;
    public TMP_Text zombiesLeftText;

    private int currentWave = 0;
    private int aliveZombies = 0;
    private bool waveInProgress = false;

    // 🔥 Lancement initial (appelé depuis ton Launcher)
    public void StartFirstWave()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (!waveInProgress)
        {
            StartCoroutine(StartNextWave());
        }
    }

    IEnumerator StartNextWave()
    {
        yield return new WaitForSeconds(timeBetweenWaves);

        currentWave++;

        int zombieCount = baseZombieCount + (currentWave * 2);

        aliveZombies = 0;
        waveInProgress = true;

        Debug.Log($"Wave {currentWave} start avec {zombieCount} zombies");

        // 📡 Sync UI
        photonView.RPC(nameof(RPC_UpdateWaveUI), RpcTarget.All, currentWave);
        photonView.RPC(nameof(RPC_UpdateZombieCount), RpcTarget.All, zombieCount);

        for (int i = 0; i < zombieCount; i++)
        {
            SpawnZombie();
            yield return new WaitForSeconds(0.4f);
        }
    }

    void SpawnZombie()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        Transform spawnPoint = spawnPoints[Random.Range(0, spawnPoints.Length)];

        GameObject zombie = PhotonNetwork.Instantiate(
            zombiePrefab.name,
            spawnPoint.position,
            spawnPoint.rotation
        );

        // 🎯 Target
        if (zombie.TryGetComponent<EnemyController>(out var ai))
        {
            ai.player = FindClosestPlayer();
        }

        aliveZombies++;

        // 💀 abonnement mort
        if (zombie.TryGetComponent<ZombieHealth>(out var zh))
        {
            zh.onDeath += OnZombieKilled;
        }
    }

    void OnZombieKilled()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        aliveZombies--;

        Debug.Log("Zombie tué, restant: " + aliveZombies);

        // 📡 Sync UI
        photonView.RPC(nameof(RPC_UpdateZombieCount), RpcTarget.All, aliveZombies);

        if (waveInProgress && aliveZombies <= 0)
        {
            waveInProgress = false;
            StartCoroutine(StartNextWave());
        }
    }

    // 🔍 Trouver le joueur le plus proche
    private Transform FindClosestPlayer()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        Transform closest = null;
        float minDistance = Mathf.Infinity;

        Vector3 currentPosition = transform.position;

        foreach (GameObject player in players)
        {
            if (!player.activeInHierarchy) continue;

            float distance = Vector3.Distance(currentPosition, player.transform.position);

            if (distance < minDistance)
            {
                minDistance = distance;
                closest = player.transform;
            }
        }

        return closest;
    }

    // 📡 UI SYNC

    [PunRPC]
    void RPC_UpdateWaveUI(int wave)
    {
        if (waveText != null)
            waveText.text = "Wave " + wave;
    }

    [PunRPC]
    void RPC_UpdateZombieCount(int count)
    {
        if (zombiesLeftText != null)
            zombiesLeftText.text = "Zombies: " + count;
    }
}