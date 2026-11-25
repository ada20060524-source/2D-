using System.Collections;
using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Pool;
using ShooterGame.Player;
using ShooterGame.UI;

namespace ShooterGame.Enemy
{
    public enum EnemyType
    {
        Minion,
        Elite,
        Boss
    }

    /// <summary>
    /// 敵人與 Boss 控制器：支援雜兵、精英怪（掉落道具）與多階段彈幕 Boss
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Enemy Classification")]
        [SerializeField] private EnemyType enemyType = EnemyType.Minion;

        [Header("Enemy Stats")]
        [SerializeField] private int maxHp = 3;
        [SerializeField] private int currentHp = 3;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private int hitScoreValue = 20;
        [SerializeField] private int killScoreValue = 100;

        [Header("Combat & Shooting")]
        [SerializeField] private bool canShoot = true;
        [SerializeField] private float fireInterval = 1.8f;
        [SerializeField] private float bulletSpeed = 5.5f;
        [SerializeField] private Transform firePoint;

        [Header("Drops & FX")]
        [SerializeField] private GameObject powerUpPrefab;
        [SerializeField] private GameObject bombItemPrefab;
        [SerializeField] private GameObject shieldItemPrefab;
        [SerializeField] private float minionDropChance = 0.25f;
        [SerializeField] private GameObject explosionPrefab;

        [Header("Boss Laser Cannon")]
        [SerializeField] private BossLaserCannon laserCannon;
        [SerializeField] private float laserInterval = 8.5f;
        private float laserTimer = 4.0f;

        private SpriteRenderer spriteRenderer;
        private Color originalColor = Color.white;
        private float shootTimer = 0f;
        private bool isDead = false;

        // 精英怪與 Boss 的懸停位移控制
        private bool hasArrivedAtTarget = false;
        private float targetHoverY = 5f;
        private float hoverTimer = 0f;

        public EnemyType Type => enemyType;
        public int CurrentHP => currentHp;
        public int MaxHP => maxHp;
        public int HitScoreValue => hitScoreValue;
        public int KillScoreValue => killScoreValue;
        public bool IsDead => isDead;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            if (spriteRenderer != null)
            {
                originalColor = spriteRenderer.color;
            }
            gameObject.layer = GameLayers.Enemy;
            currentHp = maxHp;
        }

        private void Start()
        {
            if (enemyType == EnemyType.Boss)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowBossBar(true);
                    UIManager.Instance.SetBossHealth(1f);
                }
                targetHoverY = ViewportManager.Instance != null ? ViewportManager.Instance.MaxY - 3.2f : 4.8f;

                if (laserCannon == null)
                {
                    laserCannon = GetComponent<BossLaserCannon>();
                    if (laserCannon == null)
                    {
                        laserCannon = gameObject.AddComponent<BossLaserCannon>();
                    }
                }
                laserTimer = 3.5f; // 進場 3.5 秒後釋放首次直線光束能量砲
            }
            else if (enemyType == EnemyType.Elite)
            {
                targetHoverY = ViewportManager.Instance != null ? ViewportManager.Instance.MaxY - 2.8f : 5.2f;
            }
            shootTimer = Random.Range(0.4f, fireInterval);
        }

        private void OnEnable()
        {
            currentHp = maxHp;
            isDead = false;
            hasArrivedAtTarget = false;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }

        private void Update()
        {
            if (isDead) return;

            HandleMovement();
            HandleShooting();
        }

        private void HandleMovement()
        {
            if (enemyType == EnemyType.Minion)
            {
                // 雜兵：穩定向下巡航
                transform.position += Vector3.down * (moveSpeed * Time.deltaTime);

                // 超出視口自動銷毀
                if (ViewportManager.Instance != null && transform.position.y < ViewportManager.Instance.MinY - 1.5f)
                {
                    SafelyDestroy();
                }
            }
            else if (enemyType == EnemyType.Elite)
            {
                // 精英怪：向下進場到達懸停高度後，左右平滑擺盪
                if (!hasArrivedAtTarget)
                {
                    transform.position += Vector3.down * (moveSpeed * Time.deltaTime);
                    if (transform.position.y <= targetHoverY)
                    {
                        hasArrivedAtTarget = true;
                    }
                }
                else
                {
                    hoverTimer += Time.deltaTime * 1.5f;
                    float minX = ViewportManager.Instance != null ? ViewportManager.Instance.MinX + 1.2f : -3f;
                    float maxX = ViewportManager.Instance != null ? ViewportManager.Instance.MaxX - 1.2f : 3f;
                    float xPos = Mathf.PingPong(hoverTimer * moveSpeed, maxX - minX) + minX;
                    transform.position = new Vector3(xPos, targetHoverY + Mathf.Sin(hoverTimer * 2f) * 0.3f, 0f);
                }
            }
            else if (enemyType == EnemyType.Boss)
            {
                // 關卡 Boss：威嚴進場，並於上方寬幅平滑擺動
                if (!hasArrivedAtTarget)
                {
                    transform.position = Vector3.MoveTowards(transform.position,
                        new Vector3(transform.position.x, targetHoverY, 0f), moveSpeed * Time.deltaTime);
                    if (Mathf.Abs(transform.position.y - targetHoverY) < 0.1f)
                    {
                        hasArrivedAtTarget = true;
                    }
                }
                else
                {
                    // 若正在發射光束能量砲，暫停平移以鎖定射擊
                    if (laserCannon != null && laserCannon.IsFiring)
                    {
                        return;
                    }

                    hoverTimer += Time.deltaTime * 0.8f;
                    float minX = ViewportManager.Instance != null ? ViewportManager.Instance.MinX + 1.8f : -2.5f;
                    float maxX = ViewportManager.Instance != null ? ViewportManager.Instance.MaxX - 1.8f : 2.5f;
                    float xPos = Mathf.PingPong(hoverTimer * moveSpeed, maxX - minX) + minX;
                    transform.position = new Vector3(xPos, targetHoverY + Mathf.Sin(hoverTimer * 1.5f) * 0.25f, 0f);
                }
            }
        }

        private void HandleShooting()
        {
            if (!canShoot) return;

            // Boss 專屬光束能量砲邏輯
            if (enemyType == EnemyType.Boss && hasArrivedAtTarget)
            {
                laserTimer -= Time.deltaTime;
                if (laserTimer <= 0f && (laserCannon == null || !laserCannon.IsFiring))
                {
                    if (laserCannon != null)
                    {
                        laserCannon.FireLaser();
                    }
                    laserTimer = laserInterval;
                }

                // 光束射擊期間暫停常規散彈，凸顯主砲壓制震撼感
                if (laserCannon != null && laserCannon.IsFiring)
                {
                    return;
                }
            }

            if (EnemyBulletPool.Instance == null) return;

            shootTimer -= Time.deltaTime;
            if (shootTimer <= 0f)
            {
                FirePattern();
                shootTimer = fireInterval;
            }
        }

        private void FirePattern()
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.down * 0.5f;
            Vector2 toPlayer = Vector2.down;
            if (PlayerController.Instance != null)
            {
                toPlayer = (PlayerController.Instance.transform.position - spawnPos).normalized;
            }

            if (enemyType == EnemyType.Minion)
            {
                // 雜兵：單發直下或自瞄子彈
                EnemyBulletPool.Instance.GetBullet(spawnPos, toPlayer, bulletSpeed);
            }
            else if (enemyType == EnemyType.Elite)
            {
                // 精英怪：3 向扇形散射彈幕
                FireSpread(spawnPos, toPlayer, 3, 20f, bulletSpeed);
            }
            else if (enemyType == EnemyType.Boss)
            {
                // Boss 多階段彈幕邏輯
                float hpRatio = (float)currentHp / Mathf.Max(1, maxHp);
                if (hpRatio > 0.66f)
                {
                    // 第一階段：3 向散射自瞄彈
                    FireSpread(spawnPos, toPlayer, 3, 25f, bulletSpeed);
                }
                else if (hpRatio > 0.33f)
                {
                    // 第二階段：8 向環形彈幕
                    FireRing(spawnPos, 8, bulletSpeed * 0.9f);
                }
                else
                {
                    // 第三階段狂暴：5 向高速散彈 + 環形彈幕
                    FireSpread(spawnPos, toPlayer, 5, 20f, bulletSpeed * 1.15f);
                    FireRing(spawnPos, 6, bulletSpeed * 0.8f);
                }
            }
        }

        private void FireSpread(Vector3 origin, Vector2 centerDir, int count, float spreadAngle, float speed)
        {
            float startAngle = -spreadAngle * (count - 1) / 2f;
            for (int i = 0; i < count; i++)
            {
                float angle = startAngle + i * spreadAngle;
                Vector2 dir = Quaternion.Euler(0, 0, angle) * centerDir;
                EnemyBulletPool.Instance.GetBullet(origin, dir, speed);
            }
        }

        private void FireRing(Vector3 origin, int count, float speed)
        {
            float step = 360f / count;
            for (int i = 0; i < count; i++)
            {
                float angle = i * step + (hoverTimer * 50f); // 略微旋轉增添動感
                Vector2 dir = Quaternion.Euler(0, 0, angle) * Vector2.down;
                EnemyBulletPool.Instance.GetBullet(origin, dir, speed);
            }
        }

        public void TakeDamage(int damage)
        {
            if (isDead) return;

            currentHp = Mathf.Max(0, currentHp - damage);

            // 每次受擊命中即時加分
            var gm = GameManager.Instance ?? Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.AddScore(hitScoreValue);
            }

            // 更新 Boss 血條 UI（多階段顏色變化）
            if (enemyType == EnemyType.Boss && UIManager.Instance != null)
            {
                float ratio = (float)currentHp / Mathf.Max(1, maxHp);
                UIManager.Instance.SetBossHealth(ratio);
            }

            // 受擊瞬間閃白反饋
            if (spriteRenderer != null && gameObject.activeInHierarchy)
            {
                if (Application.isPlaying)
                {
                    StartCoroutine(HitFlashRoutine());
                }
            }

            if (currentHp <= 0)
            {
                Die();
            }
        }

        private IEnumerator HitFlashRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = new Color(1f, 0.4f, 0.4f, 1f);
            }
            yield return new WaitForSeconds(0.08f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = originalColor;
            }
        }

        private void Die()
        {
            if (isDead) return;
            isDead = true;

            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayExplosion();
            }

            // 擊毀獎勵分數
            var gm = GameManager.Instance ?? Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.AddScore(killScoreValue);
                gm.AddKill();
            }

            // 播放爆炸特效
            if (explosionPrefab != null)
            {
                Instantiate(explosionPrefab, transform.position, Quaternion.identity);
            }

            // 掉落道具系統：P 武器強化, B 炸彈, H 能量護盾
            if (enemyType == EnemyType.Elite)
            {
                // 精英怪 100% 掉落道具 (P 40%, B 30%, H 30%)
                DropRandomItem(0.4f, 0.3f, 0.3f);
            }
            else if (enemyType == EnemyType.Minion)
            {
                // 雜兵 25% 機率掉落道具
                if (Random.value < minionDropChance)
                {
                    DropRandomItem(0.4f, 0.3f, 0.3f);
                }
            }
            else if (enemyType == EnemyType.Boss)
            {
                // Boss 擊破同時噴出全部 3 種獎勵
                if (powerUpPrefab != null) Instantiate(powerUpPrefab, transform.position + Vector3.left * 1.0f, Quaternion.identity);
                if (bombItemPrefab != null) Instantiate(bombItemPrefab, transform.position, Quaternion.identity);
                if (shieldItemPrefab != null) Instantiate(shieldItemPrefab, transform.position + Vector3.right * 1.0f, Quaternion.identity);
            }

            // Boss 擊破演繹與 Victory 狀態觸發
            if (enemyType == EnemyType.Boss)
            {
                if (UIManager.Instance != null)
                {
                    UIManager.Instance.ShowBossBar(false);
                }

                if (gm != null)
                {
                    gm.StartVictorySequence();
                }
            }

            SafelyDestroy();
        }

        private void DropRandomItem(float pChance, float bChance, float hChance)
        {
            float rand = Random.value;
            GameObject prefabToDrop = powerUpPrefab;

            if (rand < pChance)
            {
                prefabToDrop = powerUpPrefab;
            }
            else if (rand < pChance + bChance && bombItemPrefab != null)
            {
                prefabToDrop = bombItemPrefab;
            }
            else if (shieldItemPrefab != null)
            {
                prefabToDrop = shieldItemPrefab;
            }

            if (prefabToDrop != null)
            {
                Instantiate(prefabToDrop, transform.position, Quaternion.identity);
            }
        }

        private void SafelyDestroy()
        {
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
            }
        }
    }
}
