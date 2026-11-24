using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Enemy;
using ShooterGame.UI;

namespace ShooterGame.Wave
{
    /// <summary>
    /// 時間軸波次控制器：0-30s 雜兵、30-60s 精英怪+道具、60-90s 高壓彈幕、90s+ Boss戰
    /// </summary>
    public class WaveSystem : MonoBehaviour
    {
        public static WaveSystem Instance { get; private set; }

        [Header("Enemy Prefabs")]
        [SerializeField] private GameObject minionPrefab;
        [SerializeField] private GameObject elitePrefab;
        [SerializeField] private GameObject bossPrefab;

        [Header("Timeline Settings")]
        [SerializeField] private float timeline = 0f;
        [SerializeField] private bool autoAdvance = true;

        private float minionSpawnTimer = 0f;
        private float eliteSpawnTimer = 0f;
        private float formationSpawnTimer = 0f;

        private bool hasTriggeredBossSequence = false;
        private bool isWarningSequenceActive = false;

        public float Timeline => timeline;
        public bool IsBossActive => hasTriggeredBossSequence;

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
            RestartTimeline();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && (gm.CurrentState == GameState.Paused || gm.CurrentState == GameState.GameOver || gm.CurrentState == GameState.Victory))
            {
                return;
            }

            if (!autoAdvance || isWarningSequenceActive) return;

            timeline += Time.deltaTime;
            ProcessTimeline();
        }

        public void ProcessTimeline()
        {
            // 階段 1：0 – 30 秒（前期雜兵試探波次）
            if (timeline < 30f)
            {
                minionSpawnTimer -= Time.deltaTime;
                if (minionSpawnTimer <= 0f)
                {
                    SpawnMinion();
                    minionSpawnTimer = 2.0f;
                }
            }
            // 階段 2：30 – 60 秒（中段精英怪進場與道具補給）
            else if (timeline >= 30f && timeline < 60f)
            {
                // 少量雜兵
                minionSpawnTimer -= Time.deltaTime;
                if (minionSpawnTimer <= 0f)
                {
                    SpawnMinion();
                    minionSpawnTimer = 2.8f;
                }

                // 精英怪（擊破掉落 PowerUp）
                eliteSpawnTimer -= Time.deltaTime;
                if (eliteSpawnTimer <= 0f)
                {
                    SpawnElite();
                    eliteSpawnTimer = 11f; // 每 11 秒進場一隻精英怪
                }
            }
            // 階段 3：60 – 90 秒（高密度敵機陣型與彈幕壓力）
            else if (timeline >= 60f && timeline < 90f)
            {
                // 連續小隊陣型進場
                formationSpawnTimer -= Time.deltaTime;
                if (formationSpawnTimer <= 0f)
                {
                    SpawnMinionFormation();
                    formationSpawnTimer = 4.5f;
                }

                // 偶爾穿插精英怪
                eliteSpawnTimer -= Time.deltaTime;
                if (eliteSpawnTimer <= 0f)
                {
                    SpawnElite();
                    eliteSpawnTimer = 14f;
                }
            }
            // 階段 4：90 秒後（Boss 戰觸發）
            else if (timeline >= 90f && !hasTriggeredBossSequence)
            {
                hasTriggeredBossSequence = true;
                StartCoroutine(BossEntranceSequence());
            }
        }

        private void SpawnMinion(float? customX = null)
        {
            if (minionPrefab == null || ViewportManager.Instance == null) return;

            float minX = ViewportManager.Instance.MinX + 0.8f;
            float maxX = ViewportManager.Instance.MaxX - 0.8f;
            float x = customX ?? Random.Range(minX, maxX);
            float y = ViewportManager.Instance.MaxY + 1.2f;

            Instantiate(minionPrefab, new Vector3(x, y, 0f), Quaternion.Euler(0, 0, 180));
        }

        private void SpawnElite()
        {
            if (elitePrefab == null || ViewportManager.Instance == null) return;

            float minX = ViewportManager.Instance.MinX + 1.2f;
            float maxX = ViewportManager.Instance.MaxX - 1.2f;
            float x = Random.Range(minX, maxX);
            float y = ViewportManager.Instance.MaxY + 1.5f;

            Instantiate(elitePrefab, new Vector3(x, y, 0f), Quaternion.Euler(0, 0, 180));
        }

        private void SpawnMinionFormation()
        {
            if (minionPrefab == null || ViewportManager.Instance == null) return;

            // V 字小隊 3 隻
            float centerX = Random.Range(-1.5f, 1.5f);
            float startY = ViewportManager.Instance.MaxY + 1.5f;

            Instantiate(minionPrefab, new Vector3(centerX, startY, 0f), Quaternion.Euler(0, 0, 180));
            Instantiate(minionPrefab, new Vector3(centerX - 1.2f, startY + 0.8f, 0f), Quaternion.Euler(0, 0, 180));
            Instantiate(minionPrefab, new Vector3(centerX + 1.2f, startY + 0.8f, 0f), Quaternion.Euler(0, 0, 180));
        }

        private IEnumerator BossEntranceSequence()
        {
            isWarningSequenceActive = true;

            // 1. 切換 GameState 進入 Warning 狀態
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Warning);
            }

            // 2. 顯示動態 WARNING 標誌
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowWarning(true);
            }

            yield return new WaitForSeconds(2.5f);

            // 3. 關閉 WARNING 標誌
            if (UIManager.Instance != null)
            {
                UIManager.Instance.ShowWarning(false);
            }

            // 4. 切換回 Playing 狀態並生成 Boss
            if (GameManager.Instance != null)
            {
                GameManager.Instance.SetState(GameState.Playing);
            }

            if (bossPrefab != null && ViewportManager.Instance != null)
            {
                float spawnY = ViewportManager.Instance.MaxY + 2.5f;
                Instantiate(bossPrefab, new Vector3(0f, spawnY, 0f), Quaternion.Euler(0, 0, 180));
            }

            isWarningSequenceActive = false;
        }

        public void RestartTimeline()
        {
            StopAllCoroutines();
            timeline = 0f;
            minionSpawnTimer = 1.0f;
            eliteSpawnTimer = 10f;
            formationSpawnTimer = 3f;
            hasTriggeredBossSequence = false;
            isWarningSequenceActive = false;
        }

        /// <summary>
        /// 跳轉時間軸進度（用於 AI 自動測試或調試）
        /// </summary>
        public void SkipTimeline(float targetTime)
        {
            timeline = targetTime;
            ProcessTimeline();
        }
    }
}
