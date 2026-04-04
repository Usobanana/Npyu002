using System;
using UnityEngine;

namespace ActionGame
{
    /// <summary>
    /// スコア管理 Singleton。
    /// 敵撃破でスコアを加算し、PlayerPrefs にハイスコアを保存する。
    /// </summary>
    public class ScoreManager : MonoBehaviour
    {
        public static ScoreManager Instance { get; private set; }

        const string HighScoreKey = "HighScore";

        public int Score     { get; private set; }
        public int HighScore { get; private set; }

        public event Action<int> OnScoreChanged;
        public event Action<int> OnHighScoreUpdated;

        void Awake()
        {
            if (Instance != null && Instance != this) { Destroy(gameObject); return; }
            Instance  = this;
            HighScore = PlayerPrefs.GetInt(HighScoreKey, 0);
        }

        public void AddScore(int amount)
        {
            if (amount <= 0) return;
            Score += amount;
            OnScoreChanged?.Invoke(Score);

            if (Score > HighScore)
            {
                HighScore = Score;
                PlayerPrefs.SetInt(HighScoreKey, HighScore);
                PlayerPrefs.Save();
                OnHighScoreUpdated?.Invoke(HighScore);
            }
        }

        public void ResetScore()
        {
            Score = 0;
            OnScoreChanged?.Invoke(Score);
        }
    }
}
