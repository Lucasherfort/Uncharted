using UnityEngine;
using UnityEngine.AI;
using Photon.Pun;

public class EnemyController : MonoBehaviourPun
{
    [Header("Target")]
    public Transform player;

    [Header("Attack")]
    public float attackRange = 2f;
    public float attackDamage = 15f;
    public float attackCooldown = 1.5f;
    public float attackDelay = 0.5f;

    [Header("Target Update")]
    public float targetUpdateInterval = 1f;

    private NavMeshAgent agent;
    private Animator animator;

    private PhotonView targetView;

    private float lastAttackTime;
    private bool isAttacking;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();

        agent.updateRotation = true;

        // ❌ seuls les zombies du Master bougent
        if (!PhotonNetwork.IsMasterClient)
        {
            agent.enabled = false;
            return;
        }

        InvokeRepeating(nameof(UpdateTarget), 0f, targetUpdateInterval);
    }

    void Update()
    {
        if (!PhotonNetwork.IsMasterClient)
            return;

        if (player == null || agent == null || !agent.enabled)
            return;

        float distance = Vector3.Distance(transform.position, player.position);

        if (isAttacking)
            return;

        if (distance > attackRange)
        {
            agent.isStopped = false;
            agent.SetDestination(player.position);

            animator.SetFloat("Speed", 1f);
        }
        else
        {
            agent.isStopped = true;

            animator.SetFloat("Speed", 0f);

            LookAtPlayer();
            TryAttack();
        }
    }

    // 🔥 MASTER choisit la cible UNIQUE
    void UpdateTarget()
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        float minDistance = Mathf.Infinity;
        GameObject closest = null;

        foreach (GameObject p in players)
        {
            if (!p.activeInHierarchy) continue;

            float dist = Vector3.Distance(transform.position, p.transform.position);

            if (dist < minDistance)
            {
                minDistance = dist;
                closest = p;
            }
        }

        if (closest == null) return;

        PhotonView pv = closest.GetComponent<PhotonView>();

        if (pv != null)
        {
            player = closest.transform;
            targetView = pv;

            // 📡 SYNC de la cible à tout le monde
            photonView.RPC(nameof(RPC_SetTarget), RpcTarget.All, pv.Owner.ActorNumber);
        }
    }

    // 📡 tout le monde force la même cible
    [PunRPC]
    void RPC_SetTarget(int actorNumber)
    {
        GameObject[] players = GameObject.FindGameObjectsWithTag("Player");

        foreach (var p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();

            if (pv != null && pv.Owner.ActorNumber == actorNumber)
            {
                player = p.transform;
                targetView = pv;
            }
        }
    }

    void TryAttack()
    {
        if (Time.time < lastAttackTime + attackCooldown)
            return;

        lastAttackTime = Time.time;
        isAttacking = true;

        animator.SetTrigger("Attack");

        Invoke(nameof(DealDamage), attackDelay);
        Invoke(nameof(EndAttack), attackCooldown);
    }

    void DealDamage()
    {
        if (player == null || targetView == null)
            return;

        float dist = Vector3.Distance(transform.position, player.position);

        if (dist <= attackRange + 0.5f)
        {
            int targetActor = targetView.Owner.ActorNumber;

            photonView.RPC(nameof(RPC_DealDamage), RpcTarget.All, targetActor, attackDamage);
        }
    }

    // 💥 système de dégâts ULTRA FIABLE
    [PunRPC]
    void RPC_DealDamage(int targetActor, float damage)
    {
        PlayerHealth[] players = FindObjectsOfType<PlayerHealth>();

        foreach (var p in players)
        {
            PhotonView pv = p.GetComponent<PhotonView>();

            if (pv != null && pv.Owner.ActorNumber == targetActor)
            {
                if (pv.IsMine)
                {
                    p.ApplyDamageLocal(damage);
                }
            }
        }
    }

    void EndAttack()
    {
        isAttacking = false;
    }

    void LookAtPlayer()
    {
        if (player == null) return;

        Vector3 dir = player.position - transform.position;
        dir.y = 0;

        if (dir.sqrMagnitude < 0.01f) return;

        Quaternion rot = Quaternion.LookRotation(dir);

        transform.rotation = Quaternion.Slerp(
            transform.rotation,
            rot,
            Time.deltaTime * 10f
        );
    }
}