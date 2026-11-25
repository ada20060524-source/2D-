using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using ShooterGame.Core;

namespace ShooterGame.UI
{
    /// <summary>
    /// 全域極簡邊緣化 UI 管理器：血條漸層變化、Warning動態閃爍、結算數據與暫停重開按鈕
    /// </summary>
    public class UIManager : MonoBehaviour
    {
        public static UIManager Instance { get; private set; }

        [Header("=== 1. Top-Left Zone (玩家 HP / 殘機) ===")]
        [SerializeField] private RectTransform topLeftContainer;
        [SerializeField] private TextMeshProUGUI hpText;

        [Header("=== 2. Top-Center Zone (Boss 血條) ===")]
        [SerializeField] private RectTransform topCenterContainer;
        [SerializeField] private Slider bossHealthSlider;
        [SerializeField] private Image bossHealthFillImage;

        [Header("=== 3. Top-Right Zone (得分 / 暫停按鈕) ===")]
        [SerializeField] private RectTransform topRightContainer;
        [SerializeField] private TextMeshProUGUI scoreText;
        [SerializeField] private TextMeshProUGUI highScoreText;
        [SerializeField] private Button pauseButton;

        [Header("=== 4. Center Zone (動態 Warning / 關卡橫幅) ===")]
        [SerializeField] private RectTransform centerContainer;
        [SerializeField] private TextMeshProUGUI warningBanner;

        [Header("=== 5. Bottom-Left Zone (保命炸彈 / 火力等級 / 護盾狀態) ===")]
        [SerializeField] private RectTransform bottomLeftContainer;
        [SerializeField] private TextMeshProUGUI bombText;
        [SerializeField] private TextMeshProUGUI powerText;
        [SerializeField] private TextMeshProUGUI shieldText;

        [Header("=== 6. Overlay Zone (暫停 / 結算面板) ===")]
        [SerializeField] private RectTransform overlayContainer;
        [SerializeField] private GameObject pausePanel;
        [SerializeField] private GameObject gameOverPanel;
        [SerializeField] private GameObject victoryPanel;

        [Header("Overlay Summary Texts & Buttons")]
        [SerializeField] private TextMeshProUGUI gameOverScoreText;
        [SerializeField] private TextMeshProUGUI gameOverKillsText;
        [SerializeField] private TextMeshProUGUI victoryScoreText;
        [SerializeField] private TextMeshProUGUI victoryKillsText;

        private Coroutine warningCoroutine;

        public Image BossHealthFillImage => bossHealthFillImage;
        public Slider BossHealthSlider => bossHealthSlider;
        public GameObject PausePanel => pausePanel;
        public GameObject GameOverPanel => gameOverPanel;
        public GameObject VictoryPanel => victoryPanel;
        public TextMeshProUGUI WarningBanner => warningBanner;
        public TextMeshProUGUI ShieldText => shieldText;

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
            if (topCenterContainer != null) topCenterContainer.gameObject.SetActive(false);
            if (centerContainer != null) centerContainer.gameObject.SetActive(false);
            if (pausePanel != null) pausePanel.SetActive(false);
            if (gameOverPanel != null) gameOverPanel.SetActive(false);
            if (victoryPanel != null) victoryPanel.SetActive(false);

            // 綁定暫停按鈕
            if (pauseButton != null)
            {
                pauseButton.onClick.RemoveAllListeners();
                pauseButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.TogglePause();
                    }
                });
            }

            // 自動尋找並綁定 GameOver 面板下的所有按鈕（如 Retry / Restart）
            if (gameOverPanel != null)
            {
                var buttons = gameOverPanel.GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(OnRestartClicked);
                    }
                }
            }

            // 自動尋找並綁定 Victory 面板下的所有按鈕（如 Replay / Play Again / Restart）
            if (victoryPanel != null)
            {
                var buttons = victoryPanel.GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    if (btn != null)
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(OnRestartClicked);
                    }
                }
            }

            // 自動尋找並綁定 Pause 面板下的 Resume 與 Restart 按鈕
            if (pausePanel != null)
            {
                var buttons = pausePanel.GetComponentsInChildren<Button>(true);
                foreach (var btn in buttons)
                {
                    if (btn == null) continue;
                    if (btn.gameObject.name.ToLower().Contains("resume"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(OnResumeClicked);
                    }
                    else if (btn.gameObject.name.ToLower().Contains("restart") || btn.gameObject.name.ToLower().Contains("retry"))
                    {
                        btn.onClick.RemoveAllListeners();
                        btn.onClick.AddListener(OnRestartClicked);
                    }
                }
            }
        }

        public void SetHP(int currentHp, int maxHp)
        {
            if (hpText != null)
            {
                hpText.text = $"HP: {currentHp}/{maxHp}";
            }
        }

        public void SetHPInfinite()
        {
            if (hpText != null)
            {
                hpText.text = "HP: 3/3";
            }
        }

        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {score}";
            }
        }

        public void SetHighScore(int highScore)
        {
            if (highScoreText != null)
            {
                highScoreText.text = $"HI: {highScore}";
            }
        }

        public void SetBossHealth(float ratio)
        {
            float clampedRatio = Mathf.Clamp01(ratio);
            if (bossHealthSlider != null)
            {
                bossHealthSlider.value = clampedRatio;
            }

            // 綠 -> 黃 -> 紅 多階段血條動態顏色變化
            if (bossHealthFillImage != null)
            {
                if (clampedRatio > 0.6f)
                {
                    bossHealthFillImage.color = new Color(0.2f, 1f, 0.4f, 1f); // 綠
                }
                else if (clampedRatio > 0.3f)
                {
                    bossHealthFillImage.color = new Color(1f, 0.85f, 0.2f, 1f); // 黃
                }
                else
                {
                    bossHealthFillImage.color = new Color(1f, 0.25f, 0.25f, 1f); // 紅
                }
            }
        }

        public void ShowBossBar(bool show)
        {
            if (topCenterContainer != null)
            {
                topCenterContainer.gameObject.SetActive(show);
            }
        }

        public void ShowWarning(bool show)
        {
            if (centerContainer != null)
            {
                centerContainer.gameObject.SetActive(show);
            }

            if (show)
            {
                if (warningCoroutine != null) StopCoroutine(warningCoroutine);
                warningCoroutine = StartCoroutine(WarningFlashRoutine());
            }
            else
            {
                if (warningCoroutine != null)
                {
                    StopCoroutine(warningCoroutine);
                    warningCoroutine = null;
                }
            }
        }

        private IEnumerator WarningFlashRoutine()
        {
            if (warningBanner == null) yield break;

            warningBanner.text = "WARNING!\nBOSS APPROACHING";
            while (true)
            {
                warningBanner.color = new Color(1f, 0.2f, 0.2f, 1f);
                yield return new WaitForSeconds(0.25f);
                warningBanner.color = new Color(1f, 0.9f, 0.2f, 1f);
                yield return new WaitForSeconds(0.25f);
            }
        }

        public void SetBombCount(int count)
        {
            if (bombText != null)
            {
                bombText.text = $"BOMB: {count}";
            }
        }

        public void SetShield(bool active, int hitsRemaining, float timeLeft)
        {
            if (shieldText != null)
            {
                if (active)
                {
                    shieldText.gameObject.SetActive(true);
                    shieldText.text = $"SHIELD: {hitsRemaining} ({Mathf.CeilToInt(timeLeft)}s)";
                }
                else
                {
                    shieldText.gameObject.SetActive(false);
                }
            }
        }

        public void SetPowerLevel(int level)
        {
            if (powerText != null)
            {
                powerText.text = $"POWER: {level}";
            }
        }

        public void ShowPausePanel(bool show)
        {
            if (pausePanel != null)
            {
                pausePanel.SetActive(show);
            }
        }

        public void ShowGameOverPanel(bool show)
        {
            if (gameOverPanel != null)
            {
                gameOverPanel.SetActive(show);
            }
        }

        public void ShowVictoryPanel(bool show)
        {
            if (victoryPanel != null)
            {
                victoryPanel.SetActive(show);
            }
        }

        public void SetSummaryData(int score, int kills)
        {
            if (gameOverScoreText != null) gameOverScoreText.text = $"SCORE: {score}";
            if (gameOverKillsText != null) gameOverKillsText.text = $"KILLS: {kills}";
            if (victoryScoreText != null) victoryScoreText.text = $"FINAL SCORE: {score}";
            if (victoryKillsText != null) victoryKillsText.text = $"TOTAL KILLS: {kills}";
        }

        // 按鈕回調呼叫
        public void OnResumeClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.TogglePause();
            }
        }

        public void OnRestartClicked()
        {
            if (GameManager.Instance != null)
            {
                GameManager.Instance.RestartGame();
            }
        }

        /// <summary>
        /// 匯出玩家存檔為 JSON 字串
        /// </summary>
        public string ExportSaveData()
        {
            string json = DataPersistenceManager.ExportToJson();
            Debug.Log($"[UIManager] Save Data Exported:\n{json}");
            return json;
        }

        /// <summary>
        /// 匯入玩家存檔 JSON 字串
        /// </summary>
        public bool ImportSaveData(string json)
        {
            bool success = DataPersistenceManager.ImportFromJson(json);
            if (success && GameManager.Instance != null)
            {
                int high = DataPersistenceManager.CurrentData.highScore;
                SetHighScore(high);
            }
            return success;
        }

        /// <summary>
        /// 匯入自訂關卡排程 JSON 字串
        /// </summary>
        public bool ImportWaveConfig(string json)
        {
            if (Wave.WaveSystem.Instance != null)
            {
                return Wave.WaveSystem.Instance.ApplyCustomWaveConfig(json);
            }
            return false;
        }
    }
}
