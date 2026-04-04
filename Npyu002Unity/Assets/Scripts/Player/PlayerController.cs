using UnityEngine;

namespace ActionGame
{
    /// <summary>
    /// Player の移動・ジャンプ・三人称カメラ制御。
    /// 入力は InputHandler から取得（PC / Mobile / Gamepad 共通）。
    /// モバイルではカメラが Player の向きを自動追従する。
    /// 必要コンポーネント: CharacterController, Health
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(Health))]
    public class PlayerController : MonoBehaviour
    {
        [Header("Movement")]
        [SerializeField] float moveSpeed = 5f;
        [SerializeField] float jumpHeight = 2f;
        [SerializeField] float gravity = -20f;

        [Header("Camera")]
        [SerializeField] float mouseSensitivity = 0.2f;
        [SerializeField] float cameraDistance = 5f;
        [SerializeField] float cameraHeight = 1.5f;
        [SerializeField] float cameraAutoFollowSpeed = 5f;   // モバイル時の追従速度

        CharacterController cc;
        Transform cam;
        Vector3 verticalVelocity;
        float yaw;
        float pitch = 25f;
        bool isAlive = true;

        // モバイル判定（ビルド時の簡易判定）
        bool IsMobile => Application.isMobilePlatform;

        void Awake()
        {
            cc  = GetComponent<CharacterController>();
            cam = Camera.main != null ? Camera.main.transform : null;

            GetComponent<Health>().OnDeath += () => SetAlive(false);
        }

        void Start()
        {
            if (!IsMobile)
            {
                Cursor.lockState = CursorLockMode.Locked;
                Cursor.visible   = false;
            }
        }

        void Update()
        {
            if (!isAlive) return;
            UpdateCamera();
            UpdateMovement();
        }

        // ---- Camera ----

        void UpdateCamera()
        {
            if (cam == null) return;

            if (IsMobile)
            {
                // モバイル: Player の向きにカメラを自動追従
                yaw   = Mathf.LerpAngle(yaw, transform.eulerAngles.y, cameraAutoFollowSpeed * Time.deltaTime);
                pitch = 25f;
            }
            else
            {
                // PC: マウスでカメラ回転
                var delta = UnityEngine.InputSystem.Mouse.current != null
                    ? UnityEngine.InputSystem.Mouse.current.delta.ReadValue() * mouseSensitivity
                    : Vector2.zero;
                yaw   += delta.x;
                pitch  = Mathf.Clamp(pitch - delta.y, -15f, 60f);
            }

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            cam.position = transform.position + rot * new Vector3(0f, cameraHeight, -cameraDistance);
            cam.LookAt(transform.position + Vector3.up * 1.5f);
        }

        // ---- Movement ----

        void UpdateMovement()
        {
            if (cam == null || InputHandler.Instance == null) return;

            var input = InputHandler.Instance.MoveInput;

            var camForward = cam.forward; camForward.y = 0f; camForward.Normalize();
            var camRight   = cam.right;   camRight.y   = 0f; camRight.Normalize();

            var moveDir = (camForward * input.y + camRight * input.x).normalized;

            if (moveDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(moveDir), 15f * Time.deltaTime);

            // 重力・ジャンプ
            if (cc.isGrounded)
            {
                verticalVelocity.y = -2f;
                if (InputHandler.Instance.JumpPressed)
                    verticalVelocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
            }
            verticalVelocity.y += gravity * Time.deltaTime;

            cc.Move((moveDir * moveSpeed + verticalVelocity) * Time.deltaTime);
        }

        // ---- Public API ----

        public void SetAlive(bool alive)
        {
            isAlive = alive;
            if (!alive)
            {
                Cursor.lockState = CursorLockMode.None;
                Cursor.visible   = true;
            }
        }
    }
}
