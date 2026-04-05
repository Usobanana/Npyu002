using UnityEngine;
using UnityEngine.UI;

namespace ActionGame
{
    /// <summary>
    /// HP バー表示。fillImage の localScale.x を ratio にする方式。
    /// pivot を左端(0, 0.5)に固定することで左→右に縮む。
    /// LayoutGroup / Mask の影響を受けない最もシンプルな実装。
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] Slider      slider;     // Slider を使う場合（省略可）
        [SerializeField] Image       fillImage;  // 左端 pivot 固定の fill Image
        [SerializeField] Health      target;

        [SerializeField] Color fullColor  = new Color(0.2f, 0.85f, 0.2f);
        [SerializeField] Color emptyColor = new Color(0.9f, 0.15f, 0.1f);

        RectTransform fillRect;

        void Start()
        {
            if (slider == null) slider = GetComponentInChildren<Slider>();

            if (fillImage != null)
            {
                fillRect = fillImage.rectTransform;
                // pivot を左端に固定 → localScale.x を変えると左→右に縮む
                fillRect.pivot     = new Vector2(0f, 0.5f);
                fillRect.anchorMin = Vector2.zero;
                fillRect.anchorMax = Vector2.one;
                fillRect.offsetMin = Vector2.zero;
                fillRect.offsetMax = Vector2.zero;
                fillImage.color    = fullColor;
            }

            if (target != null)
            {
                target.OnHealthChanged += UpdateBar;
                UpdateBar(target.CurrentHP, target.MaxHP);
            }
            else
            {
                Debug.LogWarning("[HealthBarUI] target (Health) が未アサインです。Inspector で設定してください。", this);
            }
        }

        void UpdateBar(float current, float max)
        {
            float ratio = Mathf.Clamp01(max > 0f ? current / max : 0f);

            if (slider != null)
                slider.value = ratio;

            if (fillRect != null)
            {
                // localScale.x = ratio でバー幅を変える（pivot 左端なので左から縮む）
                fillRect.localScale = new Vector3(ratio, 1f, 1f);
                fillImage.color     = Color.Lerp(emptyColor, fullColor, ratio);
            }
        }

        void OnDestroy()
        {
            if (target != null)
                target.OnHealthChanged -= UpdateBar;
        }
    }
}
