using System.Collections;
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

        [Header("Victory Cutscene Settings")]
        [SerializeField] private GameObject dockingStationPrefab;

        [Header("Game Statistics")]
        [SerializeField] private int currentScore = 0;
        [SerializeField] private int highScore = 0;
        [SerializeField] private int totalKills = 0;

        public GameState CurrentState => currentState;
        public int Score => currentScore;
        public int HighScore => highScore;
        public int Kills => totalKills;
        public GameObject DockingStationPrefab => dockingStationPrefab;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            // 確保 AudioManager 存在
            if (AudioManager.Instance == null && Object.FindAnyObjectByType<AudioManager>() == null)
            {
                var audioGo = new GameObject("AudioManager");
                audioGo.AddComponent<AudioManager>();
            }

            // 載入持久化數據
            DataPersistenceManager.LoadData();
            highScore = DataPersistenceManager.CurrentData.highScore;
        }

        private void Start()
        {
            SetState(GameState.Playing);
            UpdateUI();

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNormalBgm();
            }
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
                    DataPersistenceManager.UpdateScoreAndKills(currentScore, totalKills);
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.StopBgm();
                        AudioManager.Instance.PlayFail();
                    }
                    if (UIManager.Instance != null)
                    {
                        UIManager.Instance.ShowGameOverPanel(true);
                        UIManager.Instance.SetSummaryData(currentScore, totalKills);
                    }
                    break;

                case GameState.Victory:
                    Time.timeScale = 0.5f;
                    DataPersistenceManager.UpdateScoreAndKills(currentScore, totalKills);
                    if (AudioManager.Instance != null)
                    {
                        AudioManager.Instance.StopBgm();
                        AudioManager.Instance.PlayPass();
                    }
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
                UIManager.Instance.SetHighScore(highScore);
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
        /// Boss 擊破後觸發自動返航停靠母艦過場動畫，完成後顯示勝利結算
        /// </summary>
        public void StartVictorySequence()
        {
            if (currentState == GameState.Victory || currentState == GameState.GameOver) return;
            StartCoroutine(VictoryCutsceneRoutine());
        }

        private System.Collections.IEnumerator VictoryCutsceneRoutine()
        {
            // 1. 清場：回收所有敵彈並清理剩餘小怪
            if (EnemyBulletPool.Instance != null)
            {
                EnemyBulletPool.Instance.ClearAllBullets();
            }

            var enemies = Object.FindObjectsByType<EnemyController>();
            foreach (var enemy in enemies)
            {
                if (enemy != null && enemy.Type != EnemyType.Boss)
                {
                    Destroy(enemy.gameObject);
                }
            }

            // 2. 鎖定玩家操作，進入自動返航停靠模式
            var player = PlayerController.Instance ?? Object.FindAnyObjectByType<PlayerController>();
            if (player != null)
            {
                player.LockControlsForDocking();
            }

            // 等待 Boss 爆炸火花消散（0.8 秒）
            yield return new WaitForSeconds(0.8f);

            // 3. 生成休息停靠站（Cruiser 1）自頂部外側進場
            GameObject stationObj = null;
            if (dockingStationPrefab != null)
            {
                stationObj = Instantiate(dockingStationPrefab, new Vector3(0f, 9.5f, 0f), Quaternion.identity);
            }
            else
            {
                stationObj = new GameObject("DockingStation");
                stationObj.transform.position = new Vector3(0f, 9.5f, 0f);
                var sr = stationObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = 3;
            }

            stationObj.transform.localScale = new Vector3(1.1f, 1.1f, 1f);

            // 4. 停靠站平滑飛入螢幕上方中央 (y: 9.5 -> 3.2)
            float stationFlyDuration = 2.0f;
            float elapsed = 0f;
            Vector3 stationStart = new Vector3(0f, 9.5f, 0f);
            Vector3 stationTarget = new Vector3(0f, 3.2f, 0f);

            while (elapsed < stationFlyDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / stationFlyDuration);
                if (stationObj != null)
                {
                    stationObj.transform.position = Vector3.Lerp(stationStart, stationTarget, t);
                }
                yield return null;
            }
            if (stationObj != null) stationObj.transform.position = stationTarget;

            yield return new WaitForSeconds(0.3f);

            // 5. 玩家戰鬥機自動飛入停靠站（Carrier 1 右側甲板跑道）
            if (player != null)
            {
                var stationScript = stationObj != null ? stationObj.GetComponent<DockingStation>() : null;
                Vector3 playerStartPos = player.transform.position;
                Vector3 playerTargetPos = stationScript != null ? stationScript.DockingWorldPosition : (stationTarget + new Vector3(0.2f, 0.35f, 0f));
                Vector3 origScale = player.transform.localScale;
                var playerSR = player.GetComponent<SpriteRenderer>();

                float dockDuration = 1.6f;
                elapsed = 0f;

                while (elapsed < dockDuration)
                {
                    elapsed += Time.deltaTime;
                    float t = Mathf.SmoothStep(0f, 1f, elapsed / dockDuration);

                    // 飛入機庫
                    player.transform.position = Vector3.Lerp(playerStartPos, playerTargetPos, t);

                    // 逐漸縮小並淡出入庫
                    float scaleRatio = Mathf.Lerp(1f, 0.15f, t);
                    player.transform.localScale = origScale * scaleRatio;

                    if (playerSR != null)
                    {
                        Color c = playerSR.color;
                        c.a = Mathf.Lerp(1f, 0f, t * t);
                        playerSR.color = c;
                    }

                    yield return null;
                }

                if (playerSR != null) playerSR.enabled = false;
            }

            // 6. 停靠完成：母艦推進發動，前進加速脫離
            yield return new WaitForSeconds(0.4f);

            elapsed = 0f;
            float departDuration = 1.0f;
            Vector3 departStart = stationObj.transform.position;
            Vector3 departTarget = departStart + Vector3.up * 1.5f;

            while (elapsed < departDuration)
            {
                elapsed += Time.deltaTime;
                float t = Mathf.SmoothStep(0f, 1f, elapsed / departDuration);
                if (stationObj != null)
                {
                    stationObj.transform.position = Vector3.Lerp(departStart, departTarget, t);
                }
                yield return null;
            }

            yield return new WaitForSeconds(0.4f);

            // 7. 彈出遊戲過關畫面
            TriggerVictory();
        }

        /// <summary>
        /// 完整重置遊戲：清空物件池、重置時間軸、重置玩家血量與分數
        /// </summary>
        public void RestartGame()
        {
            StopAllCoroutines();
            Time.timeScale = 1f;

            // 清理場上停靠站
            var stations = Object.FindObjectsByType<DockingStation>();
            foreach (var s in stations)
            {
                if (s != null) Destroy(s.gameObject);
            }

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

            // 7. 重置背景音樂為常規戰鬥曲
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayNormalBgm();
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
