using UnityEngine;

namespace ActionGame
{
    /// <summary>
    /// PlayerController / PlayerCombat / Health の状態を
    /// 子オブジェクトの Animator に反映する。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class PlayerAnimationController : MonoBehaviour
    {
        Animator         anim;
        CharacterController cc;
        bool wasGrounded;

        void Awake()
        {
            cc   = GetComponent<CharacterController>();
            anim = GetComponentInChildren<Animator>();

            // PlayerCombat（旧システム）があれば Attack トリガーを接続
            var combat = GetComponent<PlayerCombat>();
            if (combat != null)
                combat.OnAttack += () =>
                {
                    if (anim != null && anim.runtimeAnimatorController != null)
                        anim.SetTrigger("Attack");
                };

            var health = GetComponent<Health>();
            if (health != null)
            {
                health.OnHealthChanged += (cur, max) =>
                {
                    if (cur < max && cur > 0f && anim != null && anim.runtimeAnimatorController != null)
                        anim.SetTrigger("HitReact");
                };
                health.OnDeath += () =>
                {
                    if (anim != null && anim.runtimeAnimatorController != null)
                        anim.SetBool("IsDead", true);
                };
            }
        }

        void Update()
        {
            if (anim == null || anim.runtimeAnimatorController == null) return;

            var vel = cc.velocity;
            vel.y = 0f;
            anim.SetFloat("Speed", vel.magnitude);
        }
    }
}
