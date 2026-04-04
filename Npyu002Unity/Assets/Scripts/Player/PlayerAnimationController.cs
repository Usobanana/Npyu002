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

            var combat = GetComponent<PlayerCombat>();
            if (combat != null)
                combat.OnAttack += () => anim?.SetTrigger("Attack");

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
            if (anim == null) return;

            var vel = cc.velocity;
            vel.y = 0f;
            anim.SetFloat("Speed", vel.magnitude);

            bool grounded = cc.isGrounded;
            if (wasGrounded && !grounded)
                anim.SetBool("IsJumping", true);
            else if (!wasGrounded && grounded)
                anim.SetBool("IsJumping", false);
            wasGrounded = grounded;
        }
    }
}
