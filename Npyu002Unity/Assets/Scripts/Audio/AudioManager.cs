using UnityEngine;

namespace ActionGame
{
    /// <summary>
    /// BGM / SE を一元管理する Singleton。
    /// 複数クリップを登録すればランダムに選んで再生するため、
    /// 同じ音が連続して不自然になるのを防げる。
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Sources")]
        [SerializeField] AudioSource bgmSource;
        [SerializeField] AudioSource seSource;

        [Header("BGM (複数登録でランダム選択)")]
        public AudioClip[] bgmClips;

        [Header("スイング SE (SwingSword/ など複数登録推奨)")]
        public AudioClip[] attackClips;

        [Header("ヒット SE (Slash/ など複数登録推奨)")]
        public AudioClip[] hitClips;

        [Header("その他 SE")]
        public AudioClip enemyDeathClip;
        public AudioClip playerDeathClip;

        [Header("Volume")]
        [Range(0f, 1f)] public float bgmVolume = 0.4f;
        [Range(0f, 1f)] public float seVolume  = 1.0f;

        // 後方互換：単体クリップも受け付ける（旧コード向け）
        public AudioClip bgmClip
        {
            set { bgmClips = new[] { value }; }
        }
        public AudioClip attackClip
        {
            set { attackClips = new[] { value }; }
        }
        public AudioClip hitClip
        {
            set { hitClips = new[] { value }; }
        }

        // ── 直前に再生したクリップを覚えて連続回避 ──────────────
        AudioClip lastAttackClip;
        AudioClip lastHitClip;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            if (bgmSource == null)
            {
                bgmSource      = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
            }
            if (seSource == null)
                seSource = gameObject.AddComponent<AudioSource>();
        }

        void Start() => PlayBGMRandom();

        // ── BGM ──────────────────────────────────────────────────

        public void PlayBGMRandom()
        {
            var clip = PickRandom(bgmClips, null);
            if (clip == null) return;
            bgmSource.clip   = clip;
            bgmSource.volume = bgmVolume;
            bgmSource.Play();
        }

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

        // ── SE ───────────────────────────────────────────────────

        public void PlaySE(AudioClip clip)
        {
            if (clip == null) return;
            seSource.PlayOneShot(clip, seVolume);
        }

        public void SetSEVolume(float v) => seVolume = Mathf.Clamp01(v);

        // ── 便利メソッド ─────────────────────────────────────────

        /// <summary>スイング SE（attackClips からランダム、直前と重複しない）</summary>
        public void PlayAttack()
        {
            var clip = PickRandom(attackClips, lastAttackClip);
            if (clip == null) return;
            lastAttackClip = clip;
            seSource.PlayOneShot(clip, seVolume);
        }

        public void PlayAttackScaled(float scale = 1f)
        {
            var clip = PickRandom(attackClips, lastAttackClip);
            if (clip == null) return;
            lastAttackClip = clip;
            seSource.PlayOneShot(clip, seVolume * Mathf.Clamp(scale, 0.1f, 2f));
        }

        /// <summary>ヒット SE（hitClips からランダム、直前と重複しない）</summary>
        public void PlayHit()
        {
            var clip = PickRandom(hitClips, lastHitClip);
            if (clip == null) return;
            lastHitClip = clip;
            seSource.PlayOneShot(clip, seVolume);
        }

        public void PlayEnemyDeath()  => PlaySE(enemyDeathClip);
        public void PlayPlayerDeath() => PlaySE(playerDeathClip);

        public void PlaySEScaled(AudioClip clip, float scale)
        {
            if (clip == null) return;
            seSource.PlayOneShot(clip, seVolume * scale);
        }

        // ── ユーティリティ ───────────────────────────────────────

        /// <summary>配列からランダムに 1 つ選ぶ。exclude と同じなら別のものを選ぶ。</summary>
        static AudioClip PickRandom(AudioClip[] clips, AudioClip exclude)
        {
            if (clips == null || clips.Length == 0) return null;
            if (clips.Length == 1) return clips[0];

            AudioClip picked = clips[Random.Range(0, clips.Length)];
            // 直前と同じだった場合は 1 度だけリトライ
            if (picked == exclude)
                picked = clips[Random.Range(0, clips.Length)];
            return picked;
        }
    }
}
