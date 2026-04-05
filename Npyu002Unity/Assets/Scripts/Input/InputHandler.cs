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
        /// <summary>ドッジ入力（このフレームに押された）</summary>
        public bool    DodgePressed        { get; private set; }
        /// <summary>強攻撃入力（右クリック / ゲームパッド Y）</summary>
        public bool    StrongAttackPressed { get; private set; }
        /// <summary>スペシャル入力（E キー / ゲームパッド LB）</summary>
        public bool    SpecialPressed      { get; private set; }
        /// <summary>攻撃ボタン押しっぱなし（オートコンボ用）</summary>
        public bool    AttackHeld       { get; private set; }
        /// <summary>強攻撃ボタン押しっぱなし（オートコンボ用）</summary>
        public bool    StrongAttackHeld { get; private set; }

        // ---- モバイル UI からセット ----
        [HideInInspector] public VirtualJoystick joystick;
        bool mobileAttackThisFrame;
        bool mobileJumpThisFrame;
        bool mobileDodgeThisFrame;
        bool mobileStrongAttackThisFrame;
        bool mobileSpecialThisFrame;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Update()
        {
            MoveInput           = GetMoveInput();
            AttackPressed       = GetAttackInput();
            JumpPressed         = GetJumpInput();
            PausePressed        = GetPauseInput();
            DodgePressed        = GetDodgeInput();
            StrongAttackPressed = GetStrongAttackInput();
            SpecialPressed      = GetSpecialInput();
            AttackHeld          = Mouse.current  != null && Mouse.current.leftButton.isPressed
                               || Gamepad.current != null && Gamepad.current.buttonWest.isPressed;
            StrongAttackHeld    = Mouse.current  != null && Mouse.current.rightButton.isPressed
                               || Gamepad.current != null && Gamepad.current.buttonNorth.isPressed;

            // モバイルフラグをリセット
            mobileAttackThisFrame       = false;
            mobileJumpThisFrame         = false;
            mobileDodgeThisFrame        = false;
            mobileStrongAttackThisFrame = false;
            mobileSpecialThisFrame      = false;
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

        // ---- Dodge ----
        bool GetDodgeInput()
        {
            bool kb      = Keyboard.current != null && Keyboard.current.leftShiftKey.wasPressedThisFrame;
            bool gamepad = Gamepad.current  != null && Gamepad.current.buttonEast.wasPressedThisFrame;
            return kb || gamepad || mobileDodgeThisFrame;
        }

        // ---- Strong Attack ----
        bool GetStrongAttackInput()
        {
            bool mouse   = Mouse.current  != null && Mouse.current.rightButton.wasPressedThisFrame;
            bool gamepad = Gamepad.current != null && Gamepad.current.buttonNorth.wasPressedThisFrame;
            return mouse || gamepad || mobileStrongAttackThisFrame;
        }

        // ---- Special ----
        bool GetSpecialInput()
        {
            bool kb      = Keyboard.current != null && Keyboard.current.eKey.wasPressedThisFrame;
            bool gamepad = Gamepad.current  != null && Gamepad.current.leftShoulder.wasPressedThisFrame;
            return kb || gamepad || mobileSpecialThisFrame;
        }

        // ---- Mobile buttons (UI から呼ぶ) ----
        public void OnMobileAttack()       => mobileAttackThisFrame       = true;
        public void OnMobileJump()         => mobileJumpThisFrame         = true;
        public void OnMobileDodge()        => mobileDodgeThisFrame        = true;
        public void OnMobileStrongAttack() => mobileStrongAttackThisFrame = true;
        public void OnMobileSpecial()      => mobileSpecialThisFrame      = true;
    }
}
