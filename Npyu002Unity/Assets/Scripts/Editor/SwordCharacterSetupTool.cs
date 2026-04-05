using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;
using AnimatorClip = UnityEngine.AnimationClip;

namespace ActionGame.Editor
{
    /// <summary>
    /// SwordAnimationPack を SwordGame シーンに反映するセットアップツール。
    ///   1. AnimatorController を自動生成
    ///   2. Player を 9CG_Sword Prefab に差し替え
    ///   3. 必要コンポーネントを自動追加
    /// Tools > ActionGame > Setup Sword Character
    /// </summary>
    public static class SwordCharacterSetupTool
    {
        // ── アニメクリップパス ─────────────────────────────────────────
        const string Base = "Assets/SwordAnimationPack/Animation/Humanoid/";

        const string ClipIdle      = Base + "01_Idle/Idle_Combat.anim";
        const string ClipRun       = Base + "04_Run/02_Run_Combat_RM/01_Run_Combat_F_0_RM/Run_Combat_Loop_F_0_RM.anim";
        const string ClipAttack1   = Base + "02_Attack/01_Combo_Attack_01/Combo_Attack_01_01.anim";
        const string ClipAttack2   = Base + "02_Attack/01_Combo_Attack_01/Combo_Attack_01_02.anim";
        const string ClipAttack3   = Base + "02_Attack/01_Combo_Attack_01/Combo_Attack_01_03.anim";
        const string ClipHitReact  = Base + "08_Hit/01_Hit/Hit_F.anim";
        const string ClipDeath     = Base + "08_Hit/01_Hit/Hit_Death.anim";

        const string CharPrefabPath  = "Assets/SwordAnimationPack/Prefabs/9CG_Sword.prefab";
        const string ControllerSave  = "Assets/Animations/SwordPlayerController.controller";

        // ── 1. AnimatorController 生成 ────────────────────────────────
        [MenuItem("Tools/ActionGame/Create Sword AnimatorController")]
        public static AnimatorController CreateController()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");

            var ctrl = AnimatorController.CreateAnimatorControllerAtPath(ControllerSave);

            // パラメータ追加
            ctrl.AddParameter("Speed",     AnimatorControllerParameterType.Float);
            ctrl.AddParameter("Attack1",   AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack2",   AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("Attack3",   AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("HitReact",  AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("IsDead",    AnimatorControllerParameterType.Bool);

            var layer = ctrl.layers[0];
            var sm    = layer.stateMachine;

            // ── ステート作成 ──────────────────────────────────────────
            var stIdle    = AddState(sm, "Idle",    ClipIdle,    new Vector3(250, 0));
            var stMove    = AddState(sm, "Move",    ClipRun,     new Vector3(250, 80));
            var stAtk1    = AddState(sm, "Attack1", ClipAttack1, new Vector3(500, -80));
            var stAtk2    = AddState(sm, "Attack2", ClipAttack2, new Vector3(500, 0));
            var stAtk3    = AddState(sm, "Attack3", ClipAttack3, new Vector3(500, 80));
            var stHit     = AddState(sm, "HitReact",ClipHitReact,new Vector3(500, 160));
            var stDeath   = AddState(sm, "Death",   ClipDeath,   new Vector3(500, 240));

            // Root Motion を無効化（CharacterController で移動制御するため）
            stMove.motion = LoadClip(ClipRun);
            stIdle.motion = LoadClip(ClipIdle);

            sm.defaultState = stIdle;

            // ── 遷移: Idle ↔ Move ────────────────────────────────────
            var t = stIdle.AddTransition(stMove);
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.duration = 0.1f;

            t = stMove.AddTransition(stIdle);
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            t.duration = 0.1f;

            // ── 遷移: Any → Attack1/2/3 ──────────────────────────────
            AddAnyTriggerTransition(sm, stAtk1, "Attack1", exitTime: false);
            AddAnyTriggerTransition(sm, stAtk2, "Attack2", exitTime: false);
            AddAnyTriggerTransition(sm, stAtk3, "Attack3", exitTime: false);

            // Attack → Idle（終了後）
            AddExitTransition(stAtk1, stIdle, 0.15f);
            AddExitTransition(stAtk2, stIdle, 0.15f);
            AddExitTransition(stAtk3, stIdle, 0.15f);

            // ── 遷移: Any → HitReact ────────────────────────────────
            AddAnyTriggerTransition(sm, stHit, "HitReact", exitTime: false);
            AddExitTransition(stHit, stIdle, 0.1f);

            // ── 遷移: Any → Death ────────────────────────────────────
            var tDeath = sm.AddAnyStateTransition(stDeath);
            tDeath.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            tDeath.duration = 0.15f;
            tDeath.canTransitionToSelf = false;

            ctrl.layers = new[] { layer };
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();

            Debug.Log("[SwordCharacterSetupTool] AnimatorController を生成: " + ControllerSave);
            return ctrl;
        }

        // ── 2. Player を 9CG_Sword に差し替え ───────────────────────────
        [MenuItem("Tools/ActionGame/Setup Sword Player in Scene")]
        public static void SetupSwordPlayer()
        {
            // AnimatorController がなければ先に作る
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerSave);
            if (ctrl == null)
            {
                ctrl = CreateController();
            }

            // 既存 Player を削除
            var oldPlayer = GameObject.Find("Player");
            if (oldPlayer != null)
            {
                var oldPos = oldPlayer.transform.position;
                Object.DestroyImmediate(oldPlayer);

                // 9CG_Sword プレハブをインスタンス化
                var charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CharPrefabPath);
                if (charPrefab == null)
                {
                    Debug.LogError("[SwordCharacterSetupTool] 9CG_Sword.prefab が見つかりません: " + CharPrefabPath);
                    return;
                }

                var playerGO = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab);
                playerGO.name = "Player";
                playerGO.tag  = "Player";
                playerGO.transform.position = oldPos;

                // AnimatorController と Avatar をアサイン（子オブジェクトの Animator を使う）
                var anim = playerGO.GetComponentInChildren<Animator>();
                if (anim == null) anim = playerGO.AddComponent<Animator>();
                anim.runtimeAnimatorController = ctrl;
                anim.applyRootMotion = false;  // CharacterController で移動制御

                // FBX から Avatar を取得してアサイン
                var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(
                    "Assets/SwordAnimationPack/Model/9CG_Sword.FBX");
                if (avatar != null)
                {
                    anim.avatar = avatar;
                    Debug.Log("[SwordCharacterSetupTool] Avatar をアサイン: " + avatar.name);
                }
                else
                {
                    Debug.LogWarning("[SwordCharacterSetupTool] Avatar が見つかりません。FBX の Rig 設定を確認してください。");
                }

                // CharacterController 追加
                if (playerGO.GetComponent<CharacterController>() == null)
                {
                    var cc = playerGO.AddComponent<CharacterController>();
                    cc.height = 1.8f;
                    cc.radius = 0.3f;
                    cc.center = new Vector3(0f, 0.9f, 0f);
                }

                // Health 追加
                if (playerGO.GetComponent<Health>() == null)
                {
                    var hp = playerGO.AddComponent<Health>();
                    var so = new SerializedObject(hp);
                    var p  = so.FindProperty("maxHP");
                    if (p != null) { p.floatValue = 100f; so.ApplyModifiedProperties(); }
                }

                // TopDownPlayerController 追加
                if (playerGO.GetComponent<TopDownPlayerController>() == null)
                    playerGO.AddComponent<TopDownPlayerController>();

                // ComboAttack 追加（Animator 参照を自動で拾う）
                if (playerGO.GetComponent<ComboAttack>() == null)
                    playerGO.AddComponent<ComboAttack>();

                // PlayerAnimationController 追加（Speed を Animator に流す）
                if (playerGO.GetComponent<PlayerAnimationController>() == null)
                    playerGO.AddComponent<PlayerAnimationController>();

                Debug.Log("[SwordCharacterSetupTool] Player を 9CG_Sword に差し替えました。");
            }
            else
            {
                Debug.LogWarning("[SwordCharacterSetupTool] Player が見つかりません。SwordGame シーンを開いてください。");
            }

            // シーンを保存
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[SwordCharacterSetupTool] 完了。Ctrl+S でシーンを保存してください。");
        }

        // ── 3. Dodge ステートを既存コントローラーに追加 ─────────────────
        const string ClipDodge = Base + "06_Dodge/02_Dodge_Combat/Dodge_Combat_F.anim";

        [MenuItem("Tools/ActionGame/Add Dodge to Controller")]
        public static void PatchDodge()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerSave);
            if (ctrl == null)
            {
                Debug.LogError("[SwordCharacterSetupTool] コントローラーが見つかりません。先に Create Sword AnimatorController を実行してください。");
                return;
            }

            // すでに Dodge パラメータがあればスキップ
            foreach (var p in ctrl.parameters)
                if (p.name == "Dodge") { Debug.Log("[SwordCharacterSetupTool] Dodge は既に追加済みです。"); return; }

            ctrl.AddParameter("Dodge", AnimatorControllerParameterType.Trigger);

            var layer = ctrl.layers[0];
            var sm    = layer.stateMachine;

            // Idle ステートの位置を参考に Dodge を配置
            var stDodge = AddState(sm, "Dodge", ClipDodge, new Vector3(250, 320));

            // AnyState → Dodge
            AddAnyTriggerTransition(sm, stDodge, "Dodge", exitTime: false);

            // Dodge → Idle（終了後）
            AddExitTransition(stDodge, sm.defaultState, 0.1f);

            // DodgeController コンポーネントを Player に追加
            var player = GameObject.Find("Player");
            if (player != null && player.GetComponent<DodgeController>() == null)
            {
                player.AddComponent<DodgeController>();
                Debug.Log("[SwordCharacterSetupTool] DodgeController を Player に追加しました。");
            }

            ctrl.layers = new[] { layer };
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            Debug.Log("[SwordCharacterSetupTool] Dodge ステートを AnimatorController に追加しました。Ctrl+S でシーンを保存してください。");
        }

        // ── 4. コンボ拡張ステートを追加 ──────────────────────────────────
        const string BaseAtk = Base + "02_Attack/";

        // Combo01 4段目（ライトコンボ延長）
        const string ClipAtk4    = BaseAtk + "01_Combo_Attack_01/Combo_Attack_01_04.anim";
        // Combo02（ストロングコンボ）
        const string ClipSAtk1   = BaseAtk + "02_Combo_Attack_02/Combo_Attack_02_01.anim";
        const string ClipSAtk2   = BaseAtk + "02_Combo_Attack_02/Combo_Attack_02_02.anim";
        const string ClipSAtk3   = BaseAtk + "02_Combo_Attack_02/Combo_Attack_02_03.anim";
        const string ClipSAtk4   = BaseAtk + "02_Combo_Attack_02/Combo_Attack_02_04.anim";
        // Wave Special
        const string ClipWaveAtk = BaseAtk + "05_Combo_Attack_Wave_05/Combo_Attack_Wave_05_01.anim";

        [MenuItem("Tools/ActionGame/Add Combo Expansion to Controller")]
        public static void PatchComboExpansion()
        {
            var ctrl = AssetDatabase.LoadAssetAtPath<AnimatorController>(ControllerSave);
            if (ctrl == null)
            {
                Debug.LogError("[SwordCharacterSetupTool] コントローラーが見つかりません。先に Create Sword AnimatorController を実行してください。");
                return;
            }

            // 重複チェック
            foreach (var p in ctrl.parameters)
                if (p.name == "Attack4") { Debug.Log("[SwordCharacterSetupTool] ComboExpansion は既に追加済みです。"); return; }

            // パラメータ追加
            ctrl.AddParameter("Attack4", AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("SAtk1",   AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("SAtk2",   AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("SAtk3",   AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("SAtk4",   AnimatorControllerParameterType.Trigger);
            ctrl.AddParameter("WaveAtk", AnimatorControllerParameterType.Trigger);

            var layer = ctrl.layers[0];
            var sm    = layer.stateMachine;
            var idle  = sm.defaultState;

            // ── ライトコンボ 4段目 ────────────────────────────────────
            var stAtk4  = AddState(sm, "Attack4", ClipAtk4,  new Vector3(500, 160));
            AddAnyTriggerTransition(sm, stAtk4, "Attack4", exitTime: false);
            AddExitTransition(stAtk4, idle, 0.15f);

            // ── ストロングコンボ（Combo02）────────────────────────────
            var stSAtk1 = AddState(sm, "SAtk1", ClipSAtk1, new Vector3(750, -160));
            var stSAtk2 = AddState(sm, "SAtk2", ClipSAtk2, new Vector3(750,  -80));
            var stSAtk3 = AddState(sm, "SAtk3", ClipSAtk3, new Vector3(750,    0));
            var stSAtk4 = AddState(sm, "SAtk4", ClipSAtk4, new Vector3(750,   80));

            AddAnyTriggerTransition(sm, stSAtk1, "SAtk1", exitTime: false);
            AddAnyTriggerTransition(sm, stSAtk2, "SAtk2", exitTime: false);
            AddAnyTriggerTransition(sm, stSAtk3, "SAtk3", exitTime: false);
            AddAnyTriggerTransition(sm, stSAtk4, "SAtk4", exitTime: false);

            AddExitTransition(stSAtk1, idle, 0.15f);
            AddExitTransition(stSAtk2, idle, 0.15f);
            AddExitTransition(stSAtk3, idle, 0.15f);
            AddExitTransition(stSAtk4, idle, 0.20f);

            // ── ウェーブスペシャル ────────────────────────────────────
            var stWave = AddState(sm, "WaveAtk", ClipWaveAtk, new Vector3(750, 160));
            AddAnyTriggerTransition(sm, stWave, "WaveAtk", exitTime: false);
            AddExitTransition(stWave, idle, 0.15f);

            ctrl.layers = new[] { layer };
            EditorUtility.SetDirty(ctrl);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();

            Debug.Log("[SwordCharacterSetupTool] コンボ拡張ステートを追加しました（Attack4 / SAtk1〜4 / WaveAtk）。Ctrl+S で保存してください。");
        }

        // ── 5. 打感システムをシーンに追加 ────────────────────────────────
        [MenuItem("Tools/ActionGame/Add Hit Feel to Scene")]
        public static void PatchHitFeel()
        {
            // HitStopManager を InputHandler と同じ GameObject に追加
            var inputGO = GameObject.Find("InputHandler");
            if (inputGO == null) inputGO = new GameObject("InputHandler");
            if (inputGO.GetComponent<HitStopManager>() == null)
            {
                inputGO.AddComponent<HitStopManager>();
                Debug.Log("[SwordCharacterSetupTool] HitStopManager を追加しました。");
            }

            // CameraShake を Main Camera に追加
            var cam = Camera.main;
            if (cam != null && cam.GetComponent<CameraShake>() == null)
            {
                cam.gameObject.AddComponent<CameraShake>();
                Debug.Log("[SwordCharacterSetupTool] CameraShake を Main Camera に追加しました。");
            }

            // KnockbackReceiver + HitFlash を EnemyBT を持つ全 GameObject に追加
            int enemyCount = 0;
            foreach (var enemy in Object.FindObjectsOfType<EnemyBT>())
            {
                if (enemy.GetComponent<KnockbackReceiver>() == null)
                    enemy.gameObject.AddComponent<KnockbackReceiver>();
                if (enemy.GetComponent<HitFlash>() == null)
                    enemy.gameObject.AddComponent<HitFlash>();
                enemyCount++;
            }
            if (enemyCount > 0)
                Debug.Log($"[SwordCharacterSetupTool] {enemyCount} 体の Enemy に KnockbackReceiver + HitFlash を追加しました。");

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[SwordCharacterSetupTool] 打感セットアップ完了。Ctrl+S でシーンを保存してください。");
        }

        // ── 6. HP バーをシーンに追加 ──────────────────────────────────────
        [MenuItem("Tools/ActionGame/Add HP Bars to Scene")]
        public static void PatchHPBars()
        {
            // ── Player HP バー（画面下・Screen Space）────────────────────
            var canvas = GameObject.Find("Canvas");
            if (canvas == null)
            {
                Debug.LogError("[SwordCharacterSetupTool] Canvas が見つかりません。");
                return;
            }

            // 既存チェック
            if (canvas.transform.Find("PlayerHPBar") == null)
            {
                // 背景パネル
                var bg     = new GameObject("PlayerHPBar");
                bg.transform.SetParent(canvas.transform, false);
                var bgImg  = bg.AddComponent<UnityEngine.UI.Image>();
                bgImg.color = new Color(0f, 0f, 0f, 0.6f);
                var bgRect = bg.GetComponent<RectTransform>();
                bgRect.anchorMin        = new Vector2(0.5f, 0f);
                bgRect.anchorMax        = new Vector2(0.5f, 0f);
                bgRect.pivot            = new Vector2(0.5f, 0f);
                bgRect.anchoredPosition = new Vector2(0f, 20f);
                bgRect.sizeDelta        = new Vector2(400f, 28f);

                // フィル（localScale.x = ratio 方式 / pivot 左端）
                var fill     = new GameObject("Fill");
                fill.transform.SetParent(bg.transform, false);
                var fillImg  = fill.AddComponent<UnityEngine.UI.Image>();
                fillImg.color = new Color(0.2f, 0.85f, 0.2f);
                var fillRect  = fill.GetComponent<RectTransform>();
                fillRect.pivot     = new Vector2(0f, 0.5f);
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;

                // HealthBarUI を bg にアタッチして Player の Health を接続
                var hpBarUI = bg.AddComponent<HealthBarUI>();
                var player  = GameObject.Find("Player");
                if (player != null)
                {
                    var so = new SerializedObject(hpBarUI);
                    // fill Image を slider 代わりに直接使う拡張へ対応
                    so.ApplyModifiedProperties();
                }

                // PlayerHPFillImage を HealthBarUI に渡す（SerializedObject 経由）
                {
                    var so = new SerializedObject(hpBarUI);
                    var pFill = so.FindProperty("fillImage");
                    if (pFill != null) { pFill.objectReferenceValue = fillImg; so.ApplyModifiedProperties(); }
                    var pTarget = so.FindProperty("target");
                    if (pTarget != null && GameObject.Find("Player") != null)
                    {
                        pTarget.objectReferenceValue = GameObject.Find("Player").GetComponent<Health>();
                        so.ApplyModifiedProperties();
                    }
                }

                Debug.Log("[SwordCharacterSetupTool] Player HP バーを Canvas に追加しました。");
            }

            // ── Enemy / Cube の頭上 WorldSpaceHPBar ──────────────────────
            int count = 0;
            foreach (var enemy in Object.FindObjectsOfType<EnemyBT>())
            {
                if (enemy.GetComponent<WorldSpaceHPBar>() == null)
                {
                    enemy.gameObject.AddComponent<WorldSpaceHPBar>();
                    count++;
                }
            }
            foreach (var cube in Object.FindObjectsOfType<ExplosiveCube>())
            {
                if (cube.GetComponent<WorldSpaceHPBar>() == null)
                {
                    var bar = cube.gameObject.AddComponent<WorldSpaceHPBar>();
                    var so  = new SerializedObject(bar);
                    var p   = so.FindProperty("offset");
                    if (p != null) { p.vector3Value = new Vector3(0f, 1.8f, 0f); so.ApplyModifiedProperties(); }
                    count++;
                }
            }

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log($"[SwordCharacterSetupTool] 頭上 HP バー {count} 個追加。Ctrl+S で保存してください。");
        }

        // ── 7. デバッグオーバーレイをシーンに追加 ────────────────────────
        [MenuItem("Tools/ActionGame/Add Debug Overlay to Scene")]
        public static void PatchDebugOverlay()
        {
            // 既存チェック
            if (Object.FindFirstObjectByType<DebugOverlayUI>() != null)
            {
                Debug.Log("[SwordCharacterSetupTool] DebugOverlayUI は既にシーンに存在します。");
                return;
            }

            var go = new GameObject("DebugOverlayUI");
            go.AddComponent<DebugOverlayUI>();

            UnityEditor.SceneManagement.EditorSceneManager.MarkAllScenesDirty();
            Debug.Log("[SwordCharacterSetupTool] DebugOverlayUI をシーンに追加しました。F1 キーで表示切り替え。Ctrl+S で保存。");
        }

        // ── ヘルパー ────────────────────────────────────────────────────

        static AnimatorState AddState(AnimatorStateMachine sm, string name, string clipPath, Vector3 pos)
        {
            var state = sm.AddState(name, pos);
            state.motion = LoadClip(clipPath);
            return state;
        }

        static AnimatorClip LoadClip(string path)
        {
            var clip = AssetDatabase.LoadAssetAtPath<AnimationClip>(path);
            if (clip == null)
                Debug.LogWarning("[SwordCharacterSetupTool] クリップが見つかりません: " + path);
            return clip;
        }

        static void AddAnyTriggerTransition(AnimatorStateMachine sm, AnimatorState dest, string trigger, bool exitTime)
        {
            var t = sm.AddAnyStateTransition(dest);
            t.AddCondition(AnimatorConditionMode.If, 0, trigger);
            t.duration = 0.05f;
            t.hasExitTime = exitTime;
            t.canTransitionToSelf = false;
        }

        static void AddExitTransition(AnimatorState from, AnimatorState to, float duration)
        {
            var t = from.AddTransition(to);
            t.hasExitTime = true;
            t.exitTime    = 0.85f;
            t.duration    = duration;
        }
    }
}
