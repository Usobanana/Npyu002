using UnityEngine;
using UnityEngine.SceneManagement;

namespace ActionGame
{
    /// <summary>
    /// ゲームの勝敗状態を管理するシングルトン。
    /// Inspector で playerHealth, enemyHealth, gameOverUI, winUI をアサイン。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] Health playerHealth;
        [SerializeField] Health enemyHealth;

        [Header("UI Panels")]
        [SerializeField] GameObject gameOverUI;
        [SerializeField] GameObject winUI;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (gameOverUI) gameOverUI.SetActive(false);
            if (winUI)      winUI.SetActive(false);

            if (playerHealth) playerHealth.OnDeath += OnPlayerDeath;
            if (enemyHealth)  enemyHealth.OnDeath  += OnEnemyDeath;
        }

        void OnPlayerDeath()
        {
            Debug.Log("[Game] Game Over");
            if (gameOverUI) gameOverUI.SetActive(true);
            Time.timeScale = 0f;
        }

        void OnEnemyDeath()
        {
            Debug.Log("[Game] You Win!");
            if (winUI) winUI.SetActive(true);
            Time.timeScale = 0f;
        }

        /// <summary>UI ボタン等から呼ぶリトライ処理</summary>
        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
