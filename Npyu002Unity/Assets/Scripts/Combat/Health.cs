using System;
using UnityEngine;

namespace ActionGame
{
    /// <summary>HP 管理コンポーネント。Player / Enemy 共用。</summary>
    public class Health : MonoBehaviour
    {
        [SerializeField] float maxHP = 100f;
        float currentHP;

        public float CurrentHP => currentHP;
        public float MaxHP => maxHP;
        public float NormalizedHP => maxHP > 0 ? currentHP / maxHP : 0f;
        public bool IsAlive => currentHP > 0f;

        /// <summary>HP が変化したとき (currentHP, maxHP)</summary>
        public event Action<float, float> OnHealthChanged;

        /// <summary>HP が 0 になったとき</summary>
        public event Action OnDeath;

        void Awake() => currentHP = maxHP;

        public void TakeDamage(float amount)
        {
            if (!IsAlive) return;
            currentHP = Mathf.Max(0f, currentHP - amount);
            OnHealthChanged?.Invoke(currentHP, maxHP);
            if (currentHP <= 0f)
                OnDeath?.Invoke();
        }

        public void Heal(float amount)
        {
            if (!IsAlive) return;
            currentHP = Mathf.Min(maxHP, currentHP + amount);
            OnHealthChanged?.Invoke(currentHP, maxHP);
        }
    }
}
