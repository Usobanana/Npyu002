using UnityEngine;
using System.Collections;

namespace ActionGame
{
    /// <summary>
    /// Enemy 専用のコンボ攻撃コンポーネント。
    /// Player の ComboAttack と同じアニメトリガーを使うが、
    /// ダメージ値・範囲・ヒット判定は独立して管理する。
    ///
    /// EnemyBT から TriggerLight() / TriggerStrong() / TriggerSpecial() を呼ぶ。
    /// </summary>
    public class EnemyCombat : MonoBehaviour
    {
        // ── コンボステップ定義 ─────────────────────────────────────────
        [System.Serializable]
        public class ComboStep
        {
            public string trigger  = "Attack1";
            public float  damage   = 10f;
            public float  hitDelay = 0.30f;
        }

        // ── ライトコンボ ──────────────────────────────────────────────
        [Header("Light Combo")]
        [SerializeField] ComboStep[] lightSteps = new ComboStep[]
        {
            new ComboStep { trigger = "Attack1", damage = 10f, hitDelay = 0.25f },
            new ComboStep { trigger = "Attack2", damage = 12f, hitDelay = 0.25f },
            new ComboStep { trigger = "Attack3", damage = 15f, hitDelay = 0.40f },
            new ComboStep { trigger = "Attack4", damage = 20f, hitDelay = 0.40f },
        };

        // ── ストロングコンボ ──────────────────────────────────────────
        [Header("Strong Combo")]
        [SerializeField] ComboStep[] strongSteps = new ComboStep[]
        {
            new ComboStep { trigger = "SAtk1", damage = 18f, hitDelay = 0.35f },
            new ComboStep { trigger = "SAtk2", damage = 20f, hitDelay = 0.35f },
            new ComboStep { trigger = "SAtk3", damage = 25f, hitDelay = 0.45f },
            new ComboStep { trigger = "SAtk4", damage = 35f, hitDelay = 0.50f },
        };

        // ── ウェーブスペシャル ────────────────────────────────────────
        [Header("Wave Special")]
        [SerializeField] string waveAttackTrigger = "WaveAtk";
        [SerializeField] float  waveDamage        = 30f;
        [SerializeField] float  waveRange         = 5f;
        [SerializeField] float  waveHitDelay      = 0.4f;
        [SerializeField] float  specialCooldown   = 8f;

        // ── コンボタイミング ──────────────────────────────────────────
        [Header("Combo Timing")]
        [SerializeField] float comboWindow    = 0.8f;
        [SerializeField] float comboResetTime = 1.2f;

        // ── 攻撃範囲 ─────────────────────────────────────────────────
        [Header("Attack Shape")]
        [SerializeField] float attackRange  = 2.0f;
        [SerializeField] float attackOffset = 1.0f;

        // ── コンボ状態 ────────────────────────────────────────────────
        int       lightStep        = 0;
        bool      lightBuffered    = false;
        bool      isLightAttacking = false;
        Coroutine lightResetRoutine;

        int       strongStep        = 0;
        bool      strongBuffered    = false;
        bool      isStrongAttacking = false;
        Coroutine strongResetRoutine;

        float specialCooldownTimer = 0f;

        /// <summary>ライトコンボ実行中（EnemyBT のドッジ検知側から参照可能）</summary>
        public bool IsLightAttacking  => isLightAttacking;
        /// <summary>ストロングコンボ実行中</summary>
        public bool IsStrongAttacking => isStrongAttacking;

        Animator animator;
        Health   selfHealth;

        void Awake()
        {
            animator   = GetComponentInChildren<Animator>();
            selfHealth = GetComponent<Health>();
        }

        void Update()
        {
            if (specialCooldownTimer > 0f) specialCooldownTimer -= Time.deltaTime;
        }

        // ── AI 向け公開 API ───────────────────────────────────────────

        /// <summary>ライトコンボを発動（連続呼び出しでコンボ継続）</summary>
        public void TriggerLight()
        {
            if (!isLightAttacking)
                ExecuteLight(1);
            else if (lightStep < lightSteps.Length)
                lightBuffered = true;
        }

        /// <summary>ストロングコンボを発動</summary>
        public void TriggerStrong()
        {
            if (!isStrongAttacking)
                ExecuteStrong(1);
            else if (strongStep < strongSteps.Length)
                strongBuffered = true;
        }

        /// <summary>ウェーブスペシャルを発動（クールダウン中は無視）</summary>
        public void TriggerSpecial()
        {
            if (specialCooldownTimer > 0f) return;
            specialCooldownTimer = specialCooldown;
            TriggerAnim(waveAttackTrigger);
            StartCoroutine(ApplyWaveDamage());
        }

        // ── ライトコンボ内部 ──────────────────────────────────────────

        void ExecuteLight(int step)
        {
            lightStep        = step;
            isLightAttacking = true;
            lightBuffered    = false;

            TriggerAnim(lightSteps[step - 1].trigger);

            if (lightResetRoutine != null) StopCoroutine(lightResetRoutine);
            lightResetRoutine = StartCoroutine(LightTimer(step));
            StartCoroutine(ApplyDamage(lightSteps[step - 1]));
        }

        IEnumerator LightTimer(int step)
        {
            yield return new WaitForSeconds(comboWindow);

            if (lightBuffered && step < lightSteps.Length)
            {
                ExecuteLight(step + 1);
            }
            else
            {
                yield return new WaitForSeconds(comboResetTime - comboWindow);
                lightStep = 0; isLightAttacking = false; lightBuffered = false;
            }
        }

        // ── ストロングコンボ内部 ──────────────────────────────────────

        void ExecuteStrong(int step)
        {
            strongStep        = step;
            isStrongAttacking = true;
            strongBuffered    = false;

            TriggerAnim(strongSteps[step - 1].trigger);

            if (strongResetRoutine != null) StopCoroutine(strongResetRoutine);
            strongResetRoutine = StartCoroutine(StrongTimer(step));
            StartCoroutine(ApplyDamage(strongSteps[step - 1]));
        }

        IEnumerator StrongTimer(int step)
        {
            yield return new WaitForSeconds(comboWindow);

            if (strongBuffered && step < strongSteps.Length)
            {
                ExecuteStrong(step + 1);
            }
            else
            {
                yield return new WaitForSeconds(comboResetTime - comboWindow);
                strongStep = 0; isStrongAttacking = false; strongBuffered = false;
            }
        }

        // ── ヒット判定 ────────────────────────────────────────────────

        IEnumerator ApplyDamage(ComboStep step)
        {
            yield return new WaitForSeconds(step.hitDelay);
            if (selfHealth != null && !selfHealth.IsAlive) yield break;

            var center = transform.position + transform.forward * attackOffset;
            var hits   = Physics.OverlapSphere(center, attackRange);

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                var hp = hit.GetComponentInParent<Health>();
                if (hp != null && hp.IsAlive && hp != selfHealth)
                {
                    hp.TakeDamage(step.damage);
                    AudioManager.Instance?.PlayHit();
                    EffectManager.Instance?.SpawnHit(hit.transform.position);
                }
            }
        }

        IEnumerator ApplyWaveDamage()
        {
            yield return new WaitForSeconds(waveHitDelay);
            if (selfHealth != null && !selfHealth.IsAlive) yield break;

            var hits = Physics.OverlapSphere(transform.position, waveRange);

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                var hp = hit.GetComponentInParent<Health>();
                if (hp != null && hp.IsAlive && hp != selfHealth)
                {
                    hp.TakeDamage(waveDamage);
                    AudioManager.Instance?.PlayHit();
                    EffectManager.Instance?.SpawnHit(hit.transform.position);
                }
            }
        }

        void TriggerAnim(string triggerName)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetTrigger(triggerName);
        }

        void OnDrawGizmosSelected()
        {
            Gizmos.color = new Color(1f, 0.2f, 0.2f, 0.3f);
            Gizmos.DrawWireSphere(transform.position + transform.forward * attackOffset, attackRange);
            Gizmos.color = new Color(1f, 0.8f, 0f, 0.15f);
            Gizmos.DrawWireSphere(transform.position, waveRange);
        }
    }
}
