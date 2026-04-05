using UnityEngine;
using UnityEngine.AI;
using System.Collections;
using ActionGame.AI;

namespace ActionGame
{
    /// <summary>
    /// Inspector で EnemyType を選ぶだけで AI の性格が変わる Behavior Tree 敵 AI。
    ///
    ///   Grunt     : 標準。検知→追跡→攻撃→巡回。被弾で短い硬直あり。
    ///   Aggressive: 即突進。攻撃クールダウン短め。硬直ほぼなし。
    ///   Cautious  : 距離をキープ。攻撃後に後退。ストレイフで横移動。
    ///
    /// BT 共通構造:
    ///   Root Selector
    ///   ├─ [スタッガー]  被弾硬直タイマー > 0 → 停止待機
    ///   ├─ [攻撃]       攻撃範囲内 → 向き直し → 攻撃
    ///   ├─ [移動]       型ごとに異なる
    ///   └─ [待機/巡回]
    /// </summary>
    public enum EnemyType { Grunt, Aggressive, Cautious }

    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyBT : MonoBehaviour
    {
        [Header("AI Type")]
        [SerializeField] EnemyType enemyType = EnemyType.Grunt;

        [Header("Detection")]
        [SerializeField] float detectionRange    = 12f;
        [SerializeField] float attackRange       = 2f;
        [SerializeField] float preferredDist     = 4f;   // Cautious: 維持したい距離

        [Header("Combat")]
        [SerializeField] float attackDamage      = 10f;
        [SerializeField] float attackCooldown    = 1.5f;
        [SerializeField] float attackHitDelay    = 0.4f;
        [SerializeField] float hitStaggerDuration = 0.5f; // 被弾時の硬直秒数

        [Header("Movement")]
        [SerializeField] float chaseSpeed        = 4f;
        [SerializeField] float strafeSpeed       = 2.5f; // Cautious: ストレイフ速度
        [SerializeField] float retreatDuration   = 0.8f; // Cautious: 攻撃後に後退する時間

        [Header("Patrol")]
        [SerializeField] Transform[] patrolPoints;
        [SerializeField] float patrolWaitTime    = 2f;

        [Header("Score")]
        [SerializeField] int scoreOnDeath        = 100;

        /// <summary>false のとき全エネミーの AI を停止する（デバッグ用）</summary>
        public static bool AIEnabled = true;

        // ── 内部状態 ──────────────────────────────────────────────────
        Transform                player;
        NavMeshAgent             agent;
        Health                   health;
        EnemyAnimationController animCtrl;
        BTNode                   root;
        BTBlackboard             bb;

        float staggerTimer = 0f;
        bool  IsStaggered  => staggerTimer > 0f;

        // ─────────────────────────────────────────────────────────────
        void Start()
        {
            agent    = GetComponent<NavMeshAgent>();
            health   = GetComponent<Health>();
            animCtrl = GetComponent<EnemyAnimationController>();

            // null パトロールポイントを除去してタグで補完
            patrolPoints = System.Array.FindAll(
                patrolPoints ?? new Transform[0], p => p != null);
            if (patrolPoints.Length == 0)
            {
                var pts = GameObject.FindGameObjectsWithTag("PatrolPoint");
                patrolPoints = System.Array.ConvertAll(pts, g => g.transform);
            }

            var playerGO = GameObject.FindGameObjectWithTag("Player");
            player = playerGO != null ? playerGO.transform : null;

            // Aggressive: 初速・クールダウンを調整
            if (enemyType == EnemyType.Aggressive)
            {
                attackCooldown     = Mathf.Max(0.6f, attackCooldown * 0.6f);
                hitStaggerDuration = 0.1f;
                chaseSpeed        *= 1.4f;
            }

            bb   = new BTBlackboard();
            root = BuildTree();

            health.OnDeath         += OnDeath;
            health.OnHealthChanged += OnHealthChanged;
        }

        void Update()
        {
            if (!health.IsAlive || player == null) return;

            if (staggerTimer > 0f) staggerTimer -= Time.deltaTime;

            // ノックバック中など agent が無効な間はスキップ
            if (!agent.enabled || !agent.isOnNavMesh) return;

            if (!AIEnabled)
            {
                agent.isStopped = true;
                return;
            }

            agent.isStopped = false;
            root.Evaluate();
        }

        // ── ツリー構築 ────────────────────────────────────────────────
        BTNode BuildTree()
        {
            return enemyType switch
            {
                EnemyType.Aggressive => BuildAggressiveTree(),
                EnemyType.Cautious   => BuildCautiousTree(),
                _                    => BuildGruntTree(),
            };
        }

        BTNode BuildGruntTree()
        {
            var root = new BTSelector();
            root.AddChild(StaggerSequence());

            var attackSeq = new BTSequence();
            attackSeq.AddChild(new BTCondition(() => DistToPlayer() <= attackRange));
            attackSeq.AddChild(new BTAction(FaceAndAttack));
            root.AddChild(attackSeq);

            var chaseSeq = new BTSequence();
            chaseSeq.AddChild(new BTCondition(() => DistToPlayer() <= detectionRange));
            chaseSeq.AddChild(new BTAction(() => Chase(chaseSpeed)));
            root.AddChild(chaseSeq);

            root.AddChild(new BTAction(Patrol));
            return root;
        }

        BTNode BuildAggressiveTree()
        {
            var root = new BTSelector();
            root.AddChild(StaggerSequence());

            // 攻撃範囲を少し広めに設定
            var attackSeq = new BTSequence();
            attackSeq.AddChild(new BTCondition(() => DistToPlayer() <= attackRange * 1.2f));
            attackSeq.AddChild(new BTAction(FaceAndAttack));
            root.AddChild(attackSeq);

            // 検知範囲を無視して常に全速追跡
            root.AddChild(new BTAction(() => Chase(chaseSpeed)));
            return root;
        }

        BTNode BuildCautiousTree()
        {
            var root = new BTSelector();
            root.AddChild(StaggerSequence());

            // 攻撃後の後退タイマーが残っている間は後退
            var retreatAfterAtk = new BTSequence();
            retreatAfterAtk.AddChild(new BTCondition(() => Time.time < bb.Get<float>("retreatUntil", 0f)));
            retreatAfterAtk.AddChild(new BTAction(Retreat));
            root.AddChild(retreatAfterAtk);

            // 近すぎる → 後退
            var tooClose = new BTSequence();
            tooClose.AddChild(new BTCondition(() => DistToPlayer() < preferredDist * 0.6f));
            tooClose.AddChild(new BTAction(Retreat));
            root.AddChild(tooClose);

            // 攻撃圏内 → 攻撃（成功後 retreatUntil セット）
            var attackSeq = new BTSequence();
            attackSeq.AddChild(new BTCondition(() => DistToPlayer() <= attackRange));
            attackSeq.AddChild(new BTAction(FaceAndAttackCautious));
            root.AddChild(attackSeq);

            // 検知圏内 → 適正距離まで追跡 or ストレイフ
            var detectSeq = new BTSequence();
            detectSeq.AddChild(new BTCondition(() => DistToPlayer() <= detectionRange));
            detectSeq.AddChild(new BTAction(ChaseOrStrafe));
            root.AddChild(detectSeq);

            root.AddChild(new BTAction(Patrol));
            return root;
        }

        // ── 共通ノード ────────────────────────────────────────────────

        BTNode StaggerSequence()
        {
            var seq = new BTSequence();
            seq.AddChild(new BTCondition(() => IsStaggered));
            seq.AddChild(new BTAction(() =>
            {
                if (agent.enabled && agent.isOnNavMesh)
                    agent.isStopped = true;
                return NodeState.Running;
            }));
            return seq;
        }

        // ── アクション実装 ────────────────────────────────────────────

        float DistToPlayer() =>
            player != null ? Vector3.Distance(transform.position, player.position) : float.MaxValue;

        void FacePlayer()
        {
            if (player == null) return;
            transform.LookAt(new Vector3(player.position.x, transform.position.y, player.position.z));
        }

        NodeState FaceAndAttack()
        {
            agent.isStopped = true;
            FacePlayer();
            if (Time.time >= bb.Get<float>("nextAttack", 0f))
            {
                bb.Set("nextAttack", Time.time + attackCooldown);
                animCtrl?.TriggerAttack();
                StartCoroutine(ApplyAttackDamageDelayed(attackHitDelay));
            }
            return NodeState.Running;
        }

        NodeState FaceAndAttackCautious()
        {
            agent.isStopped = true;
            FacePlayer();
            if (Time.time >= bb.Get<float>("nextAttack", 0f))
            {
                bb.Set("nextAttack",   Time.time + attackCooldown);
                bb.Set("retreatUntil", Time.time + retreatDuration);
                animCtrl?.TriggerAttack();
                StartCoroutine(ApplyAttackDamageDelayed(attackHitDelay));
            }
            return NodeState.Running;
        }

        NodeState Chase(float speed)
        {
            if (!agent.enabled) return NodeState.Failure;
            agent.isStopped = false;
            agent.speed     = speed;
            agent.SetDestination(player.position);
            return NodeState.Running;
        }

        NodeState Retreat()
        {
            if (!agent.enabled || player == null) return NodeState.Failure;
            var away = (transform.position - player.position).normalized;
            var dest = transform.position + away * 3f;
            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 3f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.speed     = chaseSpeed;
                agent.SetDestination(hit.position);
            }
            return NodeState.Running;
        }

        NodeState ChaseOrStrafe()
        {
            // 適正距離より遠ければ追跡、近ければストレイフ
            return DistToPlayer() > preferredDist + 1.5f ? Chase(chaseSpeed) : Strafe();
        }

        NodeState Strafe()
        {
            if (!agent.enabled || player == null) return NodeState.Failure;

            // 一定時間ごとにストレイフ方向を反転
            if (Time.time >= bb.Get<float>("strafeSwitch", 0f))
            {
                float dir = bb.Get("strafeDir", 1f) * -1f;
                bb.Set("strafeDir",    dir);
                bb.Set("strafeSwitch", Time.time + Random.Range(1.5f, 3f));
            }

            float strafeDir = bb.Get("strafeDir", 1f);
            var   toPlayer  = (player.position - transform.position).normalized;
            var   perp      = new Vector3(-toPlayer.z, 0f, toPlayer.x) * strafeDir;
            var   dest      = transform.position + perp * 2f;

            if (NavMesh.SamplePosition(dest, out NavMeshHit hit, 2f, NavMesh.AllAreas))
            {
                agent.isStopped = false;
                agent.speed     = strafeSpeed;
                agent.SetDestination(hit.position);
            }
            FacePlayer(); // ストレイフ中もプレイヤーを向く
            return NodeState.Running;
        }

        NodeState Patrol()
        {
            if (!agent.enabled) return NodeState.Running;

            if (patrolPoints == null || patrolPoints.Length == 0)
            {
                agent.isStopped = true;
                return NodeState.Running;
            }

            if (!bb.Has("patrolInit"))
            {
                bb.Set("patrolInit", true);
                agent.speed     = chaseSpeed * 0.6f;
                agent.isStopped = false;
                agent.SetDestination(patrolPoints[0].position);
            }

            if (!agent.pathPending && agent.hasPath && agent.remainingDistance < 0.5f)
            {
                float waitUntil = bb.Get<float>("patrolWait", 0f);
                if (waitUntil == 0f)
                {
                    agent.isStopped = true;
                    bb.Set("patrolWait", Time.time + patrolWaitTime);
                }
                else if (Time.time >= waitUntil)
                {
                    int idx = (bb.Get("patrolIdx", 0) + 1) % patrolPoints.Length;
                    bb.Set("patrolIdx",  idx);
                    bb.Set("patrolWait", 0f);
                    agent.isStopped = false;
                    agent.SetDestination(patrolPoints[idx].position);
                }
            }
            return NodeState.Running;
        }

        // ── ダメージ・死亡 ────────────────────────────────────────────

        IEnumerator ApplyAttackDamageDelayed(float delay)
        {
            yield return new WaitForSeconds(delay);
            if (player == null || !health.IsAlive) yield break;
            var hp = player.GetComponent<Health>();
            if (hp != null && hp.IsAlive)
            {
                hp.TakeDamage(attackDamage);
                AudioManager.Instance?.PlayHit();
                EffectManager.Instance?.SpawnHit(player.position);
            }
        }

        void OnHealthChanged(float cur, float max)
        {
            if (cur < max && cur > 0f)
                staggerTimer = hitStaggerDuration; // 被弾硬直
        }

        void OnDeath()
        {
            if (agent.enabled && agent.isOnNavMesh)
                agent.isStopped = true;
            agent.enabled = false;
            AudioManager.Instance?.PlayEnemyDeath();
            ScoreManager.Instance?.AddScore(scoreOnDeath);
            EffectManager.Instance?.SpawnEnemyDeath(transform.position);
            Debug.Log($"[Enemy:{enemyType}] Defeated! +{scoreOnDeath}pts");
            Invoke(nameof(DisableSelf), 1.5f);
        }

        void DisableSelf() => gameObject.SetActive(false);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
            if (enemyType == EnemyType.Cautious)
            {
                Gizmos.color = new Color(0f, 1f, 1f, 0.4f);
                Gizmos.DrawWireSphere(transform.position, preferredDist);
            }
        }
    }
}
