using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using UnityEngine.Events;

namespace ActionGame
{
    /// <summary>
    /// ゲーム全体の UI を一元管理する。
    /// HUD (HP バー / スコア) / ポーズ / ゲームオーバー / クリア / モバイルコントロール
    /// </summary>
    public class GameUI : MonoBehaviour
    {
        public static GameUI Instance { get; private set; }

        [Header("HUD")]
        [SerializeField] Slider playerHPBar;
        [SerializeField] Slider enemyHPBar;
        [SerializeField] Text   scoreText;
        [SerializeField] Text   highScoreText;

        [Header("Panels")]
        [SerializeField] GameObject pausePanel;
        [SerializeField] GameObject gameOverPanel;
        [SerializeField] GameObject winPanel;

        [Header("Game Over / Win Text")]
        [SerializeField] Text gameOverScoreText;
        [SerializeField] Text winScoreText;

        [Header("Mobile Controls")]
        [SerializeField] GameObject mobileControls;

        bool isPaused;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            HideAll();

            if (mobileControls != null)
                mobileControls.SetActive(Application.isMobilePlatform);

            if (ScoreManager.Instance != null)
            {
                ScoreManager.Instance.OnScoreChanged     += UpdateScore;
                ScoreManager.Instance.OnHighScoreUpdated += UpdateHighScore;
                UpdateScore(ScoreManager.Instance.Score);
                UpdateHighScore(ScoreManager.Instance.HighScore);
            }

            WireButtons();
        }

        void WireButtons()
        {
            BindButton(pausePanel,    "ResumeButton", Resume);
            BindButton(pausePanel,    "QuitButton",   OnQuit);
            BindButton(gameOverPanel, "RetryButton",  OnRetry);
            BindButton(gameOverPanel, "QuitButton",   OnQuit);
            BindButton(winPanel,      "RetryButton",  OnRetry);
            BindButton(winPanel,      "QuitButton",   OnQuit);

            if (InputHandler.Instance != null)
            {
                var atkBtn = mobileControls != null
                    ? mobileControls.transform.Find("AttackButton")?.GetComponent<Button>()
                    : null;
                if (atkBtn != null)
                    atkBtn.onClick.AddListener(InputHandler.Instance.OnMobileAttack);
            }
        }

        void BindButton(GameObject panel, string childName, UnityEngine.Events.UnityAction action)
        {
            if (panel == null) return;
            var t = panel.transform.Find(childName);
            if (t == null) return;
            var btn = t.GetComponent<Button>();
            if (btn != null) btn.onClick.AddListener(action);
        }

        // ---- HP バー ----

        public void SetupPlayerHP(Health hp)
        {
            if (playerHPBar == null || hp == null) return;
            hp.OnHealthChanged += (cur, max) => UpdateBar(playerHPBar, cur, max);
            UpdateBar(playerHPBar, hp.CurrentHP, hp.MaxHP);
        }

        public void SetupEnemyHP(Health hp)
        {
            if (enemyHPBar == null || hp == null) return;
            hp.OnHealthChanged += (cur, max) => UpdateBar(enemyHPBar, cur, max);
            UpdateBar(enemyHPBar, hp.CurrentHP, hp.MaxHP);
        }

        void UpdateBar(Slider bar, float cur, float max)
        {
            if (bar != null) bar.value = max > 0 ? cur / max : 0f;
        }

        // ---- スコア ----

        void UpdateScore(int score)
        {
            if (scoreText != null) scoreText.text = $"SCORE  {score:D6}";
        }

        void UpdateHighScore(int hi)
        {
            if (highScoreText != null) highScoreText.text = $"BEST  {hi:D6}";
        }

        // ---- ポーズ ----

        public void TogglePause()
        {
            isPaused       = !isPaused;
            Time.timeScale = isPaused ? 0f : 1f;
            if (pausePanel != null) pausePanel.SetActive(isPaused);
            Cursor.lockState = isPaused ? CursorLockMode.None : CursorLockMode.Locked;
            Cursor.visible   = isPaused;
        }

        public void Resume()
        {
            if (isPaused) TogglePause();
        }

        // ---- ゲームオーバー / クリア ----

        public void ShowGameOver()
        {
            if (gameOverPanel == null) return;
            gameOverPanel.SetActive(true);
            if (gameOverScoreText != null && ScoreManager.Instance != null)
                gameOverScoreText.text = $"SCORE: {ScoreManager.Instance.Score}";
        }

        public void ShowWin()
        {
            if (winPanel == null) return;
            winPanel.SetActive(true);
            if (winScoreText != null && ScoreManager.Instance != null)
                winScoreText.text = $"SCORE: {ScoreManager.Instance.Score}";
        }

        // ---- ボタンコールバック ----

        public void OnRetry()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        public void OnQuit()
        {
            Time.timeScale = 1f;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        // ---- Pause キー ----

        void Update()
        {
            if (InputHandler.Instance != null && InputHandler.Instance.PausePressed)
                TogglePause();
        }

        void HideAll()
        {
            if (pausePanel    != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (winPanel      != null) winPanel.SetActive(false);
        }
    }
}
