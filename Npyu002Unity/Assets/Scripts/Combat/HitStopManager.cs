using UnityEngine;
using System.Collections;

namespace ActionGame
{
    /// <summary>
    /// ヒットストップ管理。攻撃ヒット時に Time.timeScale を一瞬下げてインパクトを演出する。
    /// Trigger(duration, slowScale) を呼ぶだけで使える。
    /// </summary>
    public class HitStopManager : MonoBehaviour
    {
        public static HitStopManager Instance { get; private set; }

        bool isActive = false;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        /// <summary>
        /// ヒットストップを発動する。
        /// </summary>
        /// <param name="duration">停止時間（秒・リアルタイム）</param>
        /// <param name="slowScale">この間の timeScale（0 = 完全停止、0.05 = ほぼ停止）</param>
        public void Trigger(float duration, float slowScale = 0.05f)
        {
            if (isActive) return; // 連打されても重複しない
            StartCoroutine(HitStopCoroutine(duration, slowScale));
        }

        IEnumerator HitStopCoroutine(float duration, float slowScale)
        {
            isActive       = true;
            Time.timeScale = slowScale;
            yield return new WaitForSecondsRealtime(duration);
            Time.timeScale = 1f;
            isActive       = false;
        }
    }
}
