using System.Collections;
using UnityEngine;

namespace ActionGame
{
    /// <summary>
    /// ヒット時に生成されるエフェクト用コンポーネント。
    /// ParticleSystem を持つ Prefab にアタッチし、再生後に自動破棄。
    /// EffectManager.SpawnHit() から生成される。
    /// </summary>
    [RequireComponent(typeof(ParticleSystem))]
    public class HitEffect : MonoBehaviour
    {
        ParticleSystem ps;

        void Awake() => ps = GetComponent<ParticleSystem>();

        void Start()
        {
            ps.Play();
            Destroy(gameObject, ps.main.duration + ps.main.startLifetime.constantMax);
        }
    }
}
