using UnityEngine;
using UnityEngine.InputSystem;

namespace ActionGame
{
    /// <summary>
    /// Player の移動・ジャンプ・三人称カメラ制御。
    /// 新 Input System (Keyboard / Mouse) を使用。
    /// 必要コンポーネント: CharacterController
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

        CharacterController cc;
        Transform cam;
        Vector3 verticalVelocity;
        float yaw;
        float pitch = 25f;
        bool isAlive = true;

        void Awake()
        {
            cc = GetComponent<CharacterController>();
            cam = Camera.main != null ? Camera.main.transform : null;

            var hp = GetComponent<Health>();
            hp.OnDeath += () => SetAlive(false);
        }

        void Start()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
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
            if (cam == null || Mouse.current == null) return;

            var delta = Mouse.current.delta.ReadValue() * mouseSensitivity;
            yaw   += delta.x;
            pitch  = Mathf.Clamp(pitch - delta.y, -15f, 60f);

            var rot = Quaternion.Euler(pitch, yaw, 0f);
            cam.position = transform.position + rot * new Vector3(0f, cameraHeight, -cameraDistance);
            cam.LookAt(transform.position + Vector3.up * 1.5f);
        }

        // ---- Movement ----

        void UpdateMovement()
        {
            if (Keyboard.current == null || cam == null) return;

            float h = (Keyboard.current.dKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.aKey.isPressed ? 1f : 0f);
            float v = (Keyboard.current.wKey.isPressed ? 1f : 0f)
                    - (Keyboard.current.sKey.isPressed ? 1f : 0f);

            // カメラ方向基準で移動
            var camForward = cam.forward; camForward.y = 0f; camForward.Normalize();
            var camRight   = cam.right;   camRight.y   = 0f; camRight.Normalize();

            var moveDir = (camForward * v + camRight * h).normalized;

            if (moveDir.sqrMagnitude > 0.01f)
                transform.rotation = Quaternion.Slerp(transform.rotation,
                    Quaternion.LookRotation(moveDir), 15f * Time.deltaTime);

            // 重力・ジャンプ
            if (cc.isGrounded)
            {
                verticalVelocity.y = -2f;
                if (Keyboard.current.spaceKey.wasPressedThisFrame)
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
                Cursor.visible = true;
            }
        }
    }
}
