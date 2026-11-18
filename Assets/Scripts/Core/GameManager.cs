using UnityEngine;
using ShooterGame.UI;

namespace ShooterGame.Core
{
    /// <summary>
    /// 遊戲核心管理器：維護遊戲狀態、分數與玩家整體數據
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("Game Data")]
        [SerializeField] private int currentScore = 0;
        [SerializeField] private int highScore = 0;

        public int Score => currentScore;
        public int HighScore => highScore;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        private void Start()
        {
            // 初始化 UI 顯示
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetScore(currentScore);
                UIManager.Instance.SetHighScore(highScore);
            }
        }

        /// <summary>
        /// 增加玩家分數並即時更新 UI
        /// </summary>
        public void AddScore(int amount)
        {
            currentScore += amount;
            if (currentScore > highScore)
            {
                highScore = currentScore;
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.SetHighScore(highScore);
                }
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetScore(currentScore);
            }
        }

        /// <summary>
        /// 重置分數
        /// </summary>
        public void ResetScore()
        {
            currentScore = 0;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetScore(currentScore);
            }
        }
    }
}
