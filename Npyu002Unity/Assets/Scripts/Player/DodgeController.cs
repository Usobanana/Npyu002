using UnityEngine;
using System.Collections;

namespace ActionGame
{
    /// <summary>
    /// ドッジ（回避）システム。
    /// Shift キー / ゲームパッド B ボタンで移動方向にダッシュ回避。
    /// ドッジ中は無敵フレームが付与される。
    /// Animator パラメータ: Dodge (Trigger)
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public class DodgeController : MonoBehaviour
    {
        [Header("Dodge Settings")]
        [SerializeField] float dodgeSpeed    = 14f;   // ダッシュ速度
        [SerializeField] float dodgeDuration = 0.35f; // ドッジ時間（秒）
        [SerializeField] float dodgeCooldown = 1.0f;  // クールダウン（秒）
        [SerializeField] bool  invincible       = true;  // 無敵フレーム付与
        [SerializeField] float postDodgeLockout = 0.15f; // ドッジ後の移動抑制時間（秒）

        /// <summary>ドッジ中かどうか（TopDownPlayerController が参照）</summary>
        public bool IsDodging { get; private set; }

        /// <summary>ドッジ中 or 後硬直中（入力を受け付けない期間）</summary>
        public bool IsControlLocked => IsDodging || lockoutTimer > 0f;

        /// <summary>クールダウン残り時間 0〜1（UI 表示用）</summary>
        public float CooldownNormalized => Mathf.Clamp01(cooldownTimer / dodgeCooldown);

        /// <summary>クールダウン残り秒数（UI 表示用）</summary>
        public float CooldownRemaining => cooldownTimer;

        float cooldownTimer;
        float lockoutTimer;
        CharacterController cc;
        Animator            anim;
        Health              health;

        void Awake()
        {
            cc     = GetComponent<CharacterController>();
            anim   = GetComponentInChildren<Animator>();
            health = GetComponent<Health>();
        }

        void Update()
        {
            if (cooldownTimer > 0f) cooldownTimer -= Time.deltaTime;
            if (lockoutTimer  > 0f) lockoutTimer  -= Time.deltaTime;
            if (InputHandler.Instance == null || IsDodging || cooldownTimer > 0f) return;

            if (InputHandler.Instance.DodgePressed)
                StartDodge();
        }

        void StartDodge()
        {
            // 移動入力がある方向、なければ正面にドッジ
            var input  = InputHandler.Instance.MoveInput;
            var dir    = input.sqrMagnitude > 0.01f
                ? new Vector3(input.x, 0f, input.y).normalized
                : transform.forward;

            // ドッジ方向にキャラを向ける
            transform.rotation = Quaternion.LookRotation(dir);

            // アニメーション
            if (anim != null && anim.runtimeAnimatorController != null)
                anim.SetTrigger("Dodge");

            StartCoroutine(DodgeCoroutine(dir));
        }

        IEnumerator DodgeCoroutine(Vector3 dir)
        {
            IsDodging    = true;
            cooldownTimer = dodgeCooldown;

            if (invincible && health != null)
                health.SetInvincible(true);

            float elapsed = 0f;
            while (elapsed < dodgeDuration)
            {
                // 徐々に減速するダッシュ
                float t     = elapsed / dodgeDuration;
                float speed = Mathf.Lerp(dodgeSpeed, 0f, t);
                cc.Move(dir * speed * Time.deltaTime);

                elapsed += Time.deltaTime;
                yield return null;
            }

            if (invincible && health != null)
                health.SetInvincible(false);

            IsDodging    = false;
            lockoutTimer = postDodgeLockout; // ドッジ直後の後硬直
        }

        void OnDrawGizmosSelected()
        {
            // クールダウン可視化（Sceneビュー）
            if (cooldownTimer > 0f)
            {
                Gizmos.color = new Color(0f, 0.8f, 1f, 0.3f);
                Gizmos.DrawWireSphere(transform.position, 0.6f);
            }
        }
    }
}
