using UnityEngine;

namespace ActionGame
{
    /// <summary>
    /// 見下ろし型アクションゲーム用プレイヤー移動コントローラー。
    /// カメラは固定（Inspector でアサイン）。
    /// PC: WASD / モバイル: VirtualJoystick。
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public class TopDownPlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float moveSpeed = 6f;
        [SerializeField] float gravity   = -20f;
        [SerializeField] float rotateSpeed = 20f;

        [Header("Camera (Fixed Top-Down)")]
        [SerializeField] Transform cameraTransform;  // Inspector でアサイン
        [SerializeField] float cameraHeight   = 14f;
        [SerializeField] float cameraDistance = 8f;
        [SerializeField] float cameraTilt     = 55f;  // 俯瞰角度(度)

        CharacterController cc;
        DodgeController     dodge;
        Vector3 verticalVelocity;
        bool isAlive = true;

        void Awake()
        {
            cc    = GetComponent<CharacterController>();
            dodge = GetComponent<DodgeController>();
            var health = GetComponent<Health>();
            health.OnDeath += () => isAlive = false;
            health.OnDeath += OnPlayerDeath;
        }

        void Start()
        {
            // カメラが未設定なら Main Camera を使う
            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;

            SetupCamera();
        }

        void SetupCamera()
        {
            if (cameraTransform == null) return;
            // プレイヤー真後ろ上方の固定位置へ
            cameraTransform.position = transform.position
                + Vector3.up * cameraHeight
                + Vector3.back * cameraDistance;
            cameraTransform.rotation = Quaternion.Euler(cameraTilt, 0f, 0f);
        }

        void Update()
        {
            if (!isAlive) return;
            UpdateMovement();
            FollowCamera();
        }

        void UpdateMovement()
        {
            if (InputHandler.Instance == null) return;
            // ドッジ参照を遅延取得（初期化順ズレ対策）
            if (dodge == null) dodge = GetComponent<DodgeController>();
            // ドッジ中 or 後硬直中は移動制御を DodgeController に委譲
            if (dodge != null && dodge.IsControlLocked) return;

            var input = InputHandler.Instance.MoveInput;

            // ワールド空間での移動方向（カメラのX-Z平面基準）
            var moveDir = new Vector3(input.x, 0f, input.y).normalized;

            // キャラクターを移動方向に回転
            if (moveDir.sqrMagnitude > 0.01f)
            {
                var targetRot = Quaternion.LookRotation(moveDir);
                transform.rotation = Quaternion.Slerp(
                    transform.rotation, targetRot, rotateSpeed * Time.deltaTime);
            }

            // 重力
            if (cc.isGrounded)
                verticalVelocity.y = -2f;
            verticalVelocity.y += gravity * Time.deltaTime;

            cc.Move((moveDir * moveSpeed + verticalVelocity) * Time.deltaTime);
        }

        void FollowCamera()
        {
            if (cameraTransform == null) return;
            // XZ はプレイヤーに追従、Y と角度は固定
            var target = new Vector3(
                transform.position.x,
                transform.position.y + cameraHeight,
                transform.position.z - cameraDistance);
            cameraTransform.position = Vector3.Lerp(
                cameraTransform.position, target, 10f * Time.deltaTime);
        }

        void OnPlayerDeath()
        {
            AudioManager.Instance?.PlayPlayerDeath();
            EffectManager.Instance?.SpawnPlayerDeath(transform.position);
        }
    }
}
