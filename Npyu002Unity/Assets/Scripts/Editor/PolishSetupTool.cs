using UnityEngine;
using UnityEditor;

namespace ActionGame.Editor
{
    public static class PolishSetupTool
    {
        [MenuItem("Tools/ActionGame/Setup Polish Effects")]
        public static void SetupPolishEffects()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");
            if (!AssetDatabase.IsValidFolder("Assets/Materials"))
                AssetDatabase.CreateFolder("Assets", "Materials");

            // パーティクル用マテリアルを作成
            var matHit         = CreateParticleMaterial("Mat_HitEffect",         new Color(1f, 0.6f, 0.1f));
            var matEnemyDeath  = CreateParticleMaterial("Mat_EnemyDeathEffect",  new Color(1f, 0.2f, 0.1f));
            var matPlayerDeath = CreateParticleMaterial("Mat_PlayerDeathEffect", new Color(0.3f, 0.6f, 1f));

            var hitPrefab         = CreateHitEffect("HitEffect",         new Color(1f, 0.6f, 0.1f), 0.3f, 20, matHit);
            var enemyDeathPrefab  = CreateHitEffect("EnemyDeathEffect",  new Color(1f, 0.2f, 0.1f), 0.6f, 40, matEnemyDeath);
            var playerDeathPrefab = CreateHitEffect("PlayerDeathEffect", new Color(0.3f, 0.6f, 1f), 0.6f, 40, matPlayerDeath);

            // EffectManager に Prefab を設定
            var gm = GameObject.Find("GameManagers");
            if (gm != null)
            {
                var em = gm.GetComponent<ActionGame.EffectManager>();
                if (em != null)
                {
                    var so = new SerializedObject(em);
                    so.FindProperty("hitEffectPrefab").objectReferenceValue         = hitPrefab;
                    so.FindProperty("enemyDeathEffectPrefab").objectReferenceValue  = enemyDeathPrefab;
                    so.FindProperty("playerDeathEffectPrefab").objectReferenceValue = playerDeathPrefab;
                    so.ApplyModifiedProperties();
                    EditorUtility.SetDirty(gm);
                }
            }

            // HP バー改善
            ImproveHPBar("Canvas/HUD/PlayerHPBar",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -25), new Vector2(300, 28),
                new Color(0.15f, 0.85f, 0.25f));
            ImproveHPBar("Canvas/HUD/EnemyHPBar",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-160, -25), new Vector2(300, 28),
                new Color(0.9f, 0.2f, 0.15f));
            AddHPLabel("Canvas/HUD/PlayerHPBar", "HP",    true);
            AddHPLabel("Canvas/HUD/EnemyHPBar",  "ENEMY", false);

            AssetDatabase.SaveAssets();
            Debug.Log("[PolishSetupTool] Done.");
        }

        // ---- マテリアル生成 ----

        static Material CreateParticleMaterial(string name, Color color)
        {
            string path = $"Assets/Materials/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.color = color;
                EditorUtility.SetDirty(existing);
                return existing;
            }

            // Particles/Standard Unlit シェーダーを使用
            var shader = Shader.Find("Particles/Standard Unlit");
            if (shader == null) shader = Shader.Find("Legacy Shaders/Particles/Additive");
            if (shader == null) shader = Shader.Find("Sprites/Default");

            var mat = new Material(shader);
            mat.name  = name;
            mat.color = color;
            AssetDatabase.CreateAsset(mat, path);
            return mat;
        }

        // ---- パーティクル Prefab 生成 ----

        static GameObject CreateHitEffect(string name, Color color, float duration, int count, Material mat)
        {
            string path = $"Assets/Prefabs/{name}.prefab";

            // 既存 Prefab を上書き更新
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null)
            {
                // マテリアルだけ更新
                var ps = existing.GetComponentInChildren<ParticleSystemRenderer>();
                if (ps != null && mat != null)
                {
                    ps.material = mat;
                    EditorUtility.SetDirty(existing);
                }
                return existing;
            }

            var go = new GameObject(name);
            var psComp = go.AddComponent<ParticleSystem>();
            go.AddComponent<ActionGame.HitEffect>();

            var main = psComp.main;
            main.duration        = duration;
            main.loop            = false;
            main.startLifetime   = duration * 1.2f;
            main.startSpeed      = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startColor      = color;
            main.gravityModifier = 0.3f;
            main.maxParticles    = count * 2;

            var emission = psComp.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

            var shape = psComp.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.2f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;
            if (mat != null) renderer.material = mat;

            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ---- HP バー改善 ----

        static void ImproveHPBar(string path, Vector2 anchorMin, Vector2 anchorMax,
            Vector2 pos, Vector2 size, Color fillColor)
        {
            var go = GameObject.Find(path);
            if (go == null) return;
            var rt = go.GetComponent<RectTransform>();
            if (rt != null)
            {
                rt.anchorMin = anchorMin; rt.anchorMax = anchorMax;
                rt.anchoredPosition = pos; rt.sizeDelta = size;
            }
            var fill = go.transform.Find("Fill Area/Fill") ?? go.transform.Find("Fill");
            if (fill != null)
            {
                var img = fill.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.color = fillColor;
            }
            EditorUtility.SetDirty(go);
        }

        static void AddHPLabel(string barPath, string labelText, bool leftAlign)
        {
            var barGO = GameObject.Find(barPath);
            if (barGO == null || barGO.transform.Find("Label") != null) return;

            var label = new GameObject("Label");
            label.transform.SetParent(barGO.transform, false);
            var rt = label.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(leftAlign ? 0f : 1f, 0.5f);
            rt.anchorMax = rt.anchorMin;
            rt.anchoredPosition = new Vector2(leftAlign ? -45f : 45f, 0f);
            rt.sizeDelta = new Vector2(90f, 28f);

            var text = label.AddComponent<UnityEngine.UI.Text>();
            text.text      = labelText;
            text.fontSize  = 18;
            text.color     = Color.white;
            text.alignment = leftAlign ? TextAnchor.MiddleRight : TextAnchor.MiddleLeft;
            text.font      = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            EditorUtility.SetDirty(barGO);
        }
    }
}
