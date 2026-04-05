using UnityEngine;
using System.Collections;

namespace ActionGame
{
    /// <summary>
    /// 攻撃で壊れる爆発Cube（建物イメージ）。
    /// Health が 0 になると爆発エフェクト＋周囲の敵・Playerにダメージ。
    /// 必要コンポーネント: Health
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class ExplosiveCube : MonoBehaviour
    {
        [Header("Explosion")]
        [SerializeField] float explosionRadius  = 4f;
        [SerializeField] float explosionDamage  = 50f;
        [SerializeField] float explosionDelay   = 0.1f;  // 爆発までのわずかな遅延（演出）
        [SerializeField] LayerMask damageLayers = ~0;    // 全レイヤー

        [Header("Visual")]
        [SerializeField] Color normalColor    = new Color(0.4f, 0.55f, 0.7f);
        [SerializeField] Color damagedColor   = new Color(0.9f, 0.5f, 0.1f);
        [SerializeField] Color criticalColor  = new Color(1f,   0.15f, 0.05f);

        Health health;
        Renderer rend;
        MaterialPropertyBlock mpb;
        bool exploded = false;

        void Awake()
        {
            health = GetComponent<Health>();
            rend   = GetComponentInChildren<Renderer>();
            mpb    = new MaterialPropertyBlock();

            health.OnHealthChanged += OnHealthChanged;
            health.OnDeath         += OnDeath;

            // 初期色を設定
            SetColor(normalColor);
        }

        void OnHealthChanged(float current, float max)
        {
            float ratio = current / max;
            // HP 比率に応じて色を変化
            Color c = ratio > 0.5f
                ? Color.Lerp(damagedColor, normalColor, (ratio - 0.5f) * 2f)
                : Color.Lerp(criticalColor, damagedColor, ratio * 2f);
            SetColor(c);

            // ダメージ時に小さく揺らす
            StopAllCoroutines();
            StartCoroutine(ShakeCoroutine());
        }

        void OnDeath()
        {
            if (exploded) return;
            exploded = true;
            StartCoroutine(ExplodeCoroutine());
        }

        IEnumerator ExplodeCoroutine()
        {
            // 少し縮んでから爆発（タメ演出）
            float t = 0f;
            var originalScale = transform.localScale;
            while (t < explosionDelay)
            {
                t += Time.deltaTime;
                transform.localScale = originalScale * Mathf.Lerp(1f, 1.3f, t / explosionDelay);
                yield return null;
            }

            // 爆発範囲ダメージ
            var hits = Physics.OverlapSphere(transform.position, explosionRadius, damageLayers);
            foreach (var hit in hits)
            {
                if (hit.gameObject == gameObject) continue;
                var hp = hit.GetComponentInParent<Health>();
                if (hp != null && hp.IsAlive)
                {
                    // 距離に応じてダメージ減衰
                    float dist   = Vector3.Distance(transform.position, hit.transform.position);
                    float dmg    = explosionDamage * Mathf.Clamp01(1f - dist / explosionRadius);
                    hp.TakeDamage(dmg);
                }
            }

            // エフェクト
            EffectManager.Instance?.SpawnEnemyDeath(transform.position);
            AudioManager.Instance?.PlayPlayerDeath(); // 仮SE（爆発SE）

            // SE 再生の少し後に消す
            yield return new WaitForSeconds(0.05f);
            Destroy(gameObject);
        }

        IEnumerator ShakeCoroutine()
        {
            var origin = transform.localPosition;
            float elapsed = 0f;
            float duration = 0.2f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                float strength = Mathf.Lerp(0.1f, 0f, elapsed / duration);
                transform.localPosition = origin + (Vector3)Random.insideUnitCircle * strength;
                yield return null;
            }
            transform.localPosition = origin;
        }

        void SetColor(Color c)
        {
            if (rend == null) return;
            rend.GetPropertyBlock(mpb);
            mpb.SetColor("_BaseColor", c);  // URP
            mpb.SetColor("_Color", c);       // Built-in fallback
            rend.SetPropertyBlock(mpb);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.3f, 0f, 0.25f);
            Gizmos.DrawWireSphere(transform.position, explosionRadius);
        }
    }
}
