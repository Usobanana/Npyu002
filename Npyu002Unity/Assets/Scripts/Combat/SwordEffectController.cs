using UnityEngine;

public class SwordEffectController : MonoBehaviour
{
    [Header("トレイル設定")]
    [SerializeField] private TrailRenderer _trailRenderer;

    [Header("エフェクト設定")]
    [SerializeField] private GameObject _slashEffectPrefab; // 斬撃エフェクト
    [SerializeField] private Transform _effectSpawnPoint;  // 出現させる場所（剣の先など）

    public void StartTrail()
    {
        if (_trailRenderer != null) _trailRenderer.emitting = true;

        // 振った瞬間にエフェクト（プレハブ）を生成する
        if (_slashEffectPrefab != null && _effectSpawnPoint != null)
        {
            // エフェクトを生成
            GameObject effect = Instantiate(_slashEffectPrefab, _effectSpawnPoint.position, _effectSpawnPoint.rotation);
            
            // 親を剣に設定すると剣と一緒に動きます（お好みで）
            // effect.transform.SetParent(_effectSpawnPoint);

            // 3秒後に自動で消えるように設定（アセット側で消える設定がない場合）
            Destroy(effect, 3f);
        }
    }

    public void StopTrail()
    {
        if (_trailRenderer != null) _trailRenderer.emitting = false;
    }
}