using UnityEngine;

namespace ActionGame
{
    /// <summary>
    /// Player の近接攻撃。
    /// InputHandler.AttackPressed で発動し、AudioManager で SE を再生する。
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Attack")]
        [SerializeField] float attackDamage  = 30f;
        [SerializeField] float attackRange   = 2f;
        [SerializeField] float attackOffset  = 1f;
        [SerializeField] float attackCooldown = 0.6f;

        float nextAttackTime;

        void Update()
        {
            if (InputHandler.Instance == null) return;
            if (InputHandler.Instance.AttackPressed)
                TryAttack();
        }

        void TryAttack()
        {
            if (Time.time < nextAttackTime) return;
            nextAttackTime = Time.time + attackCooldown;

            // SE
            AudioManager.Instance?.PlayAttack();

            var center = transform.position + transform.forward * attackOffset;
            var hits   = Physics.OverlapSphere(center, attackRange);

            int hitCount = 0;
            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                var hp = hit.GetComponentInParent<Health>();
                if (hp != null && hp.IsAlive && hp.gameObject != gameObject)
                {
                    hp.TakeDamage(attackDamage);
                    AudioManager.Instance?.PlayHit();
                    EffectManager.Instance?.SpawnHit(hit.transform.position);
                    hitCount++;
                }
            }

            Debug.Log(hitCount > 0 ? $"[Player] Hit {hitCount} target(s)" : "[Player] Miss");
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawSphere(transform.position + transform.forward * attackOffset, attackRange);
        }
    }
}
