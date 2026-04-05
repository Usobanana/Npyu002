using UnityEngine;
using System.Collections;

namespace ActionGame
{
    /// <summary>
    /// 攻撃ヒット時に敵が一瞬白く光るヒットフラッシュ。
    /// Health.OnHealthChanged に自動購読して発火する。
    /// MaterialPropertyBlock を使うのでマテリアルのインスタンス化なし。
    /// </summary>
    public class HitFlash : MonoBehaviour
    {
        [SerializeField] Color flashColor    = Color.white;
        [SerializeField] float flashDuration = 0.08f; // 秒（リアルタイム）

        Renderer[]            renderers;
        MaterialPropertyBlock mpb;
        Color[]               originalColors;

        void Awake()
        {
            renderers      = GetComponentsInChildren<Renderer>();
            mpb            = new MaterialPropertyBlock();
            originalColors = new Color[renderers.Length];

            // 元のベースカラーを記憶
            for (int i = 0; i < renderers.Length; i++)
            {
                var mat = renderers[i].sharedMaterial;
                if (mat == null) { originalColors[i] = Color.white; continue; }

                if      (mat.HasProperty("_BaseColor")) originalColors[i] = mat.GetColor("_BaseColor");
                else if (mat.HasProperty("_Color"))     originalColors[i] = mat.GetColor("_Color");
                else                                    originalColors[i] = Color.white;
            }

            // Health に自動購読
            var health = GetComponent<Health>();
            if (health != null)
                health.OnHealthChanged += (cur, max) => { if (cur < max && cur > 0f) Flash(); };
        }

        public void Flash()
        {
            StopAllCoroutines();
            StartCoroutine(FlashCoroutine());
        }

        IEnumerator FlashCoroutine()
        {
            ApplyColor(flashColor);
            yield return new WaitForSecondsRealtime(flashDuration); // ヒットストップ中も動く
            RestoreColor();
        }

        void ApplyColor(Color c)
        {
            foreach (var r in renderers)
            {
                r.GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", c);
                mpb.SetColor("_Color",     c);
                r.SetPropertyBlock(mpb);
            }
        }

        void RestoreColor()
        {
            for (int i = 0; i < renderers.Length; i++)
            {
                renderers[i].GetPropertyBlock(mpb);
                mpb.SetColor("_BaseColor", originalColors[i]);
                mpb.SetColor("_Color",     originalColors[i]);
                renderers[i].SetPropertyBlock(mpb);
            }
        }
    }
}
