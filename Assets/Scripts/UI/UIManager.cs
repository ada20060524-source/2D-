using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace ShooterGame.UI
{
    /// <summary>
    /// 全域極簡邊緣化 UI 管理器：全面採用 TextMeshPro (TMP) 渲染高解析度極致銳利文字
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

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
        }

        public void SetHP(int currentHp, int maxHp)
        {
            if (hpText != null)
            {
                hpText.text = $"HP: {currentHp}/{maxHp}";
            }
        }

        /// <summary>
        /// 更新分數：自然數值遞增顯示
        /// </summary>
        public void SetScore(int score)
        {
            if (scoreText != null)
            {
                scoreText.text = $"SCORE: {score}";
            }
        }

        public void SetHighScore(int highScore)
        {
            // 配合使用者需求僅保留 Score，此處保留空實作確保系統相容性
        }

        public void SetBossHealth(float ratio)
        {
            if (bossHealthSlider != null)
            {
                bossHealthSlider.value = Mathf.Clamp01(ratio);
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
            if (warningBanner != null)
            {
                warningBanner.gameObject.SetActive(show);
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
    }
}
