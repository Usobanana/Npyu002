using UnityEngine;
using UnityEngine.SceneManagement;

namespace ActionGame
{
    /// <summary>
    /// ゲームの勝敗状態を管理するシングルトン。
    /// Inspector で playerHealth, enemyHealth をアサイン。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("References")]
        [SerializeField] Health playerHealth;
        [SerializeField] Health enemyHealth;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            if (playerHealth != null)
            {
                playerHealth.OnDeath += OnPlayerDeath;
                GameUI.Instance?.SetupPlayerHP(playerHealth);
            }

            if (enemyHealth != null)
            {
                enemyHealth.OnDeath += OnEnemyDeath;
                GameUI.Instance?.SetupEnemyHP(enemyHealth);
            }
        }

        void OnPlayerDeath()
        {
            Debug.Log("[Game] Game Over");
            GameUI.Instance?.ShowGameOver();
            Time.timeScale = 0f;
        }

        void OnEnemyDeath()
        {
            Debug.Log("[Game] You Win!");
            GameUI.Instance?.ShowWin();
            Time.timeScale = 0f;
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
