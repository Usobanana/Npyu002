using UnityEngine;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine.AI;
using Unity.AI.Navigation;

namespace ActionGame.Editor
{
    /// <summary>
    /// SwordGame シーンを一発で構築するセットアップツール。
    /// Tools > ActionGame > Setup SwordGame Scene
    /// </summary>
    public static class SwordGameSetupTool
    {
        const string ScenePath = "Assets/Scenes/SwordGame.unity";

        [MenuItem("Tools/ActionGame/Setup SwordGame Scene")]
        public static void SetupScene()
        {
            // 新規シーン
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            // ── ライト ──────────────────────────────────────────
            var lightGO = new GameObject("Directional Light");
            var light   = lightGO.AddComponent<Light>();
            light.type      = LightType.Directional;
            light.intensity = 1.2f;
            light.shadows   = LightShadows.Soft;
            lightGO.transform.rotation = Quaternion.Euler(55f, -30f, 0f);

            // ── グラウンド ───────────────────────────────────────
            var ground = GameObject.CreatePrimitive(PrimitiveType.Plane);
            ground.name = "Ground";
            ground.transform.localScale = new Vector3(5f, 1f, 5f);
            SetColor(ground, new Color(0.25f, 0.35f, 0.25f));

            // ── NavMeshSurface ────────────────────────────────────
            var navSurface = ground.AddComponent<NavMeshSurface>();
            navSurface.collectObjects = CollectObjects.All;
            navSurface.BuildNavMesh();

            // ── カメラ ───────────────────────────────────────────
            var camGO = new GameObject("Main Camera");
            camGO.tag = "MainCamera";
            var cam = camGO.AddComponent<Camera>();
            cam.clearFlags      = CameraClearFlags.SolidColor;
            cam.backgroundColor = new Color(0.1f, 0.1f, 0.15f);
            camGO.AddComponent<AudioListener>();
            // 初期位置（TopDownPlayerController が実行時に上書きする）
            camGO.transform.position = new Vector3(0f, 14f, -8f);
            camGO.transform.rotation = Quaternion.Euler(55f, 0f, 0f);

            // ── InputHandler ──────────────────────────────────────
            var inputGO = new GameObject("InputHandler");
            inputGO.AddComponent<InputHandler>();

            // ── AudioManager ─────────────────────────────────────
            var audioGO = new GameObject("AudioManager");
            audioGO.AddComponent<AudioManager>();

            // ── EffectManager ─────────────────────────────────────
            var effectGO = new GameObject("EffectManager");
            effectGO.AddComponent<EffectManager>();

            // ── Player ──────────────────────────────────────────
            var playerGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
            playerGO.name = "Player";
            playerGO.tag  = "Player";
            playerGO.transform.position = new Vector3(0f, 1f, 0f);
            SetColor(playerGO, new Color(0.3f, 0.6f, 1f));

            // CharacterController (Capsule collider を削除して CharacterController を追加)
            Object.DestroyImmediate(playerGO.GetComponent<CapsuleCollider>());
            var cc = playerGO.AddComponent<CharacterController>();
            cc.height = 2f;
            cc.center = new Vector3(0f, 0f, 0f);

            var health = playerGO.AddComponent<Health>();
            SetSerializedField(health, "maxHP", 100f);

            var controller = playerGO.AddComponent<TopDownPlayerController>();
            var combo      = playerGO.AddComponent<ComboAttack>();

            // Animator (プレースホルダー)
            playerGO.AddComponent<Animator>();

            // ── Enemy × 2 ──────────────────────────────────────
            CreateEnemy(new Vector3(8f,  1f, 6f),  "Enemy_A");
            CreateEnemy(new Vector3(-7f, 1f, 8f),  "Enemy_B");

            // ── ExplosiveCube × 3 ─────────────────────────────
            CreateExplosiveCube(new Vector3(5f,  0.75f, -4f), "Building_A");
            CreateExplosiveCube(new Vector3(-5f, 0.75f, -4f), "Building_B");
            CreateExplosiveCube(new Vector3(0f,  0.75f,  9f), "Building_C");

            // ── Canvas / UI ───────────────────────────────────
            SetupMinimalUI();

            // ── シーン保存 ────────────────────────────────────
            if (!AssetDatabase.IsValidFolder("Assets/Scenes"))
                AssetDatabase.CreateFolder("Assets", "Scenes");

            EditorSceneManager.SaveScene(scene, ScenePath);
            AssetDatabase.Refresh();
            Debug.Log("[SwordGameSetupTool] SwordGame シーンを保存: " + ScenePath);
        }

        // ─── ヘルパー ────────────────────────────────────────────

        static void CreateEnemy(Vector3 pos, string name)
        {
            // 既存 Enemy.prefab を使う（あれば）
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/Prefabs/Enemy.prefab");
            GameObject enemyGO;
            if (prefab != null)
            {
                enemyGO = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
                enemyGO.name = name;
                enemyGO.transform.position = pos;
            }
            else
            {
                // フォールバック: シンプルなカプセル
                enemyGO = GameObject.CreatePrimitive(PrimitiveType.Capsule);
                enemyGO.name = name;
                enemyGO.transform.position = pos;
                SetColor(enemyGO, new Color(1f, 0.3f, 0.3f));
                Object.DestroyImmediate(enemyGO.GetComponent<CapsuleCollider>());
                enemyGO.AddComponent<CapsuleCollider>();

                var hp = enemyGO.AddComponent<Health>();
                SetSerializedField(hp, "maxHP", 60f);

                enemyGO.AddComponent<NavMeshAgent>();
                enemyGO.AddComponent<EnemyBT>();
                enemyGO.AddComponent<EnemyAnimationController>();
                enemyGO.AddComponent<Animator>();
            }
        }

        static void CreateExplosiveCube(Vector3 pos, string name)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.name = name;
            cube.transform.position   = pos;
            cube.transform.localScale = new Vector3(2f, 1.5f, 2f);

            var hp = cube.AddComponent<Health>();
            SetSerializedField(hp, "maxHP", 80f);

            cube.AddComponent<ExplosiveCube>();
        }

        static void SetupMinimalUI()
        {
            // EventSystem
            var esGO = new GameObject("EventSystem");
            esGO.AddComponent<UnityEngine.EventSystems.EventSystem>();
            esGO.AddComponent<UnityEngine.InputSystem.UI.InputSystemUIInputModule>();

            // Canvas
            var canvasGO = new GameObject("Canvas");
            var canvas   = canvasGO.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvasGO.AddComponent<UnityEngine.UI.CanvasScaler>();
            canvasGO.AddComponent<UnityEngine.UI.GraphicRaycaster>();
        }

        static void SetColor(GameObject go, Color color)
        {
            var r = go.GetComponent<Renderer>();
            if (r == null) return;
            var mat = new Material(Shader.Find("Universal Render Pipeline/Lit")
                      ?? Shader.Find("Standard"));
            mat.color = color;
            if (mat.HasProperty("_BaseColor")) mat.SetColor("_BaseColor", color);
            r.sharedMaterial = mat;
        }

        static void SetSerializedField(Object obj, string fieldName, float value)
        {
            var so   = new SerializedObject(obj);
            var prop = so.FindProperty(fieldName);
            if (prop != null) { prop.floatValue = value; so.ApplyModifiedProperties(); }
        }
    }
}
