using UnityEngine;

namespace ActionGame
{
    /// <summary>
    /// BGM / SE を一元管理する Singleton。
    /// AudioSource を 2 つ持つ: bgmSource (loop) / seSource (one-shot)。
    /// Inspector で各 AudioClip をアサイン。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] AudioSource bgmSource;
        [SerializeField] AudioSource seSource;

        [Header("BGM")]
        public AudioClip bgmClip;

        [Header("SE")]
        public AudioClip attackClip;
        public AudioClip hitClip;
        public AudioClip enemyDeathClip;
        public AudioClip playerDeathClip;

        [Header("Volume")]
        [Range(0f, 1f)] public float bgmVolume = 0.5f;
        [Range(0f, 1f)] public float seVolume  = 1.0f;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            // AudioSource を自動生成（Inspector でアサインしていない場合）
            if (bgmSource == null)
            {
                bgmSource      = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
            }
            if (seSource == null)
                seSource = gameObject.AddComponent<AudioSource>();
        }

        void Start()
        {
            PlayBGM(bgmClip);
        }

        // ---- BGM ----

        public void PlayBGM(AudioClip clip)
        {
            if (clip == null) return;
            bgmSource.clip   = clip;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }

        public void StopBGM() => bgmSource.Stop();

        public void SetBGMVolume(float v)
        {
            bgmVolume        = Mathf.Clamp01(v);
            bgmSource.volume = bgmVolume;
        }

        // ---- SE ----

        public void PlaySE(AudioClip clip)
        {
            if (clip == null) return;
            seSource.PlayOneShot(clip, seVolume);
        }

        public void SetSEVolume(float v) => seVolume = Mathf.Clamp01(v);

        // ---- 便利メソッド ----
        public void PlayAttack()     => PlaySE(attackClip);
        public void PlayHit()        => PlaySE(hitClip);
        public void PlayEnemyDeath() => PlaySE(enemyDeathClip);
        public void PlayPlayerDeath() => PlaySE(playerDeathClip);
    }
}
