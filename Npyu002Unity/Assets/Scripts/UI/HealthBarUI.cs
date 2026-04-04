using UnityEngine;
using UnityEngine.UI;

namespace ActionGame
{
    /// <summary>
    /// Slider を使った HP バー。
    /// Inspector で Target (Health コンポーネント) をアサイン。
    /// </summary>
    public class HealthBarUI : MonoBehaviour
    {
        [SerializeField] Slider slider;
        [SerializeField] Health target;

        void Start()
        {
            if (slider == null) slider = GetComponentInChildren<Slider>();

            if (target != null)
            {
                target.OnHealthChanged += UpdateBar;
                UpdateBar(target.CurrentHP, target.MaxHP);
            }
        }

        void UpdateBar(float current, float max)
        {
            if (slider != null)
                slider.value = max > 0 ? current / max : 0f;
        }

        void OnDestroy()
        {
            if (target != null)
                target.OnHealthChanged -= UpdateBar;
        }
    }
}
