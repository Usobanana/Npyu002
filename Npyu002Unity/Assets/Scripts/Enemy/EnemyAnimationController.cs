using UnityEngine;
using UnityEngine.AI;

namespace ActionGame
{
    /// <summary>
    /// NavMeshAgent の速度と Health イベントを子 Animator に反映する。
    /// EnemyBT から TriggerAttack() / TriggerHitReact() を呼ぶ。
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    [RequireComponent(typeof(Health))]
    public class EnemyAnimationController : MonoBehaviour
    {
        Animator     anim;
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
                        TriggerHitReact();
                };
                health.OnDeath += () =>
                {
                    if (anim != null) anim.SetBool("IsDead", true);
                };
            }
        }

        void Update()
        {
            if (anim == null || agent == null) return;
            // NavMeshAgent の速度を Speed パラメータに流す
            anim.SetFloat("Speed", agent.velocity.magnitude);
        }

        /// <summary>攻撃アニメーションを再生（EnemyBT から呼ぶ）</summary>
        public void TriggerAttack()
        {
            if (anim != null && anim.runtimeAnimatorController != null)
                anim.SetTrigger("Attack");
        }

        /// <summary>被弾リアクションを再生（EnemyBT / Health から呼ぶ）</summary>
        public void TriggerHitReact()
        {
            if (anim != null && anim.runtimeAnimatorController != null)
                anim.SetTrigger("HitReact");
        }
    }
}
