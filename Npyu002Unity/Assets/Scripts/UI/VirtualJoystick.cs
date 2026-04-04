using UnityEngine;
using UnityEngine.EventSystems;

namespace ActionGame
{
    /// <summary>
    /// モバイル用仮想ジョイスティック。
    /// Background (RectTransform) の中で Handle を動かし、Direction を出力する。
    /// InputHandler.joystick にアサインして使う。
    /// </summary>
    public class VirtualJoystick : MonoBehaviour, IPointerDownHandler, IDragHandler, IPointerUpHandler
    {
        [SerializeField] RectTransform background;
        [SerializeField] RectTransform handle;
        [SerializeField] float handleRange = 0.5f;   // background 半径に対する割合

        /// <summary>-1〜1 の入力方向ベクトル</summary>
        public Vector2 Direction { get; private set; }

        Canvas canvas;
        RectTransform canvasRect;
        bool isDragging;

        void Awake()
        {
            canvas     = GetComponentInParent<Canvas>();
            canvasRect = canvas.GetComponent<RectTransform>();

            if (background == null) background = GetComponent<RectTransform>();
            if (handle == null)
            {
                var child = transform.GetChild(0);
                if (child != null) handle = child.GetComponent<RectTransform>();
            }
        }

        void Start()
        {
            // InputHandler に自身を登録
            if (InputHandler.Instance != null)
                InputHandler.Instance.joystick = this;
        }

        public void OnPointerDown(PointerEventData data)
        {
            isDragging = true;
            OnDrag(data);
        }

        public void OnDrag(PointerEventData data)
        {
            if (!isDragging || background == null || handle == null) return;

            // スクリーン座標 → Canvas ローカル座標に変換
            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, data.position, data.pressEventCamera, out Vector2 localPoint);

            RectTransformUtility.ScreenPointToLocalPointInRectangle(
                canvasRect, background.position, data.pressEventCamera, out Vector2 bgCenter);

            Vector2 delta    = localPoint - bgCenter;
            float   maxRange = background.sizeDelta.x * 0.5f * handleRange;
            Vector2 clamped  = Vector2.ClampMagnitude(delta, maxRange);

            handle.anchoredPosition = clamped;
            Direction = clamped / maxRange;
        }

        public void OnPointerUp(PointerEventData data)
        {
            isDragging = false;
            Direction  = Vector2.zero;
            if (handle != null) handle.anchoredPosition = Vector2.zero;
        }
    }
}
