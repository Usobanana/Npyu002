using UnityEngine;
using UnityEngine.InputSystem;

namespace ActionGame
{
    /// <summary>
    /// PC (Keyboard/Mouse/Gamepad) とモバイル (VirtualJoystick) の入力を統一する Singleton。
    /// PlayerController / PlayerCombat はこのクラスのみ参照する。
    /// </summary>
    public class InputHandler : MonoBehaviour
    {
        public static InputHandler Instance { get; private set; }

        // ---- 外部から読み取る入力値 ----
        /// <summary>移動方向 (-1〜1, normalized 済み)</summary>
        public Vector2 MoveInput    { get; private set; }
        /// <summary>攻撃入力（このフレームに押された）</summary>
        public bool    AttackPressed { get; private set; }
        /// <summary>ジャンプ入力（このフレームに押された）</summary>
        public bool    JumpPressed   { get; private set; }
        /// <summary>ポーズ入力（このフレームに押された）</summary>
        public bool    PausePressed  { get; private set; }

        // ---- モバイル UI からセット ----
        [HideInInspector] public VirtualJoystick joystick;
        bool mobileAttackThisFrame;
        bool mobileJumpThisFrame;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Update()
        {
            MoveInput    = GetMoveInput();
            AttackPressed = GetAttackInput();
            JumpPressed   = GetJumpInput();
            PausePressed  = GetPauseInput();

            // モバイルフラグをリセット
            mobileAttackThisFrame = false;
            mobileJumpThisFrame   = false;
        }

        // ---- Move ----
        Vector2 GetMoveInput()
        {
            // Keyboard
            var kb = Keyboard.current;
            Vector2 kbInput = Vector2.zero;
            if (kb != null)
            {
                float h = (kb.dKey.isPressed ? 1f : 0f) - (kb.aKey.isPressed ? 1f : 0f);
                float v = (kb.wKey.isPressed ? 1f : 0f) - (kb.sKey.isPressed ? 1f : 0f);
                kbInput = new Vector2(h, v);
            }

            // Gamepad
            var gp = Gamepad.current;
            Vector2 gpInput = gp != null ? gp.leftStick.ReadValue() : Vector2.zero;

            // Virtual joystick (mobile)
            Vector2 joyInput = joystick != null ? joystick.Direction : Vector2.zero;

            // 最大値を採用
            Vector2 best = kbInput;
            if (gpInput.magnitude  > best.magnitude) best = gpInput;
            if (joyInput.magnitude > best.magnitude) best = joyInput;

            return best.magnitude > 1f ? best.normalized : best;
        }

        // ---- Attack ----
        bool GetAttackInput()
        {
            bool mouse   = Mouse.current  != null && Mouse.current.leftButton.wasPressedThisFrame;
            bool gamepad = Gamepad.current != null && Gamepad.current.buttonWest.wasPressedThisFrame;
            return mouse || gamepad || mobileAttackThisFrame;
        }

        // ---- Jump ----
        bool GetJumpInput()
        {
            bool kb      = Keyboard.current != null && Keyboard.current.spaceKey.wasPressedThisFrame;
            bool gamepad = Gamepad.current  != null && Gamepad.current.buttonSouth.wasPressedThisFrame;
            return kb || gamepad || mobileJumpThisFrame;
        }

        // ---- Pause ----
        bool GetPauseInput()
        {
            bool kb      = Keyboard.current != null && Keyboard.current.escapeKey.wasPressedThisFrame;
            bool gamepad = Gamepad.current  != null && Gamepad.current.startButton.wasPressedThisFrame;
            return kb || gamepad;
        }

        // ---- Mobile buttons (UI から呼ぶ) ----
        public void OnMobileAttack() => mobileAttackThisFrame = true;
        public void OnMobileJump()   => mobileJumpThisFrame   = true;
    }
}
