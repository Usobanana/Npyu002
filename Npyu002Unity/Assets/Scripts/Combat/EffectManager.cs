using UnityEngine;

namespace ActionGame
{
    /// <summary>
    /// ヒットエフェクト・死亡エフェクトを一元管理するシングルトン。
    /// Prefab が未設定でもエラーにならない。
    /// </summary>
    public class EffectManager : MonoBehaviour
    {
        public static EffectManager Instance { get; private set; }

        [Header("Hit Effects")]
        [SerializeField] GameObject hitEffectPrefab;
        [SerializeField] GameObject enemyDeathEffectPrefab;
        [SerializeField] GameObject playerDeathEffectPrefab;

        [Header("Camera Shake")]
        [SerializeField] float hitShakeDuration  = 0.12f;
        [SerializeField] float hitShakeMagnitude = 0.12f;
        [SerializeField] float deathShakeDuration  = 0.25f;
        [SerializeField] float deathShakeMagnitude = 0.25f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        public void SpawnHit(Vector3 position)
        {
            Spawn(hitEffectPrefab, position);
            CameraShake.Instance?.Shake(hitShakeDuration, hitShakeMagnitude);
        }

        public void SpawnEnemyDeath(Vector3 position)
        {
            Spawn(enemyDeathEffectPrefab, position);
            CameraShake.Instance?.Shake(deathShakeDuration, deathShakeMagnitude);
        }

        public void SpawnPlayerDeath(Vector3 position)
        {
            Spawn(playerDeathEffectPrefab, position);
            CameraShake.Instance?.Shake(deathShakeDuration, deathShakeMagnitude);
        }

        void Spawn(GameObject prefab, Vector3 position)
        {
            if (prefab != null)
                Instantiate(prefab, position, Quaternion.identity);
        }
    }
}
