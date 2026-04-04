using System.Collections;
using UnityEngine;

namespace ActionGame
{
    /// <summary>
    /// ヒット時にカメラを揺らす。
    /// AudioManager と同様に静的 Instance でどこからでも呼べる。
    /// </summary>
    public class CameraShake : MonoBehaviour
    {
        public static CameraShake Instance { get; private set; }

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(this); return; }
            Instance = this;
        }

        /// <summary>duration 秒間、magnitude の強さで揺らす</summary>
        public void Shake(float duration = 0.15f, float magnitude = 0.15f)
        {
            StopAllCoroutines();
            StartCoroutine(DoShake(duration, magnitude));
        }

        IEnumerator DoShake(float duration, float magnitude)
        {
            var origin = transform.localPosition;
            float elapsed = 0f;

            while (elapsed < duration)
            {
                float strength = magnitude * (1f - elapsed / duration);
                transform.localPosition = origin + (Vector3)Random.insideUnitCircle * strength;
                elapsed += Time.deltaTime;
                yield return null;
            }

            transform.localPosition = origin;
        }
    }
}
