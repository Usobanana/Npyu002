using UnityEngine;
using UnityEngine.UI;

namespace ActionGame
{
    /// <summary>
    /// Enemy / ExplosiveCube などの頭上に追従する World Space HP バー。
    /// Health コンポーネントと同じ GameObject にアタッチする。
    /// 死亡時に自動で非表示。
    /// </summary>
    [RequireComponent(typeof(Health))]
    public class WorldSpaceHPBar : MonoBehaviour
    {
        [Header("表示設定")]
        [SerializeField] Vector3 offset        = new Vector3(0f, 2.4f, 0f); // 頭上のオフセット
        [SerializeField] Vector2 barSize       = new Vector2(1.2f, 0.12f);  // バーのサイズ(m)
        [SerializeField] Color   fullColor     = new Color(0.2f, 0.9f, 0.2f); // 満タン時の色
        [SerializeField] Color   emptyColor    = new Color(0.9f, 0.1f, 0.1f); // 空時の色
        [SerializeField] Color   bgColor       = new Color(0f, 0f, 0f, 0.6f);
        [SerializeField] bool    hideWhenFull  = true; // 満タン時は非表示
        [SerializeField] float   hideDelay     = 2f;   // ダメージ後この秒数で非表示

        Health        health;
        Canvas        canvas;
        Image         barFill;
        Image         barBG;
        Camera        mainCam;
        float         hideTimer = 0f;
        bool          isDead    = false;

        void Awake()
        {
            health = GetComponent<Health>();
            mainCam = Camera.main;

            BuildCanvas();

            health.OnHealthChanged += OnHealthChanged;
            health.OnDeath         += OnDeath;

            // 初期状態（満タンなら非表示）
            SetVisible(!hideWhenFull);
        }

        void BuildCanvas()
        {
            // World Space Canvas を子として作成
            var canvasGO = new GameObject("HPBarCanvas");
            canvasGO.transform.SetParent(transform);
            canvasGO.transform.localPosition = offset;

            canvas             = canvasGO.AddComponent<Canvas>();
            canvas.renderMode  = RenderMode.WorldSpace;
            canvas.sortingOrder = 10;

            var rect = canvasGO.GetComponent<RectTransform>();
            rect.sizeDelta  = barSize;
            rect.localScale = Vector3.one;

            // 背景
            var bgGO  = new GameObject("BG");
            bgGO.transform.SetParent(canvasGO.transform, false);
            barBG = bgGO.AddComponent<Image>();
            barBG.color = bgColor;
            var bgRect = bgGO.GetComponent<RectTransform>();
            bgRect.anchorMin = Vector2.zero;
            bgRect.anchorMax = Vector2.one;
            bgRect.offsetMin = Vector2.zero;
            bgRect.offsetMax = Vector2.zero;

            // フィル（localScale.x = ratio 方式 / pivot 左端）
            var fillGO = new GameObject("Fill");
            fillGO.transform.SetParent(canvasGO.transform, false);
            barFill = fillGO.AddComponent<Image>();
            barFill.color = fullColor;
            var fillRect = fillGO.GetComponent<RectTransform>();
            fillRect.pivot     = new Vector2(0f, 0.5f);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;
        }

        void LateUpdate()
        {
            if (isDead || canvas == null) return;

            // 常にカメラの方を向く（ビルボード）
            if (mainCam != null)
                canvas.transform.LookAt(
                    canvas.transform.position + mainCam.transform.rotation * Vector3.forward,
                    mainCam.transform.rotation * Vector3.up);

            // 満タン非表示タイマー
            if (hideWhenFull && hideTimer > 0f)
            {
                hideTimer -= Time.deltaTime;
                if (hideTimer <= 0f && Mathf.Approximately(barFill.fillAmount, 1f))
                    SetVisible(false);
            }
        }

        void OnHealthChanged(float cur, float max)
        {
            if (barFill == null) return;

            float ratio = Mathf.Clamp01(max > 0f ? cur / max : 0f);
            barFill.rectTransform.localScale = new Vector3(ratio, 1f, 1f);
            barFill.color                    = Color.Lerp(emptyColor, fullColor, ratio);

            SetVisible(true);

            // 満タン時は一定秒後に非表示
            if (hideWhenFull && ratio >= 1f)
                hideTimer = hideDelay;
            else
                hideTimer = 0f;
        }

        void OnDeath()
        {
            isDead = true;
            SetVisible(false);
        }

        void SetVisible(bool visible)
        {
            if (canvas != null)
                canvas.gameObject.SetActive(visible);
        }
    }
}
