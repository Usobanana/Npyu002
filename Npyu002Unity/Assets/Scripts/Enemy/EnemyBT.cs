using UnityEngine;
using UnityEngine.AI;
using ActionGame.AI;

namespace ActionGame
{
    /// <summary>
    /// Behavior Tree ベースの Enemy AI。
    /// 撃破時に AudioManager で SE 再生、ScoreManager でスコア加算。
    ///
    /// BT 構造:
    ///   Selector (Root)
    ///   ├─ Sequence [攻撃]  IsInAttackRange → AttackPlayer
    ///   ├─ Sequence [追跡]  IsInDetectionRange → ChasePlayer
    ///   └─ Patrol / Idle
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyBT : MonoBehaviour
    {
        [Header("Detection")]
        [SerializeField] float detectionRange = 12f;
        [SerializeField] float attackRange    = 2f;

        [Header("Combat")]
        [SerializeField] float attackDamage  = 10f;
        [SerializeField] float attackCooldown = 1.5f;

        [Header("Patrol")]
        [SerializeField] Transform[] patrolPoints;
        [SerializeField] float patrolWaitTime = 2f;

        [Header("Score")]
        [SerializeField] int scoreOnDeath = 100;

        Transform player;
        NavMeshAgent agent;
        Health health;
        BTNode root;
        BTBlackboard bb;

        void Start()
        {
            agent  = GetComponent<NavMeshAgent>();
            health = GetComponent<Health>();

            // Prefab 由来の missing 参照を除去してからタグで補完
            patrolPoints = System.Array.FindAll(
                patrolPoints ?? new Transform[0],
                p => p != null && p.gameObject != null);

            if (patrolPoints.Length == 0)
            {
                var pts = GameObject.FindGameObjectsWithTag("PatrolPoint");
                patrolPoints = System.Array.ConvertAll(pts, g => g.transform);
            }

            var playerGO = GameObject.FindGameObjectWithTag("Player");
            player = playerGO != null ? playerGO.transform : null;

            bb   = new BTBlackboard();
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

            var attackSeq = new BTSequence();
            attackSeq.AddChild(new IsInRange(this, attackRange));
            attackSeq.AddChild(new AttackPlayer(this));

            var chaseSeq = new BTSequence();
            chaseSeq.AddChild(new IsInRange(this, detectionRange));
            chaseSeq.AddChild(new ChasePlayer(this));

            root.AddChild(attackSeq);
            root.AddChild(chaseSeq);
            root.AddChild(new Patrol(this));

            return root;
        }

        void OnDeath()
        {
            agent.isStopped = true;
            agent.enabled   = false;

            AudioManager.Instance?.PlayEnemyDeath();
            ScoreManager.Instance?.AddScore(scoreOnDeath);
            EffectManager.Instance?.SpawnEnemyDeath(transform.position);

            Debug.Log($"[Enemy] Defeated! +{scoreOnDeath}pts");
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
        // BT Nodes
        // =====================================================================

        class IsInRange : BTNode
        {
            readonly EnemyBT self; readonly float range;
            public IsInRange(EnemyBT e, float r) { self = e; range = r; }
            public override NodeState Evaluate()
            {
                if (self.player == null) return State = NodeState.Failure;
                return State = Vector3.Distance(self.transform.position, self.player.position) <= range
                    ? NodeState.Success : NodeState.Failure;
            }
        }

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

        class AttackPlayer : BTNode
        {
            readonly EnemyBT self;
            public AttackPlayer(EnemyBT e) { self = e; }
            public override NodeState Evaluate()
            {
                if (!self.agent.enabled) return State = NodeState.Failure;
                self.agent.isStopped = true;
                self.transform.LookAt(self.player);

                if (Time.time >= self.bb.Get<float>("nextAttack", 0f))
                {
                    self.bb.Set("nextAttack", Time.time + self.attackCooldown);
                    var hp = self.player.GetComponent<Health>();
                    if (hp != null && hp.IsAlive)
                    {
                        hp.TakeDamage(self.attackDamage);
                        AudioManager.Instance?.PlayHit();
                        EffectManager.Instance?.SpawnHit(self.player.position);
                    }
                }
                return State = NodeState.Running;
            }
        }

        class Patrol : BTNode
        {
            readonly EnemyBT self;
            bool initialized;
            public Patrol(EnemyBT e) { self = e; }

            public override NodeState Evaluate()
            {
                if (!self.agent.enabled) return State = NodeState.Running;

                if (self.patrolPoints == null || self.patrolPoints.Length == 0)
                {
                    self.agent.isStopped = true;
                    return State = NodeState.Running;
                }

                if (!initialized)
                {
                    self.agent.SetDestination(self.patrolPoints[0].position);
                    self.agent.isStopped = false;
                    initialized = true;
                }

                if (!self.agent.pathPending && self.agent.hasPath && self.agent.remainingDistance < 0.5f)
                {
                    float waitUntil = self.bb.Get<float>("patrolWaitUntil", 0f);
                    if (waitUntil == 0f)
                    {
                        self.agent.isStopped = true;
                        self.bb.Set("patrolWaitUntil", Time.time + self.patrolWaitTime);
                    }
                    else if (Time.time >= waitUntil)
                    {
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
