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
            public string trigger = "Attack1";
        }

        // ── ライトコンボ（左クリック）──────────────────────────────────
        [Header("Light Combo  ─ 左クリック")]
        [SerializeField] ComboStep[] lightSteps = new ComboStep[]
        {
            new ComboStep { trigger = "Attack1" },
            new ComboStep { trigger = "Attack2" },
            new ComboStep { trigger = "Attack3" },
            new ComboStep { trigger = "Attack4" },
        };

        // ── ストロングコンボ（右クリック）─────────────────────────────
        [Header("Strong Combo ─ 右クリック")]
        [SerializeField] ComboStep[] strongSteps = new ComboStep[]
        {
            new ComboStep { trigger = "SAtk1" },
            new ComboStep { trigger = "SAtk2" },
            new ComboStep { trigger = "SAtk3" },
            new ComboStep { trigger = "SAtk4" },
        };

        // ── ウェーブスペシャル（E キー）──────────────────────────────
        [Header("Wave Special ─ E キー")]
        [SerializeField] string waveAttackTrigger = "WaveAtk";
        [SerializeField] float  waveRange         = 5f;
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

            TriggerAnim(lightSteps[step - 1].trigger);
            OnComboStep?.Invoke(step);

            if (lightResetRoutine != null) StopCoroutine(lightResetRoutine);
            lightResetRoutine = StartCoroutine(LightTimer(step));
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

            TriggerAnim(strongSteps[step - 1].trigger);
            OnComboStep?.Invoke(step + 10); // 10オフセットでライトと区別

            if (strongResetRoutine != null) StopCoroutine(strongResetRoutine);
            strongResetRoutine = StartCoroutine(StrongTimer(step));
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

            TriggerAnim(waveAttackTrigger);
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

        /// <summary>AttackEventBehaviour（StateMachineBehaviour）から外部呼び出し用</summary>
        public void ShowHitGizmoExternal(Vector3 center, float range, bool didHit)
        {
            gizmoTimer  = GizmoDuration;
            gizmoRange  = range;
            gizmoOffset = Vector3.Distance(transform.position, center); // 近似
            gizmoIsWave = false;
            gizmoDidHit = didHit;
        }

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
