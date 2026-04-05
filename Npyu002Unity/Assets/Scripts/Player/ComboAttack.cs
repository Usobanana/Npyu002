using UnityEngine;
using System.Collections;

namespace ActionGame
{
    /// <summary>
    /// 拡張コンボ攻撃システム。
    ///
    ///   左クリック  : ライトコンボ（Combo01 4段）
    ///   右クリック  : ストロングコンボ（Combo02 4段）
    ///   E キー      : ウェーブスペシャル（全方位AoE 単発）
    ///
    /// Animator Trigger: Attack1〜4 / SAtk1〜4 / WaveAtk
    /// </summary>
    public class ComboAttack : MonoBehaviour
    {
        // ── コンボステップ定義 ─────────────────────────────────────────
        [System.Serializable]
        public class ComboStep
        {
            public string trigger  = "Attack1";
            public float  damage   = 20f;
            public float  hitDelay = 0.25f;  // ダメージ判定までの遅延（秒）
        }

        // ── ライトコンボ（左クリック）──────────────────────────────────
        [Header("Light Combo  ─ 左クリック")]
        [SerializeField] ComboStep[] lightSteps = new ComboStep[]
        {
            new ComboStep { trigger = "Attack1", damage = 20f, hitDelay = 0.25f },
            new ComboStep { trigger = "Attack2", damage = 25f, hitDelay = 0.25f },
            new ComboStep { trigger = "Attack3", damage = 30f, hitDelay = 0.40f },
            new ComboStep { trigger = "Attack4", damage = 45f, hitDelay = 0.40f },
        };

        // ── ストロングコンボ（右クリック）─────────────────────────────
        [Header("Strong Combo ─ 右クリック")]
        [SerializeField] ComboStep[] strongSteps = new ComboStep[]
        {
            new ComboStep { trigger = "SAtk1", damage = 35f, hitDelay = 0.35f },
            new ComboStep { trigger = "SAtk2", damage = 40f, hitDelay = 0.35f },
            new ComboStep { trigger = "SAtk3", damage = 55f, hitDelay = 0.45f },
            new ComboStep { trigger = "SAtk4", damage = 75f, hitDelay = 0.50f },
        };

        // ── ウェーブスペシャル（E キー）──────────────────────────────
        [Header("Wave Special ─ E キー")]
        [SerializeField] string waveAttackTrigger = "WaveAtk";
        [SerializeField] float  waveDamage        = 80f;
        [SerializeField] float  waveRange         = 5f;
        [SerializeField] float  waveHitDelay      = 0.4f;
        [SerializeField] float  specialCooldown   = 5f;

        // ── コンボタイミング ──────────────────────────────────────────
        [Header("Combo Timing")]
        [SerializeField] float comboWindow    = 0.8f;   // 次入力受付時間
        [SerializeField] float comboResetTime = 1.2f;   // この時間入力なしでリセット

        // ── 攻撃範囲 ─────────────────────────────────────────────────
        [Header("Attack Shape")]
        [SerializeField] float attackRange  = 2.0f;
        [SerializeField] float attackOffset = 1.0f;

        [Header("References")]
        [SerializeField] Animator animator;

        // ── ライトコンボ状態 ──────────────────────────────────────────
        int       lightStep        = 0;
        bool      lightBuffered    = false;
        bool      isLightAttacking = false;
        Coroutine lightResetRoutine;

        // ── ストロングコンボ状態 ──────────────────────────────────────
        int       strongStep        = 0;
        bool      strongBuffered    = false;
        bool      isStrongAttacking = false;
        Coroutine strongResetRoutine;

        // ── スペシャル状態 ────────────────────────────────────────────
        float specialCooldownTimer = 0f;

        public event System.Action<int> OnComboStep;

        // ── デバッグ UI 向け読み出しプロパティ ────────────────────────
        public bool  IsLightAttacking  => isLightAttacking;
        public bool  IsStrongAttacking => isStrongAttacking;
        public int   LightStep         => lightStep;
        public int   StrongStep        => strongStep;
        public int   LightStepMax      => lightSteps.Length;
        public int   StrongStepMax     => strongSteps.Length;
        public float SpecialCooldownNormalized => specialCooldown > 0f
            ? Mathf.Clamp01(specialCooldownTimer / specialCooldown) : 0f;

        Health selfHealth; // 自分自身への攻撃を除外するために保持

        void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            selfHealth = GetComponent<Health>();
        }

        // ── ライトコンボ ──────────────────────────────────────────────

        void HandleLightInput()
        {
            if (!isLightAttacking)
                ExecuteLight(1);
            else if (lightStep < lightSteps.Length)
                lightBuffered = true;
        }

        void ExecuteLight(int step)
        {
            lightStep        = step;
            isLightAttacking = true;
            lightBuffered    = false;

            AudioManager.Instance?.PlayAttack();
            TriggerAnim(lightSteps[step - 1].trigger);
            OnComboStep?.Invoke(step);

            if (lightResetRoutine != null) StopCoroutine(lightResetRoutine);
            lightResetRoutine = StartCoroutine(LightTimer(step));
            StartCoroutine(ApplyDamage(lightSteps[step - 1], attackRange, attackOffset));
        }

        IEnumerator LightTimer(int step)
        {
            yield return new WaitForSeconds(comboWindow);

            // 押しっぱなし or バッファ済みなら次の段へ自動進行
            bool autoAdvance = InputHandler.Instance != null && InputHandler.Instance.AttackHeld;
            if ((lightBuffered || autoAdvance) && step < lightSteps.Length)
            {
                ExecuteLight(step + 1);
            }
            else
            {
                yield return new WaitForSeconds(comboResetTime - comboWindow);
                lightStep = 0; isLightAttacking = false; lightBuffered = false;
            }
        }

        // ── ストロングコンボ ──────────────────────────────────────────

        void HandleStrongInput()
        {
            if (!isStrongAttacking)
                ExecuteStrong(1);
            else if (strongStep < strongSteps.Length)
                strongBuffered = true;
        }

        void ExecuteStrong(int step)
        {
            strongStep        = step;
            isStrongAttacking = true;
            strongBuffered    = false;

            AudioManager.Instance?.PlayAttack();
            TriggerAnim(strongSteps[step - 1].trigger);
            OnComboStep?.Invoke(step + 10); // 10オフセットでライトと区別

            if (strongResetRoutine != null) StopCoroutine(strongResetRoutine);
            strongResetRoutine = StartCoroutine(StrongTimer(step));
            StartCoroutine(ApplyDamage(strongSteps[step - 1], attackRange, attackOffset));
        }

        IEnumerator StrongTimer(int step)
        {
            yield return new WaitForSeconds(comboWindow);

            // 押しっぱなし or バッファ済みなら次の段へ自動進行
            bool autoAdvance = InputHandler.Instance != null && InputHandler.Instance.StrongAttackHeld;
            if ((strongBuffered || autoAdvance) && step < strongSteps.Length)
            {
                ExecuteStrong(step + 1);
            }
            else
            {
                yield return new WaitForSeconds(comboResetTime - comboWindow);
                strongStep = 0; isStrongAttacking = false; strongBuffered = false;
            }
        }

        // ── ウェーブスペシャル ────────────────────────────────────────

        void HandleSpecial()
        {
            if (specialCooldownTimer > 0f) return;
            specialCooldownTimer = specialCooldown;

            AudioManager.Instance?.PlayAttack();
            TriggerAnim(waveAttackTrigger);

            StartCoroutine(ApplyWaveDamage());
        }

        IEnumerator ApplyWaveDamage()
        {
            yield return new WaitForSeconds(waveHitDelay);

            var hits     = Physics.OverlapSphere(transform.position, waveRange);
            int hitCount = 0;

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                var hp = hit.GetComponentInParent<Health>();
                if (hp != null && hp.IsAlive && hp != selfHealth)
                {
                    hp.TakeDamage(waveDamage);
                    AudioManager.Instance?.PlayHit();
                    EffectManager.Instance?.SpawnHit(hit.transform.position);

                    // Wave は強めのノックバック
                    var dir = (hit.transform.position - transform.position).normalized;
                    hit.GetComponentInParent<KnockbackReceiver>()?.ApplyKnockback(dir, 6f);
                    hitCount++;
                }
            }

            ShowHitGizmo(waveRange, 0f, true, hitCount > 0);
            // Wave は全体に強い打感
            if (hitCount > 0)
            {
                HitStopManager.Instance?.Trigger(0.12f);
                // Shake(duration, magnitude)
                CameraShake.Instance?.Shake(0.22f, 0.35f);
                Debug.Log($"[Wave Special] {hitCount} 体ヒット / {waveDamage} ダメージ");
            }
        }

        // ── 通常ダメージ判定（共通）──────────────────────────────────

        IEnumerator ApplyDamage(ComboStep step, float range, float offset)
        {
            yield return new WaitForSeconds(step.hitDelay);

            var center   = transform.position + transform.forward * offset;
            var hits     = Physics.OverlapSphere(center, range);
            int hitCount = 0;

            foreach (var hit in hits)
            {
                if (hit.transform == transform || hit.transform.IsChildOf(transform)) continue;
                var hp = hit.GetComponentInParent<Health>();
                if (hp != null && hp.IsAlive && hp != selfHealth)
                {
                    hp.TakeDamage(step.damage);
                    AudioManager.Instance?.PlayHit();
                    EffectManager.Instance?.SpawnHit(hit.transform.position);

                    // 打感（ダメージ量に応じてスケール）
                    float power = Mathf.Clamp01(step.damage / 50f);
                    HitStopManager.Instance?.Trigger(Mathf.Lerp(0.04f, 0.10f, power));
                    // Shake(duration, magnitude)
                    CameraShake.Instance?.Shake(0.12f, Mathf.Lerp(0.05f, 0.20f, power));
                    var knockDir = (hit.transform.position - transform.position).normalized;
                    hit.GetComponentInParent<KnockbackReceiver>()?.ApplyKnockback(
                        knockDir, Mathf.Lerp(2f, 5f, power));

                    hitCount++;
                }
            }

            ShowHitGizmo(range, offset, false, hitCount > 0);
            if (hitCount > 0)
                Debug.Log($"[Combo] {step.trigger} {hitCount} 体ヒット / {step.damage} ダメージ");
        }

        // ── ヘルパー ──────────────────────────────────────────────────

        void TriggerAnim(string triggerName)
        {
            if (animator != null && animator.runtimeAnimatorController != null)
                animator.SetTrigger(triggerName);
        }

        // ── Gizmo 可視化 ──────────────────────────────────────────────
        // 判定が出た瞬間だけ表示するためのタイマーと位置
        float  gizmoTimer      = 0f;
        float  gizmoRange      = 0f;
        float  gizmoOffset     = 0f;
        bool   gizmoIsWave     = false;
        bool   gizmoDidHit     = false;
        const float GizmoDuration = 0.25f; // 表示時間（秒）

        /// <summary>判定が出た瞬間に Gizmo タイマーをセット（ApplyDamage / ApplyWaveDamage から呼ぶ）</summary>
        void ShowHitGizmo(float range, float offset, bool isWave, bool didHit)
        {
            gizmoTimer  = GizmoDuration;
            gizmoRange  = range;
            gizmoOffset = offset;
            gizmoIsWave = isWave;
            gizmoDidHit = didHit;
        }

        void Update()
        {
            if (InputHandler.Instance == null) return;

            if (gizmoTimer > 0f) gizmoTimer -= Time.deltaTime;

            if (specialCooldownTimer > 0f) specialCooldownTimer -= Time.deltaTime;

            if (InputHandler.Instance.AttackPressed)       HandleLightInput();
            if (InputHandler.Instance.StrongAttackPressed) HandleStrongInput();
            if (InputHandler.Instance.SpecialPressed)      HandleSpecial();
        }

        void OnDrawGizmos()
        {
#if UNITY_EDITOR
            // 常時：選択中のみ薄く表示
            if (UnityEditor.Selection.activeGameObject == gameObject)
            {
                Gizmos.color = new Color(1f, 0.3f, 0f, 0.15f);
                Gizmos.DrawWireSphere(transform.position + transform.forward * attackOffset, attackRange);
                Gizmos.color = new Color(1f, 0.8f, 0f, 0.1f);
                Gizmos.DrawWireSphere(transform.position, waveRange);
            }
#endif
            // 判定発生中：色を変えて強調表示
            if (gizmoTimer > 0f)
            {
                float alpha = gizmoTimer / GizmoDuration; // 徐々にフェードアウト
                if (gizmoIsWave)
                {
                    // Wave: 全方位黄色
                    Gizmos.color = new Color(1f, 0.9f, 0f, alpha * 0.8f);
                    Gizmos.DrawSphere(transform.position, gizmoRange);
                }
                else
                {
                    var center = transform.position + transform.forward * gizmoOffset;
                    // ヒットあり: 赤 / ヒットなし: 白
                    Gizmos.color = gizmoDidHit
                        ? new Color(1f, 0.1f, 0.1f, alpha * 0.9f)
                        : new Color(1f, 1f,   1f,   alpha * 0.5f);
                    Gizmos.DrawSphere(center, gizmoRange);
                }
            }
        }
    }
}
