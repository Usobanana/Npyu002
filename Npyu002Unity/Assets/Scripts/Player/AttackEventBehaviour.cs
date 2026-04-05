using UnityEngine;

namespace ActionGame
{
    /// <summary>
    /// 攻撃アニメーションの任意タイミングでダメージ判定 + SE を発火する StateMachineBehaviour。
    ///
    /// 使い方:
    ///   1. AnimatorController で攻撃ステートを選択
    ///   2. Inspector 右下「Add Behaviour」→ AttackEventBehaviour
    ///   3. Hit Count でヒット回数を設定（最大 4）
    ///   4. Times / Damages / Ranges / Play Sounds の各配列を同じ数に揃えて設定
    ///      Times  : 0〜1 スライダーでヒットタイミング
    ///      Damages: このヒットのダメージ量
    ///      Ranges : 判定半径（m）
    ///      Offsets: 判定球位置（キャラ前方ローカル座標）
    /// </summary>
    public class AttackEventBehaviour : StateMachineBehaviour
    {
        // ── Inspector に確実に表示される並列配列方式 ──────────────────

        [Header("ヒット回数 (以下の配列サイズをこれに合わせる)")]
        [SerializeField, Range(1, 4)] int hitCount = 1;

        [Header("各ヒットのタイミング (0=開始, 1=終了)")]
        [SerializeField, Range(0f, 1f)] float time0 = 0.25f;
        [SerializeField, Range(0f, 1f)] float time1 = 0.60f;
        [SerializeField, Range(0f, 1f)] float time2 = 0.75f;
        [SerializeField, Range(0f, 1f)] float time3 = 0.90f;

        [Header("各ヒットのダメージ")]
        [SerializeField] float damage0 = 20f;
        [SerializeField] float damage1 = 20f;
        [SerializeField] float damage2 = 20f;
        [SerializeField] float damage3 = 20f;

        [Header("各ヒットの判定半径 (m)")]
        [SerializeField] float range0 = 1.5f;
        [SerializeField] float range1 = 1.5f;
        [SerializeField] float range2 = 1.5f;
        [SerializeField] float range3 = 1.5f;

        [Header("各ヒットの判定オフセット (キャラ前方ローカル)")]
        [SerializeField] Vector3 offset0 = new Vector3(0f, 0.8f, 1f);
        [SerializeField] Vector3 offset1 = new Vector3(0f, 0.8f, 1f);
        [SerializeField] Vector3 offset2 = new Vector3(0f, 0.8f, 1f);
        [SerializeField] Vector3 offset3 = new Vector3(0f, 0.8f, 1f);

        [Header("スイング SE を鳴らすか")]
        [SerializeField] bool playSound0 = true;
        [SerializeField] bool playSound1 = true;
        [SerializeField] bool playSound2 = true;
        [SerializeField] bool playSound3 = true;

        // ── 実行時バッファ（Awake不可のため遅延初期化）───────────────
        float[]   times;
        float[]   damages;
        float[]   ranges;
        Vector3[] offsets;
        bool[]    playSounds;
        bool[]    fired;
        ComboAttack comboAttack;

        void BuildArrays()
        {
            times      = new float[]   { time0,    time1,    time2,    time3    };
            damages    = new float[]   { damage0,  damage1,  damage2,  damage3  };
            ranges     = new float[]   { range0,   range1,   range2,   range3   };
            offsets    = new Vector3[] { offset0,  offset1,  offset2,  offset3  };
            playSounds = new bool[]    { playSound0, playSound1, playSound2, playSound3 };
        }

        public override void OnStateEnter(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (comboAttack == null)
                comboAttack = animator.GetComponentInParent<ComboAttack>();

            BuildArrays();
            fired = new bool[hitCount];
        }

        public override void OnStateUpdate(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (fired == null) { BuildArrays(); fired = new bool[hitCount]; }

            float t = stateInfo.normalizedTime % 1f;

            for (int i = 0; i < hitCount; i++)
            {
                if (!fired[i] && t >= times[i])
                {
                    fired[i] = true;
                    Fire(animator.transform, i);
                }
            }
        }

        public override void OnStateExit(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            fired = null;
        }

        // ── ヒット発火 ────────────────────────────────────────────
        void Fire(Transform animatorRoot, int idx)
        {
            if (playSounds[idx])
                AudioManager.Instance?.PlayAttack();

            var root   = comboAttack != null ? comboAttack.transform : animatorRoot;
            var center = root.TransformPoint(offsets[idx]);

            int hitCnt = 0;
            var hits   = Physics.OverlapSphere(center, ranges[idx]);
            foreach (var col in hits)
            {
                var hp = col.GetComponentInParent<Health>();
                if (hp == null || !hp.IsAlive) continue;
                if (comboAttack != null && col.transform.IsChildOf(comboAttack.transform)) continue;
                if (comboAttack != null && col.transform == comboAttack.transform)          continue;

                hp.TakeDamage(damages[idx]);
                hitCnt++;

                AudioManager.Instance?.PlayHit();

                float power = Mathf.Clamp01(damages[idx] / 50f);
                HitStopManager.Instance?.Trigger(Mathf.Lerp(0.04f, 0.10f, power));
                CameraShake.Instance?.Shake(0.12f, Mathf.Lerp(0.05f, 0.20f, power));

                var dir = (col.transform.position - root.position).normalized;
                dir.y = 0;
                col.GetComponentInParent<KnockbackReceiver>()?
                    .ApplyKnockback(dir, Mathf.Lerp(2f, 5f, power));

                EffectManager.Instance?.SpawnHit(col.transform.position);
            }

            comboAttack?.ShowHitGizmoExternal(center, ranges[idx], hitCnt > 0);
        }
    }
}
