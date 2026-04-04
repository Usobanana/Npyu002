using UnityEngine;
using UnityEditor;
using UnityEditor.Animations;

namespace ActionGame.Editor
{
    public static class CharacterSetupTool
    {
        [MenuItem("Tools/ActionGame/Setup Animator Controller")]
        public static void SetupAnimatorController()
        {
            // フォルダ準備
            if (!AssetDatabase.IsValidFolder("Assets/Animations"))
                AssetDatabase.CreateFolder("Assets", "Animations");

            string controllerPath = "Assets/Animations/PlayerAnimator.controller";

            // 既存があれば削除して作り直す
            AssetDatabase.DeleteAsset(controllerPath);

            var controller = AnimatorController.CreateAnimatorControllerAtPath(controllerPath);
            var root = controller.layers[0].stateMachine;

            // クリップ読み込み
            var clipIdle      = LoadClip("Assets/Characters/Animations/Idle.fbx",     "mixamo.com");
            var clipRun       = LoadClip("Assets/Characters/Animations/Run.fbx",      "mixamo.com");
            var clipAttack    = LoadClip("Assets/Characters/Animations/Attack.fbx",   "mixamo.com");
            var clipJump      = LoadClip("Assets/Characters/Animations/Jump.fbx",     "mixamo.com");
            var clipHitReact  = LoadClip("Assets/Characters/Animations/HitReact.fbx", "mixamo.com");
            var clipDeath     = LoadClip("Assets/Characters/Animations/Death.fbx",    "mixamo.com");

            // パラメータ追加
            controller.AddParameter("Speed",     AnimatorControllerParameterType.Float);
            controller.AddParameter("IsJumping", AnimatorControllerParameterType.Bool);
            controller.AddParameter("Attack",    AnimatorControllerParameterType.Trigger);
            controller.AddParameter("HitReact",  AnimatorControllerParameterType.Trigger);
            controller.AddParameter("IsDead",    AnimatorControllerParameterType.Bool);

            // ステート作成
            var stIdle     = root.AddState("Idle");     stIdle.motion     = clipIdle;
            var stRun      = root.AddState("Run");      stRun.motion      = clipRun;
            var stAttack   = root.AddState("Attack");   stAttack.motion   = clipAttack;
            var stJump     = root.AddState("Jump");     stJump.motion     = clipJump;
            var stHitReact = root.AddState("HitReact"); stHitReact.motion = clipHitReact;
            var stDeath    = root.AddState("Death");    stDeath.motion    = clipDeath;

            root.defaultState = stIdle;

            // --- Idle <-> Run ---
            var t = stIdle.AddTransition(stRun);
            t.AddCondition(AnimatorConditionMode.Greater, 0.1f, "Speed");
            t.hasExitTime = false;

            t = stRun.AddTransition(stIdle);
            t.AddCondition(AnimatorConditionMode.Less, 0.1f, "Speed");
            t.hasExitTime = false;

            // --- Idle/Run -> Attack ---
            foreach (var from in new[] { stIdle, stRun })
            {
                t = from.AddTransition(stAttack);
                t.AddCondition(AnimatorConditionMode.If, 0, "Attack");
                t.hasExitTime = false;
            }
            t = stAttack.AddTransition(stIdle);
            t.hasExitTime  = true;
            t.exitTime     = 0.9f;
            t.duration     = 0.1f;

            // --- Any -> Jump ---
            t = root.AddAnyStateTransition(stJump);
            t.AddCondition(AnimatorConditionMode.If, 0, "IsJumping");
            t.hasExitTime = false;
            t.canTransitionToSelf = false;

            t = stJump.AddTransition(stIdle);
            t.AddCondition(AnimatorConditionMode.IfNot, 0, "IsJumping");
            t.hasExitTime = false;

            // --- Any -> HitReact ---
            t = root.AddAnyStateTransition(stHitReact);
            t.AddCondition(AnimatorConditionMode.If, 0, "HitReact");
            t.hasExitTime = false;
            t.canTransitionToSelf = false;

            t = stHitReact.AddTransition(stIdle);
            t.hasExitTime = true;
            t.exitTime    = 0.9f;
            t.duration    = 0.1f;

            // --- Any -> Death (優先度高) ---
            t = root.AddAnyStateTransition(stDeath);
            t.AddCondition(AnimatorConditionMode.If, 0, "IsDead");
            t.hasExitTime = false;
            t.canTransitionToSelf = false;
            t.interruptionSource = TransitionInterruptionSource.None;

            AssetDatabase.SaveAssets();
            Debug.Log("[CharacterSetupTool] Animator Controller created: " + controllerPath);
        }

        static AnimationClip LoadClip(string fbxPath, string clipName)
        {
            var assets = AssetDatabase.LoadAllAssetsAtPath(fbxPath);
            foreach (var a in assets)
                if (a is AnimationClip c && c.name == clipName) return c;
            // フォールバック: 最初の AnimationClip
            foreach (var a in assets)
                if (a is AnimationClip c2 && !c2.name.Contains("__preview__")) return c2;
            Debug.LogWarning("[CharacterSetupTool] Clip not found: " + fbxPath);
            return null;
        }

        // ---------------------------------------------------------------
        [MenuItem("Tools/ActionGame/Setup Player Character")]
        public static void SetupPlayerCharacter()
        {
            var player = GameObject.Find("Player");
            if (player == null) { Debug.LogError("[CharacterSetupTool] Player not found."); return; }

            // 既存の X Bot 子があれば削除
            var existing = player.transform.Find("X Bot");
            if (existing != null) Object.DestroyImmediate(existing.gameObject);

            // X Bot Prefab をロード
            var xbotPrefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Characters/X Bot.fbx");
            if (xbotPrefab == null) { Debug.LogError("[CharacterSetupTool] X Bot.fbx not found."); return; }

            // 子として配置 (足元を合わせるため y=-1)
            var xbotGO = (GameObject)PrefabUtility.InstantiatePrefab(xbotPrefab, player.transform);
            xbotGO.name = "X Bot";
            xbotGO.transform.localPosition = new Vector3(0f, -1f, 0f);
            xbotGO.transform.localRotation = Quaternion.identity;
            xbotGO.transform.localScale    = Vector3.one;

            // Animator に Controller を設定
            var animator = xbotGO.GetComponent<Animator>();
            if (animator == null) animator = xbotGO.AddComponent<Animator>();
            var controller = AssetDatabase.LoadAssetAtPath<RuntimeAnimatorController>(
                "Assets/Animations/PlayerAnimator.controller");
            animator.runtimeAnimatorController = controller;
            animator.applyRootMotion = false;

            // Player ルートの MeshRenderer を無効化
            var mr = player.GetComponent<MeshRenderer>();
            if (mr != null) mr.enabled = false;

            // PlayerAnimationController を追加
            if (player.GetComponent<ActionGame.PlayerAnimationController>() == null)
                player.AddComponent<ActionGame.PlayerAnimationController>();

            EditorUtility.SetDirty(player);
            UnityEditor.SceneManagement.EditorSceneManager.MarkSceneDirty(
                UnityEngine.SceneManagement.SceneManager.GetActiveScene());

            Debug.Log("[CharacterSetupTool] Player character setup done.");
        }

        // ---------------------------------------------------------------
        [MenuItem("Tools/ActionGame/Setup Animation Imports")]
        public static void SetupAnimationImports()
        {
            // X Bot のアバターソースを取得
            string avatarSourcePath = "Assets/Characters/X Bot.fbx";
            var avatarSourceImporter = AssetImporter.GetAtPath(avatarSourcePath) as ModelImporter;
            if (avatarSourceImporter == null)
            {
                Debug.LogError("[CharacterSetupTool] X Bot.fbx not found at " + avatarSourcePath);
                return;
            }

            string[] animPaths = new[]
            {
                "Assets/Characters/Animations/Idle.fbx",
                "Assets/Characters/Animations/Run.fbx",
                "Assets/Characters/Animations/Attack.fbx",
                "Assets/Characters/Animations/Jump.fbx",
                "Assets/Characters/Animations/HitReact.fbx",
                "Assets/Characters/Animations/Death.fbx",
            };

            // ループさせるクリップ
            var loopPaths = new System.Collections.Generic.HashSet<string>
            {
                "Assets/Characters/Animations/Idle.fbx",
                "Assets/Characters/Animations/Run.fbx",
            };

            foreach (var path in animPaths)
            {
                var importer = AssetImporter.GetAtPath(path) as ModelImporter;
                if (importer == null)
                {
                    Debug.LogWarning("[CharacterSetupTool] Not found: " + path);
                    continue;
                }

                importer.animationType = ModelImporterAnimationType.Human;
                importer.avatarSetup   = ModelImporterAvatarSetup.CopyFromOther;
                importer.sourceAvatar  = avatarSourceImporter.sourceAvatar;

                // Loop Time 設定
                var clips = importer.clipAnimations;
                if (clips == null || clips.Length == 0)
                    clips = importer.defaultClipAnimations;

                bool loop = loopPaths.Contains(path);
                foreach (var clip in clips)
                {
                    clip.loopTime = loop;
                    clip.loopPose = loop;
                }
                importer.clipAnimations = clips;

                importer.SaveAndReimport();
                Debug.Log($"[CharacterSetupTool] Fixed (loop={loop}): " + path);
            }

            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("[CharacterSetupTool] Done.");
        }
    }
}
