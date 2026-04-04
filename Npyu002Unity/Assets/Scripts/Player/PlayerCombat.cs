using UnityEngine;
using UnityEngine.InputSystem;

namespace ActionGame
{
    /// <summary>
    /// Player の近接攻撃。左クリックで攻撃。
    /// OverlapSphere で範囲内の Health を持つ他オブジェクトをヒット判定。
    /// </summary>
    public class PlayerCombat : MonoBehaviour
    {
        [Header("Attack")]
        [SerializeField] float attackDamage = 30f;
        [SerializeField] float attackRange = 2f;
        [SerializeField] float attackOffset = 1f;
        [SerializeField] float attackCooldown = 0.6f;

        float nextAttackTime;

        void Update()
        {
            if (Mouse.current == null) return;
            if (Mouse.current.leftButton.wasPressedThisFrame)
                TryAttack();
        }

        void TryAttack()
        {
            if (Time.time < nextAttackTime) return;
            nextAttackTime = Time.time + attackCooldown;

            var center = transform.position + transform.forward * attackOffset;
            var hits = Physics.OverlapSphere(center, attackRange);

            int hitCount = 0;
            foreach (var hit in hits)
            {
                // 自分自身・自分の子はスキップ
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;

                var hp = hit.GetComponentInParent<Health>();
                if (hp != null && hp.IsAlive && hp.gameObject != gameObject)
                {
                    hp.TakeDamage(attackDamage);
                    Debug.Log($"[Player] Hit {hit.name} for {attackDamage} damage");
                    hitCount++;
                }
            }

            if (hitCount == 0)
                Debug.Log("[Player] Attack missed");
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.4f);
            Gizmos.DrawSphere(transform.position + transform.forward * attackOffset, attackRange);
        }
    }
}
