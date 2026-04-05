using UnityEngine;
using UnityEngine.AI;
using System.Collections;

namespace ActionGame
{
    /// <summary>
    /// 攻撃ヒット時に敵を吹き飛ばすコンポーネント。
    /// ComboAttack から ApplyKnockback() を呼ぶ。
    /// NavMeshAgent を一時無効にして transform を直接動かし、その後再有効化する。
    /// </summary>
    [RequireComponent(typeof(NavMeshAgent))]
    public class KnockbackReceiver : MonoBehaviour
    {
        [SerializeField] float resistance = 1f; // 大きいほど吹き飛びにくい（ボス用途など）

        NavMeshAgent agent;
        bool         isKnockedBack;

        void Awake() => agent = GetComponent<NavMeshAgent>();

        /// <summary>
        /// ノックバックを適用する。
        /// </summary>
        /// <param name="direction">吹き飛び方向（normalized）</param>
        /// <param name="force">吹き飛ばし力</param>
        public void ApplyKnockback(Vector3 direction, float force)
        {
            if (isKnockedBack) return;
            StartCoroutine(KnockbackCoroutine(direction, force / Mathf.Max(resistance, 0.1f)));
        }

        IEnumerator KnockbackCoroutine(Vector3 direction, float force)
        {
            isKnockedBack = true;
            // enabled/isOnNavMesh を確認してから isStopped → 無効化
            if (agent.enabled && agent.isOnNavMesh)
                agent.isStopped = true;
            agent.enabled = false; // NavMesh 制御を一時解除

            float elapsed  = 0f;
            float duration = 0.15f;

            while (elapsed < duration)
            {
                float t     = elapsed / duration;
                float speed = Mathf.Lerp(force, 0f, t); // 徐々に減速
                transform.position += direction * speed * Time.deltaTime;
                elapsed += Time.deltaTime;
                yield return null;
            }

            // NavMesh 上の有効な位置に補正してから再有効化
            if (NavMesh.SamplePosition(transform.position, out NavMeshHit hit, 1f, NavMesh.AllAreas))
                transform.position = hit.position;

            agent.enabled   = true;
            agent.isStopped = false;
            isKnockedBack   = false;
        }
    }
}
