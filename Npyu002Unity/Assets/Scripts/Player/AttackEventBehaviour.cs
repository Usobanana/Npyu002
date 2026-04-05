using System.Collections.Generic;
using UnityEngine;

namespace ActionGame
{
    // ────────────────────────────────────────────────────────────
    //  1 回の攻撃ヒットを定義するデータ
    // ────────────────────────────────────────────────────────────
    [System.Serializable]
    public class AttackHitEvent
    {
        [Tooltip("アニメーション内の発生タイミング (0 = 開始, 1 = 終了)")]
        [Range(0f, 1f)] public float time   = 0.25f;

        [Tooltip("このヒットで与えるダメージ")]
        public float damage = 20f;

        [Tooltip("判定の半径 (m)")]
        public float range  = 1.5f;

        [Tooltip("キャラ前方ローカル座標でのオフセット")]
        public Vector3 offset = new Vector3(0f, 0.8f, 1f);

        [Tooltip("ヒット時にスイング SE を鳴らすか")]
        public bool playSound = true;
    }

    // ────────────────────────────────────────────────────────────
    //  StateMachineBehaviour 本体
    //  AnimatorController の攻撃ステートにアタッチする
    // ────────────────────────────────────────────────────────────
    /// <summary>
    /// 攻撃アニメーションの任意タイミングでダメージ判定 + SE を発火する。
    ///
    /// 使い方:
    ///   1. AnimatorController で攻撃ステート（Attack1, WaveAtk 等）を選択
    ///   2. Inspector 右下「Add Behaviour」→ AttackEventBehaviour
    ///   3. Hit Events の要素を追加し、各項目を設定
    ///      - Time      : スライダーで 0〜1 を指定
    ///      - Damage    : このヒットのダメージ量
    ///      - Range     : 判定半径
    ///      - Offset    : 判定球の位置（キャラ前方ローカル）
    ///      - Play Sound: スイング SE を鳴らすか
    ///
    ///  ヒット SE（PlayHit）はダメージが通った瞬間に ComboAttack 側で鳴らす。
    /// </summary>
    public class AttackEventBehaviour : StateMachineBehaviour
    {
        [SerializeField] public AttackHitEvent[] hitEvents = { new AttackHitEvent() };

        // ── 実行時の状態 ─────────────────────────────────────────
        bool[]      fired;
        ComboAttack comboAttack;

        public override void OnStateEnter(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            // 親 GameObject (Player) の ComboAttack を取得
            if (comboAttack == null)
                comboAttack = animator.GetComponentInParent<ComboAttack>();

            fired = new bool[hitEvents.Length];
        }

        public override void OnStateUpdate(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            if (hitEvents == null || hitEvents.Length == 0) return;
            if (fired == null || fired.Length != hitEvents.Length)
                fired = new bool[hitEvents.Length];

            // ループアニメでも 1 サイクル 1 発にするため % 1f
            float t = stateInfo.normalizedTime % 1f;

            for (int i = 0; i < hitEvents.Length; i++)
            {
                var ev = hitEvents[i];
                if (!fired[i] && t >= ev.time)
                {
                    fired[i] = true;
                    Fire(animator.transform, ev);
                }
            }
        }

        public override void OnStateExit(
            Animator animator, AnimatorStateInfo stateInfo, int layerIndex)
        {
            fired = null;
        }

        // ── ヒット発火 ────────────────────────────────────────────
        void Fire(Transform animatorRoot, AttackHitEvent ev)
        {
            // スイング SE
            if (ev.playSound)
                AudioManager.Instance?.PlayAttack();

            // 判定球の中心（ローカル offset → ワールド座標）
            var root   = comboAttack != null ? comboAttack.transform : animatorRoot;
            var center = root.TransformPoint(ev.offset);

            // 範囲内の Collider を検索してダメージ
            int hitCount = 0;
            var hits     = Physics.OverlapSphere(center, ev.range);
            foreach (var col in hits)
            {
                var hp = col.GetComponentInParent<Health>();
                if (hp == null || !hp.IsAlive) continue;

                // 自分自身は除外
                if (comboAttack != null && col.transform.IsChildOf(comboAttack.transform)) continue;
                if (comboAttack != null && col.transform == comboAttack.transform)          continue;

                hp.TakeDamage(ev.damage);
                hitCount++;

                AudioManager.Instance?.PlayHit();

                // ヒットストップ・カメラシェイク・ノックバック
                float power = Mathf.Clamp01(ev.damage / 50f);
                HitStopManager.Instance?.Trigger(Mathf.Lerp(0.04f, 0.10f, power));
                CameraShake.Instance?.Shake(0.12f, Mathf.Lerp(0.05f, 0.20f, power));

                var dir = (col.transform.position - root.position).normalized;
                dir.y   = 0;
                col.GetComponentInParent<KnockbackReceiver>()?
                    .ApplyKnockback(dir, Mathf.Lerp(2f, 5f, power));

                EffectManager.Instance?.SpawnHit(col.transform.position);
            }

            // Gizmo 表示（Comboattack 側に委譲）
            comboAttack?.ShowHitGizmoExternal(center, ev.range, hitCount > 0);
        }
    }
}
