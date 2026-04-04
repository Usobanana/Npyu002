using UnityEngine;
using UnityEngine.AI;
using ActionGame.AI;

namespace ActionGame
{
    /// <summary>
    /// Behavior Tree ベースの Enemy AI。
    ///
    /// BT 構造:
    ///   Selector (Root)
    ///   ├─ Sequence [攻撃]
    ///   │    ├─ IsInAttackRange (Condition)
    ///   │    └─ Attack (Action)
    ///   ├─ Sequence [追跡]
    ///   │    ├─ IsInDetectionRange (Condition)
    ///   │    └─ Chase (Action)
    ///   └─ Patrol (Action / Idle)
    ///
    /// 必要コンポーネント: NavMeshAgent, Health
    /// NavMesh は事前にベイクが必要 (Window > AI > Navigation > Bake)
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyBT : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] float detectionRange = 12f;
        [SerializeField] float attackRange = 2f;

        [Header("Combat")]
        [SerializeField] float attackDamage = 10f;
        [SerializeField] float attackCooldown = 1.5f;

        [Header("Patrol")]
        [SerializeField] Transform[] patrolPoints;
        [SerializeField] float patrolWaitTime = 2f;

        Transform player;
        NavMeshAgent agent;
        Health health;
        BTNode root;
        BTBlackboard bb;

        void Start()
        {
            agent  = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();

            var playerGO = GameObject.FindGameObjectWithTag("Player");
            player = playerGO != null ? playerGO.transform : null;

            bb = new BTBlackboard();
            root = BuildTree();

            health.OnDeath += OnDeath;
        }

        void Update()
        {
            if (!health.IsAlive || player == null) return;
            root.Evaluate();
        }

        BTNode BuildTree()
        {
            var root = new BTSelector();

            // --- 攻撃 Sequence ---
            var attackSeq = new BTSequence();
            attackSeq.AddChild(new IsInRange(this, attackRange));
            attackSeq.AddChild(new AttackPlayer(this));

            // --- 追跡 Sequence ---
            var chaseSeq = new BTSequence();
            chaseSeq.AddChild(new IsInRange(this, detectionRange));
            chaseSeq.AddChild(new ChasePlayer(this));

            // --- パトロール / 待機 ---
            var patrol = new Patrol(this);

            root.AddChild(attackSeq);
            root.AddChild(chaseSeq);
            root.AddChild(patrol);

            return root;
        }

        void OnDeath()
        {
            agent.isStopped = true;
            agent.enabled = false;
            Debug.Log("[Enemy] Defeated!");
            // 少し待って非表示（視覚フィードバック）
            Invoke(nameof(DisableSelf), 1.5f);
        }

        void DisableSelf() => gameObject.SetActive(false);

        void OnDrawGizmosSelected()
        {
            Gizmos.color = Color.yellow;
            Gizmos.DrawWireSphere(transform.position, detectionRange);
            Gizmos.color = Color.red;
            Gizmos.DrawWireSphere(transform.position, attackRange);
        }

        // =====================================================================
        // BT Nodes (private inner classes)
        // =====================================================================

        /// <summary>Player との距離チェック</summary>
        class IsInRange : BTNode
        {
            readonly EnemyBT self;
            readonly float range;
            public IsInRange(EnemyBT e, float r) { self = e; range = r; }

            public override NodeState Evaluate()
            {
                if (self.player == null) return State = NodeState.Failure;
                float dist = Vector3.Distance(self.transform.position, self.player.position);
                return State = dist <= range ? NodeState.Success : NodeState.Failure;
            }
        }

        /// <summary>Player を追跡する</summary>
        class ChasePlayer : BTNode
        {
            readonly EnemyBT self;
            public ChasePlayer(EnemyBT e) { self = e; }

            public override NodeState Evaluate()
            {
                if (!self.agent.enabled) return State = NodeState.Failure;
                self.agent.isStopped = false;
                self.agent.SetDestination(self.player.position);
                return State = NodeState.Running;
            }
        }

        /// <summary>停止して Player を攻撃する</summary>
        class AttackPlayer : BTNode
        {
            readonly EnemyBT self;
            public AttackPlayer(EnemyBT e) { self = e; }

            public override NodeState Evaluate()
            {
                if (!self.agent.enabled) return State = NodeState.Failure;
                self.agent.isStopped = true;
                self.transform.LookAt(self.player);

                float nextAttack = self.bb.Get<float>("nextAttackTime", 0f);
                if (Time.time >= nextAttack)
                {
                    self.bb.Set("nextAttackTime", Time.time + self.attackCooldown);
                    var playerHP = self.player.GetComponent<Health>();
                    playerHP?.TakeDamage(self.attackDamage);
                    Debug.Log($"[Enemy] Attack! {self.attackDamage} damage");
                }
                return State = NodeState.Running;
            }
        }

        /// <summary>パトロールポイント間を巡回する。未設定の場合は Idle。</summary>
        class Patrol : BTNode
        {
            readonly EnemyBT self;
            bool initialized;

            public Patrol(EnemyBT e) { self = e; }

            public override NodeState Evaluate()
            {
                if (!self.agent.enabled) return State = NodeState.Running;

                // パトロールポイントなし → Idle
                if (self.patrolPoints == null || self.patrolPoints.Length == 0)
                {
                    self.agent.isStopped = true;
                    return State = NodeState.Running;
                }

                // 初期化: 最初のポイントへ出発
                if (!initialized)
                {
                    int idx = self.bb.Get("patrolIndex", 0);
                    self.agent.SetDestination(self.patrolPoints[idx].position);
                    self.agent.isStopped = false;
                    initialized = true;
                }

                // 到着チェック
                if (!self.agent.pathPending && self.agent.hasPath
                    && self.agent.remainingDistance < 0.5f)
                {
                    float waitUntil = self.bb.Get<float>("patrolWaitUntil", 0f);

                    if (waitUntil == 0f)
                    {
                        // 待機開始
                        self.agent.isStopped = true;
                        self.bb.Set("patrolWaitUntil", Time.time + self.patrolWaitTime);
                    }
                    else if (Time.time >= waitUntil)
                    {
                        // 次のポイントへ
                        int idx = (self.bb.Get("patrolIndex", 0) + 1) % self.patrolPoints.Length;
                        self.bb.Set("patrolIndex", idx);
                        self.bb.Set("patrolWaitUntil", 0f);
                        self.agent.SetDestination(self.patrolPoints[idx].position);
                        self.agent.isStopped = false;
                    }
                }

                return State = NodeState.Running;
            }
        }
    }
}
