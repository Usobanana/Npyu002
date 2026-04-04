using UnityEngine;
using UnityEditor;

namespace ActionGame.Editor
{
    /// <summary>
    /// パーティクルエフェクト Prefab 生成 + HP バー改善を一括実行。
    /// Tools/ActionGame/Setup Polish Effects から実行。
    /// </summary>
    public static class PolishSetupTool
    {
        [MenuItem("Tools/ActionGame/Setup Polish Effects")]
        public static void SetupPolishEffects()
        {
            if (!AssetDatabase.IsValidFolder("Assets/Prefabs"))
                AssetDatabase.CreateFolder("Assets", "Prefabs");

            var hitPrefab         = CreateHitEffect("HitEffect",         new Color(1f, 0.6f, 0.1f), 0.3f, 20);
            var enemyDeathPrefab  = CreateHitEffect("EnemyDeathEffect",  new Color(1f, 0.2f, 0.1f), 0.6f, 40);
            var playerDeathPrefab = CreateHitEffect("PlayerDeathEffect", new Color(0.3f, 0.6f, 1f), 0.6f, 40);

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

            // HP バー: PlayerHPBar を左上に大きく、EnemyHPBar を右上に配置
            ImproveHPBar("Canvas/HUD/PlayerHPBar",
                new Vector2(0, 1), new Vector2(0, 1), new Vector2(160, -25), new Vector2(300, 28),
                new Color(0.15f, 0.85f, 0.25f));

            ImproveHPBar("Canvas/HUD/EnemyHPBar",
                new Vector2(1, 1), new Vector2(1, 1), new Vector2(-160, -25), new Vector2(300, 28),
                new Color(0.9f, 0.2f, 0.15f));

            // HP バーにラベルテキスト追加
            AddHPLabel("Canvas/HUD/PlayerHPBar", "HP", true);
            AddHPLabel("Canvas/HUD/EnemyHPBar",  "ENEMY", false);

            AssetDatabase.SaveAssets();
            Debug.Log("[PolishSetupTool] Effects and HP bars setup complete.");
        }

        // ---- パーティクル Prefab 生成 ----

        static GameObject CreateHitEffect(string name, Color color, float duration, int count)
        {
            string path = $"Assets/Prefabs/{name}.prefab";
            var existing = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (existing != null) return existing;

            var go = new GameObject(name);
            var ps = go.AddComponent<ParticleSystem>();
            go.AddComponent<ActionGame.HitEffect>();

            var main = ps.main;
            main.duration        = duration;
            main.loop            = false;
            main.startLifetime   = duration * 1.2f;
            main.startSpeed      = new ParticleSystem.MinMaxCurve(3f, 7f);
            main.startSize       = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
            main.startColor      = color;
            main.gravityModifier = 0.3f;
            main.maxParticles    = count * 2;

            var emission = ps.emission;
            emission.rateOverTime = 0;
            emission.SetBursts(new[] { new ParticleSystem.Burst(0f, count) });

            var shape = ps.shape;
            shape.shapeType = ParticleSystemShapeType.Sphere;
            shape.radius    = 0.2f;

            var renderer = go.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Billboard;

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

            // Fill の色を更新
            var fill = go.transform.Find("Fill Area/Fill");
            if (fill != null)
            {
                var img = fill.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.color = fillColor;
            }
            // 旧構造 ("Fill") にも対応
            var fillOld = go.transform.Find("Fill");
            if (fillOld != null)
            {
                var img = fillOld.GetComponent<UnityEngine.UI.Image>();
                if (img != null) img.color = fillColor;
            }

            EditorUtility.SetDirty(go);
        }

        static void AddHPLabel(string barPath, string labelText, bool leftAlign)
        {
            var barGO = GameObject.Find(barPath);
            if (barGO == null) return;
            if (barGO.transform.Find("Label") != null) return; // 既にある

            var label = new GameObject("Label");
            label.transform.SetParent(barGO.transform, false);
            var rt = label.AddComponent<RectTransform>();
            rt.anchorMin = new Vector2(leftAlign ? 0f : 1f, 0.5f);
            rt.anchorMax = new Vector2(leftAlign ? 0f : 1f, 0.5f);
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
