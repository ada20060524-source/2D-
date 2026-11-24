using UnityEngine;
using ShooterGame.UI;
using ShooterGame.Player;
using ShooterGame.Pool;
using ShooterGame.Wave;
using ShooterGame.Enemy;
using ShooterGame.Item;

namespace ShooterGame.Core
{
    /// <summary>
    /// 遊戲全域核心狀態機：維護 Title/Playing/Warning/Paused/GameOver/Victory 狀態迴圈與數據
    /// </summary>
    public class GameManager : MonoBehaviour
    {
        public static GameManager Instance { get; private set; }

        [Header("State Machine")]
        [SerializeField] private GameState currentState = GameState.Playing;

        [Header("Game Statistics")]
        [SerializeField] private int currentScore = 0;
        [SerializeField] private int highScore = 0;
        [SerializeField] private int totalKills = 0;

        public GameState CurrentState => currentState;
        public int Score => currentScore;
        public int HighScore => highScore;
        public int Kills => totalKills;

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
            SetState(GameState.Playing);
            UpdateUI();
        }

        private void Update()
        {
            // 監聽暫停鍵（ESC 或 P）
            #if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.escapeKey.wasPressedThisFrame)
            {
                TogglePause();
            }
            #endif

            #if ENABLE_LEGACY_INPUT_MANAGER
            if (Input.GetKeyDown(KeyCode.Escape) || Input.GetKeyDown(KeyCode.P))
            {
                TogglePause();
            }
            #endif
        }

        public void SetState(GameState newState)
        {
            currentState = newState;

            switch (currentState)
            {
                case GameState.Playing:
                    Time.timeScale = 1f;
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowPausePanel(false);
                        UIManager.Instance.ShowGameOverPanel(false);
                        UIManager.Instance.ShowVictoryPanel(false);
                    }
                    break;

                case GameState.Warning:
                    // 警示狀態由 WaveSystem 控制過渡演出
                    Time.timeScale = 1f;
                    break;

                case GameState.Paused:
                    Time.timeScale = 0f;
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowPausePanel(true);
                    }
                    break;

                case GameState.GameOver:
                    Time.timeScale = 0.3f; // 慢動作感受死亡回饋，隨後彈出面板
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowGameOverPanel(true);
                        UIManager.Instance.SetSummaryData(currentScore, totalKills);
                    }
                    break;

                case GameState.Victory:
                    Time.timeScale = 0.5f;
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowVictoryPanel(true);
                        UIManager.Instance.SetSummaryData(currentScore, totalKills);
                    }
                    break;
            }
        }

        public void AddScore(int amount)
        {
            currentScore += amount;
            if (currentScore > highScore)
            {
                highScore = currentScore;
            }

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetScore(currentScore);
            }
        }

        public void AddKill()
        {
            totalKills++;
        }

        public void TogglePause()
        {
            if (currentState == GameState.Playing || currentState == GameState.Warning)
            {
                SetState(GameState.Paused);
            }
            else if (currentState == GameState.Paused)
            {
                SetState(GameState.Playing);
            }
        }

        public void TriggerGameOver()
        {
            if (currentState == GameState.GameOver) return;
            SetState(GameState.GameOver);
        }

        public void TriggerVictory()
        {
            if (currentState == GameState.Victory) return;
            SetState(GameState.Victory);
        }

        /// <summary>
        /// 完整重置遊戲：清空物件池、重置時間軸、重置玩家血量與分數
        /// </summary>
        public void RestartGame()
        {
            Time.timeScale = 1f;

            // 1. 清空所有子彈池
            if (BulletPool.Instance != null)
            {
                BulletPool.Instance.InitializePool();
            }
            if (EnemyBulletPool.Instance != null)
            {
                EnemyBulletPool.Instance.ClearAllBullets();
            }

            // 2. 清除場上所有敵機與掉落道具
            var enemies = Object.FindObjectsByType<EnemyController>();
            foreach (var enemy in enemies)
            {
                if (enemy != null) Destroy(enemy.gameObject);
            }

            var items = Object.FindObjectsByType<PowerUpItem>();
            foreach (var item in items)
            {
                if (item != null) Destroy(item.gameObject);
            }

            // 3. 重置統計數據
            currentScore = 0;
            totalKills = 0;

            // 4. 重置玩家機體
            if (PlayerController.Instance != null)
            {
                PlayerController.Instance.ResetPlayer();
                PlayerController.Instance.transform.position = new Vector3(0f, -5f, 0f);
            }

            // 5. 重置波次系統
            var waveSys = WaveSystem.Instance ?? Object.FindAnyObjectByType<WaveSystem>();
            if (waveSys != null)
            {
                waveSys.RestartTimeline();
            }

            // 6. 重置 UI 面板與血條
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowBossBar(false);
                UIManager.Instance.ShowWarning(false);
                UIManager.Instance.ShowPausePanel(false);
                UIManager.Instance.ShowGameOverPanel(false);
                UIManager.Instance.ShowVictoryPanel(false);
                UIManager.Instance.SetScore(currentScore);
            }

            SetState(GameState.Playing);
        }

        private void UpdateUI()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetScore(currentScore);
            }
        }
    }
}
