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
        [SerializeField] private Button pauseButton;

        [Header("=== 4. Center Zone (動態 Warning / 關卡橫幅) ===")]
        [SerializeField] private RectTransform centerContainer;
        [SerializeField] private TextMeshProUGUI warningBanner;

        [Header("=== 5. Bottom-Left Zone (保命炸彈 / 火力等級) ===")]
        [SerializeField] private RectTransform bottomLeftContainer;
        [SerializeField] private TextMeshProUGUI bombText;
        [SerializeField] private TextMeshProUGUI powerText;

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

            if (pauseButton != null)
            {
                pauseButton.onClick.AddListener(() =>
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.TogglePause();
                    }
                });
            }
        }

        public void SetHP(int currentHp, int maxHp)
        {
            if (hpText != null)
            {
                hpText.text = $"HP: {currentHp}/{maxHp}";
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
    }
}
