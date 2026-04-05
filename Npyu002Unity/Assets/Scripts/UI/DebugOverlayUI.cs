using UnityEngine;
using UnityEngine.InputSystem;

namespace ActionGame
{
    /// <summary>
    /// 画面左上に表示するデバッグ情報オーバーレイ。
    ///
    ///   表示内容:
    ///     - FPS（0.5 秒平均、色分け）
    ///     - Player HP / MaxHP + ゲージ
    ///     - 現在再生中のアニメーションクリップ名 + 進行率
    ///     - ドッジ状態（実行中 / CD残り秒 / Ready）
    ///     - コンボ段数（Light / Strong）
    ///     - Wave Special クールダウン
    ///     - 無敵フレーム中かどうか
    ///
    ///   F1 キーで表示 / 非表示を切り替え。
    ///   Canvas 不要。OnGUI で描画するため即アタッチして動作する。
    /// </summary>
    public class DebugOverlayUI : MonoBehaviour
    {
        [Header("表示設定")]
        [SerializeField] bool    showInBuild = false;       // ビルドでも表示する場合 true
        // F1 固定（新 Input System / Keyboard.current 使用）
        [SerializeField] float   panelWidth  = 280f;

        // ── Player 参照 ───────────────────────────────────────────────
        Health          playerHealth;
        Animator        playerAnim;
        DodgeController dodge;
        ComboAttack     combo;

        // ── FPS カウンタ ──────────────────────────────────────────────
        float fps        = 0f;
        float fpsTimer   = 0f;
        int   frameCount = 0;

        // ── 表示状態 ──────────────────────────────────────────────────
        bool visible = true;

        // ── GUI スタイル（遅延初期化）────────────────────────────────
        GUIStyle styleBox;
        GUIStyle styleLabel;
        GUIStyle styleFpsGood;   // ≥ 55 fps : 緑
        GUIStyle styleFpsWarn;   // ≥ 30 fps : 黄
        GUIStyle styleFpsBad;    // < 30 fps : 赤
        GUIStyle styleSectionHdr;
        bool stylesReady = false;

        // ─────────────────────────────────────────────────────────────
        void Start()
        {
            var playerGO = GameObject.FindGameObjectWithTag("Player");
            if (playerGO != null)
            {
                playerHealth = playerGO.GetComponent<Health>();
                playerAnim   = playerGO.GetComponentInChildren<Animator>();
                dodge        = playerGO.GetComponent<DodgeController>();
                combo        = playerGO.GetComponent<ComboAttack>();
            }
        }

        void Update()
        {
            // FPS (0.5 秒ごとに更新)
            frameCount++;
            fpsTimer += Time.unscaledDeltaTime;
            if (fpsTimer >= 0.5f)
            {
                fps        = frameCount / fpsTimer;
                frameCount = 0;
                fpsTimer   = 0f;
            }

            // 新 Input System で F1 トグル
            if (Keyboard.current != null && Keyboard.current.f1Key.wasPressedThisFrame)
                visible = !visible;
        }

        void OnGUI()
        {
#if !UNITY_EDITOR
            if (!showInBuild) return;
#endif
            if (!visible) return;

            InitStyles();

            const float PAD = 10f;
            const float LH  = 22f;   // 1行の高さ
            const float GAP = 6f;    // セクション間余白

            // ── 行数を先に計算してパネル高さを決める ─────────────────
            int lines = 1;                       // FPS
            lines += 1;                          // ── Player ──
            if (playerHealth != null) lines += 2;// HP行 + HPバー行
            if (playerAnim   != null) lines += 1;// Anim
            if (dodge        != null) lines += 1;// Dodge
            if (combo        != null) lines += 2;// Combo + Special
            lines += 1;                          // 操作ヒント

            float panelH = PAD + lines * LH + GAP * 3 + PAD;

            // ── 背景パネル ────────────────────────────────────────────
            GUI.Box(new Rect(PAD, PAD, panelWidth, panelH), GUIContent.none, styleBox);

            float cx = PAD + 12f;
            float cy = PAD + 10f;

            // ── FPS ──────────────────────────────────────────────────
            var fpsStyle = fps >= 55f ? styleFpsGood
                         : fps >= 30f ? styleFpsWarn
                                      : styleFpsBad;
            string fpsStr = $"FPS : {fps:F1}  [{MakeBar(Mathf.Clamp01(fps / 60f), 10)}]";
            GUI.Label(MakeRect(cx, cy, panelWidth), fpsStr, fpsStyle);
            cy += LH + GAP;

            // ── Player セクション ─────────────────────────────────────
            GUI.Label(MakeRect(cx, cy, panelWidth), "── Player ──────────────────", styleSectionHdr);
            cy += LH;

            // HP
            if (playerHealth != null)
            {
                float ratio = playerHealth.NormalizedHP;
                string hpLine = $"HP   : {playerHealth.CurrentHP:F0} / {playerHealth.MaxHP:F0}";
                GUI.Label(MakeRect(cx, cy, panelWidth), hpLine, styleLabel);
                cy += LH;

                // HP ゲージ
                string hpBar  = $"       [{MakeBar(ratio, 20)}] {ratio * 100f:F0}%";
                GUI.Label(MakeRect(cx, cy, panelWidth), hpBar, ratio < 0.3f ? styleFpsBad : styleLabel);
                cy += LH;
            }

            // アニメーション
            if (playerAnim != null && playerAnim.runtimeAnimatorController != null)
            {
                var    clips     = playerAnim.GetCurrentAnimatorClipInfo(0);
                string clipName  = clips.Length > 0 ? clips[0].clip.name : "─";
                var    stateInfo = playerAnim.GetCurrentAnimatorStateInfo(0);
                float  progress  = Mathf.Clamp01(stateInfo.normalizedTime);
                string animLine  = $"Anim : {clipName}  {progress * 100f:F0}%";
                GUI.Label(MakeRect(cx, cy, panelWidth), animLine, styleLabel);
                cy += LH;
            }

            // ドッジ
            if (dodge != null)
            {
                string dodgeStr;
                if (dodge.IsDodging)
                    dodgeStr = "<color=#00FFFF>● Dodging</color>";
                else if (dodge.CooldownRemaining > 0.01f)
                    dodgeStr = $"CD {dodge.CooldownRemaining:F1}s  [{MakeBar(1f - dodge.CooldownNormalized, 8)}]";
                else
                    dodgeStr = "<color=#44FF44>● Ready</color>";
                GUI.Label(MakeRect(cx, cy, panelWidth), $"Dodge: {dodgeStr}", styleLabel);
                cy += LH;
            }

            // コンボ
            if (combo != null)
            {
                string comboStr;
                if (combo.IsLightAttacking)
                    comboStr = $"<color=#FFAA00>Light  Lv.{combo.LightStep} / {combo.LightStepMax}</color>";
                else if (combo.IsStrongAttacking)
                    comboStr = $"<color=#FF6600>Strong Lv.{combo.StrongStep} / {combo.StrongStepMax}</color>";
                else
                    comboStr = "─";
                GUI.Label(MakeRect(cx, cy, panelWidth), $"Combo: {comboStr}", styleLabel);
                cy += LH;

                // Wave Special
                float spNorm = combo.SpecialCooldownNormalized;
                string spStr = spNorm > 0f
                    ? $"CD {spNorm * 100f:F0}%  [{MakeBar(1f - spNorm, 8)}]"
                    : "<color=#44FF44>● Ready</color>";
                GUI.Label(MakeRect(cx, cy, panelWidth), $"Wave : {spStr}", styleLabel);
                cy += LH + GAP;
            }

            // 操作ヒント
            GUI.Label(MakeRect(cx, cy, panelWidth),
                "<color=#888888>[F1] Hide</color>", styleLabel);
        }

        // ── ヘルパー ──────────────────────────────────────────────────

        static Rect MakeRect(float x, float y, float panelW) =>
            new Rect(x, y, panelW - 24f, 22f);

        /// <summary>ASCII ゲージ  [████░░░░]</summary>
        static string MakeBar(float ratio, int cells)
        {
            int filled = Mathf.RoundToInt(Mathf.Clamp01(ratio) * cells);
            var sb = new System.Text.StringBuilder(cells);
            for (int i = 0; i < cells; i++)
                sb.Append(i < filled ? '█' : '░');
            return sb.ToString();
        }

        void InitStyles()
        {
            if (stylesReady) return;
            stylesReady = true;

            // 背景ボックス
            styleBox = new GUIStyle(GUI.skin.box);
            var bgTex = new Texture2D(1, 1);
            bgTex.SetPixel(0, 0, new Color(0f, 0f, 0f, 0.72f));
            bgTex.Apply();
            styleBox.normal.background = bgTex;
            styleBox.border            = new RectOffset(4, 4, 4, 4);

            // 基本ラベル
            styleLabel = new GUIStyle(GUI.skin.label)
            {
                fontSize  = 13,
                richText  = true,
                alignment = TextAnchor.MiddleLeft,
            };
            styleLabel.normal.textColor = Color.white;

            // セクション見出し
            styleSectionHdr = new GUIStyle(styleLabel)
            {
                fontSize = 12,
            };
            styleSectionHdr.normal.textColor = new Color(0.6f, 0.9f, 1f);

            // FPS 色
            styleFpsGood = new GUIStyle(styleLabel);
            styleFpsGood.normal.textColor = new Color(0.3f, 1f, 0.3f);

            styleFpsWarn = new GUIStyle(styleLabel);
            styleFpsWarn.normal.textColor = new Color(1f, 0.9f, 0.1f);

            styleFpsBad = new GUIStyle(styleLabel);
            styleFpsBad.normal.textColor = new Color(1f, 0.25f, 0.2f);
        }
    }
}
