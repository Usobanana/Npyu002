using UnityEngine;
using UnityEngine.UI;

namespace ActionGame
{
    /// <summary>
    /// エネミー AI の ON/OFF を切り替えるデバッグボタン。
    /// EnemyBT.AIEnabled を制御する。デフォルト OFF。
    /// </summary>
    public class EnemyAIToggleButton : MonoBehaviour
    {
        [SerializeField] Button button;
        [SerializeField] Text   label;

        static readonly Color ColorOff = new Color(0.9f, 0.3f, 0.3f, 0.85f);
        static readonly Color ColorOn  = new Color(0.3f, 0.8f, 0.3f, 0.85f);

        void Start()
        {
            if (button == null) button = GetComponent<Button>();
            if (label  == null) label  = GetComponentInChildren<Text>();

            button.onClick.AddListener(Toggle);
            Refresh();
        }

        void Toggle()
        {
            EnemyBT.AIEnabled = !EnemyBT.AIEnabled;
            Refresh();
        }

        void Refresh()
        {
            bool on = EnemyBT.AIEnabled;
            if (label  != null) label.text = on ? "AI: ON" : "AI: OFF";
            var img = button.GetComponent<Image>();
            if (img != null) img.color = on ? ColorOn : ColorOff;
        }
    }
}
