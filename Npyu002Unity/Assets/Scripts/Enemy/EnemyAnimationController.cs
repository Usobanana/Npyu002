using UnityEngine;
using UnityEngine.AI;

namespace ActionGame
{
    /// <summary>
    /// NavMeshAgent の速度と EnemyBT のイベントを子 Animator に反映する。
    /// PlayerAnimationController の Enemy 版。
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyAnimationController : MonoBehaviour
    {
        Animator anim;
        NavMeshAgent agent;

        void Awake()
        {
            agent = GetComponent<NavMeshAgent>();
            anim  = GetComponentInChildren<Animator>();

            var health = GetComponent<Health>();
            if (health != null)
            {
                health.OnHealthChanged += (cur, max) =>
                {
                    if (cur < max && cur > 0f)
                        anim?.SetTrigger("HitReact");
                };
                health.OnDeath += () => anim?.SetBool("IsDead", true);
            }
        }

        void Update()
        {
            if (anim == null || agent == null) return;
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }

        /// <summary>EnemyBT の AttackPlayer ノードから呼ぶ</summary>
        public void TriggerAttack() => anim?.SetTrigger("Attack");
    }
}
