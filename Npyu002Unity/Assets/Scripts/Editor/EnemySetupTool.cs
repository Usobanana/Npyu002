using UnityEngine;
using UnityEngine.UI;
using UnityEditor;
using UnityEditor.Animations;
using UnityEngine.SceneManagement;
using UnityEditor.SceneManagement;

namespace ActionGame.Editor
{
    public static class EnemySetupTool
    {
        const string ControllerPath = "Assets/Animations/SwordPlayerController.controller";

        // ---------------------------------------------------------------
        [MenuItem("Tools/ActionGame/Setup Enemy Animator Controller")]
        public static void SetupEnemyAnimatorController()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");

            AssetDatabase.DeleteAsset(ControllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(ControllerPath);
            var root = controller.layers[0].stateMachine;

            var clipIdle     = LoadClip("Assets/Characters/Animations/Idle.fbx");
            var clipRun      = LoadClip("Assets/Characters/Animations/Run.fbx");
            var clipAttack   = LoadClip("Assets/Characters/Animations/Attack.fbx");
            var clipHitReact = LoadClip("Assets/Characters/Animations/HitReact.fbx");
            var clipDeath    = LoadClip("Assets/Characters/Animations/Death.fbx");

            controller.AddParameter("Speed",    AnimatorControllerParameterType.Float);
            controller.AddParameter("Attack",   AnimatorControllerParameterType.Trigger);
            controller.AddParameter("HitReact", AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead",   AnimatorControllerParameterType.Bool);

            var stIdle     = root.AddState("Idle");     stIdle.motion     = clipIdle;
            var stRun      = root.AddState("Run");      stRun.motion      = clipRun;
            var stAttack   = root.AddState("Attack");   stAttack.motion   = clipAttack;
            var stHitReact = root.AddState("HitReact"); stHitReact.motion = clipHitReact;
            var stDeath    = root.AddState("Death");    stDeath.motion    = clipDeath;

            root.defaultState = stIdle;

            // Idle <-> Run
            var t = stIdle.AddTransition(stRun);
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.hasExitTime = false; t.duration = 0.1f;

            t = stRun.AddTransition(stIdle);
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            t.hasExitTime = false; t.duration = 0.1f;

            // Idle/Run -> Attack
            foreach (var from in new[] { stIdle, stRun })
            {
                t = from.AddTransition(stAttack);
                t.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                t.hasExitTime = false; t.duration = 0.05f;
            }
            t = stAttack.AddTransition(stIdle);
            t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f;

            // Any -> HitReact
            t = root.AddAnyStateTransition(stHitReact);
            t.AddCondition(AnimatorConditionMode.If, 0, "HitReact");
            t.hasExitTime = false; t.canTransitionToSelf = false; t.duration = 0.05f;

            t = stHitReact.AddTransition(stIdle);
            t.hasExitTime = true; t.exitTime = 0.9f; t.duration = 0.1f;

            // Any -> Death (優先度最高)
            t = root.AddAnyStateTransition(stDeath);
            t.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            t.hasExitTime = false; t.canTransitionToSelf = false;
            t.interruptionSource = TransitionInterruptionSource.None;

            AssetDatabase.SaveAssets();
            Debug.Log("[EnemySetupTool] EnemyAnimator.controller created.");
        }

        // ---------------------------------------------------------------
        [MenuItem("Tools/ActionGame/Setup Enemy Characters")]
        public static void SetupEnemyCharacters()
        {
            // コントローラーが未作成なら先に作る
            if (!AssetDatabase.IsValidFolder("Assets/Animations") ||
                AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath) == null)
            {
                SetupEnemyAnimatorController();
            }

            var charPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/SwordAnimationPack/Prefabs/9CG_Sword.prefab");
            if (charPrefab == null)
            {
                Debug.LogError("[EnemySetupTool] 9CG_Sword.prefab not found.");
                return;
            }

            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(ControllerPath);

            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            if (enemies.Length == 0)
            {
                // タグが未設定の場合、名前で検索
                var all = Object.FindObjectsByType<EnemyBT>(FindObjectsInactive.Include);
                var list = new System.Collections.Generic.List<GameObject>();
                foreach (var e in all) list.Add(e.gameObject);
                enemies = list.ToArray();
            }

            if (enemies.Length == 0)
            {
                Debug.LogError("[EnemySetupTool] No Enemy GameObjects found.");
                return;
            }

            foreach (var enemy in enemies)
            {
                SetupSingleEnemy(enemy, charPrefab, controller);
            }

            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log($"[EnemySetupTool] Setup complete for {enemies.Length} enemies.");
        }

        static void SetupSingleEnemy(GameObject enemy, GameObject charPrefab, RuntimeAnimatorController controller)
        {
            // 既存のモデル子を削除（X Bot / 9CG_Sword どちらでも対応）
            foreach (var childName in new[] { "X Bot", "9CG_Sword" })
            {
                var existing = enemy.transform.Find(childName);
                if (existing != null) Object.DestroyImmediate(existing.gameObject);
            }

            // キャラクターモデルを子として配置
            var charGO = (GameObject)PrefabUtility.InstantiatePrefab(charPrefab, enemy.transform);
            charGO.name = charPrefab.name;
            charGO.transform.localPosition = Vector3.zero;
            charGO.transform.localRotation = Quaternion.identity;
            charGO.transform.localScale    = Vector3.one;

            // Animator 設定
            var animator = charGO.GetComponentInChildren<Animator>();
            if (animator == null) animator = charGO.AddComponent<Animator>();
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // Avatar をアサイン
            var avatar = AssetDatabase.LoadAssetAtPath<Avatar>(
                "Assets/SwordAnimationPack/Model/9CG_Sword.FBX");
            if (avatar != null)
                animator.avatar = avatar;

            // EnemyMat（URP対応）を SkinnedMeshRenderer（キャラ本体）に適用
            var enemyMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/Materials/EnemyMat.mat");
            if (enemyMat != null)
            {
                foreach (var smr in charGO.GetComponentsInChildren<SkinnedMeshRenderer>())
                {
                    var mats = new Material[smr.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = enemyMat;
                    smr.sharedMaterials = mats;
                }
            }
            else
            {
                Debug.LogWarning("[EnemySetupTool] EnemyMat.mat が見つかりません。");
            }

            // Sword_URP.mat を MeshRenderer（剣）に適用
            var swordMat = AssetDatabase.LoadAssetAtPath<Material>("Assets/SwordAnimationPack/Materials/Sword_URP.mat");
            if (swordMat != null)
            {
                foreach (var mr2 in charGO.GetComponentsInChildren<MeshRenderer>())
                {
                    var mats = new Material[mr2.sharedMaterials.Length];
                    for (int i = 0; i < mats.Length; i++) mats[i] = swordMat;
                    mr2.sharedMaterials = mats;
                }
            }
            else
            {
                Debug.LogWarning("[EnemySetupTool] Sword_URP.mat が見つかりません。");
            }

            // 親カプセルの MeshRenderer を非表示
            var mr = enemy.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;

            // EnemyAnimationController を追加
            if (enemy.GetComponent<EnemyAnimationController>() == null)
                enemy.AddComponent<EnemyAnimationController>();

            EditorUtility.SetDirty(enemy);
            Debug.Log($"[EnemySetupTool] Setup: {enemy.name}");
        }

        // ---------------------------------------------------------------
        [MenuItem("Tools/ActionGame/Setup Enemy AI Toggle Button")]
        public static void SetupAIToggleButton()
        {
            // 既存の Canvas を探す
            var canvas = Object.FindAnyObjectByType<Canvas>();
            if (canvas == null)
            {
                Debug.LogError("[EnemySetupTool] Canvas not found in scene.");
                return;
            }

            // 既存ボタンがあれば削除
            var existing = canvas.transform.Find("EnemyAIToggleButton");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // ボタン GO 作成
            var btnGO = new GameObject("EnemyAIToggleButton");
            btnGO.transform.SetParent(canvas.transform, false);

            var rect = btnGO.AddComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot     = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(10f, -10f);
            rect.sizeDelta = new Vector2(120f, 40f);

            var img = btnGO.AddComponent<Image>();
            img.color = new Color(0.9f, 0.3f, 0.3f, 0.85f);

            var btn = btnGO.AddComponent<Button>();

            // ラベル
            var lblGO = new GameObject("Label");
            lblGO.transform.SetParent(btnGO.transform, false);
            var lblRect = lblGO.AddComponent<RectTransform>();
            lblRect.anchorMin = Vector2.zero;
            lblRect.anchorMax = Vector2.one;
            lblRect.offsetMin = Vector2.zero;
            lblRect.offsetMax = Vector2.zero;

            var txt = lblGO.AddComponent<Text>();
            txt.text      = "AI: OFF";
            txt.alignment = TextAnchor.MiddleCenter;
            txt.fontSize  = 18;
            txt.color     = Color.white;
            txt.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

            // EnemyAIToggleButton コンポーネント
            var toggle = btnGO.AddComponent<EnemyAIToggleButton>();

            // SerializedObject で参照をワイヤリング
            var so = new SerializedObject(toggle);
            so.FindProperty("button").objectReferenceValue = btn;
            so.FindProperty("label").objectReferenceValue  = txt;
            so.ApplyModifiedProperties();

            EditorUtility.SetDirty(btnGO);
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            Debug.Log("[EnemySetupTool] AI Toggle Button created.");
        }

        static AnimationClip LoadClip(string fbxPath)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
                if (a is AnimationClip c && !c.name.Contains("__preview__")) return c;
            Debug.LogWarning("[EnemySetupTool] Clip not found: " + fbxPath);
            return null;
        }
    }
}
