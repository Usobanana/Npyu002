using UnityEngine;
using UnityEngine.SceneManagement;

namespace ActionGame
{
    /// <summary>
    /// ゲームの勝敗状態を管理するシングルトン。
    /// 複数 Enemy に対応: タグ "Enemy" を持つ全 GameObject を自動検索。
    /// 全員倒すと WIN。
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Player")]
        [SerializeField] Health playerHealth;

        int enemyAliveCount;
        bool gameEnded;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance = this;
        }

        void Start()
        {
            // Player
            if (playerHealth != null)
            {
                playerHealth.OnDeath += OnPlayerDeath;
                GameUI.Instance?.SetupPlayerHP(playerHealth);
            }

            // Enemy: タグ "Enemy" で全自動検索
            var enemies = GameObject.FindGameObjectsWithTag("Enemy");
            enemyAliveCount = enemies.Length;

            for (int i = 0; i < enemies.Length; i++)
            {
                var hp = enemies[i].GetComponent<Health>();
                if (hp == null) continue;

                hp.OnDeath += OnEnemyDeath;

                // 最初の敵だけ HP バーに表示
                if (i == 0) GameUI.Instance?.SetupEnemyHP(hp);
            }

            Debug.Log($"[Game] {enemyAliveCount} enemies found.");
        }

        void OnPlayerDeath()
        {
            if (gameEnded) return;
            gameEnded = true;
            Debug.Log("[Game] Game Over");
            GameUI.Instance?.ShowGameOver();
            Time.timeScale = 0f;
        }

        void OnEnemyDeath()
        {
            if (gameEnded) return;
            enemyAliveCount--;
            Debug.Log($"[Game] Enemy defeated. Remaining: {enemyAliveCount}");

            if (enemyAliveCount <= 0)
            {
                gameEnded = true;
                Debug.Log("[Game] You Win!");
                GameUI.Instance?.ShowWin();
                Time.timeScale = 0f;
            }
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }
    }
}
