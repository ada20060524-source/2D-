using System.Collections;
using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Pool;
using ShooterGame.UI;

namespace ShooterGame.Player
{
    /// <summary>
    /// 玩家機體控制器：8方向移動、視口邊界Clamp、核心微型Hitbox、四階段火力升級、保命炸彈與受擊無敵
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8.5f;
        [SerializeField] private float clampMarginX = 0.45f;
        [SerializeField] private float clampMarginY = 0.8f;

        [Header("Combat & Health")]
        [SerializeField] private int maxHp = 3;
        [SerializeField] private int currentHp = 3;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 0.14f;
        [SerializeField] private float invincibilityDuration = 1.5f;

        [Header("Weapon & Bomb System")]
        [SerializeField] private int powerLevel = 1; // 1 ~ 4 階火力
        [SerializeField] private int bombCount = 2;   // 保命炸彈數
        [SerializeField] private int maxBombCount = 3;

        [Header("Hitbox References")]
        [SerializeField] private Collider2D coreHitboxCollider;
        [SerializeField] private GameObject hitboxMarker;

        private SpriteRenderer spriteRenderer;
        private bool isInvulnerable = false;
        private float fireTimer = 0f;

        // 測試與模擬輸入支援
        private Vector2 simulatedInput = Vector2.zero;
        private bool isSimulatedFiring = false;

        public int CurrentHP => currentHp;
        public int MaxHP => maxHp;
        public int PowerLevel => powerLevel;
        public int BombCount => bombCount;
        public int MaxBombCount => maxBombCount;
        public bool IsInvulnerable => isInvulnerable;
        public Collider2D CoreHitbox => coreHitboxCollider;
        public GameObject HitboxMarker => hitboxMarker;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            spriteRenderer = GetComponent<SpriteRenderer>();
            currentHp = maxHp;

            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
            }
            gameObject.layer = GameLayers.Player;
        }

        private void Start()
        {
            UpdateAllUI();
        }

        private void Update()
        {
            var gm = GameManager.Instance;
            if (gm != null && (gm.CurrentState == GameState.Paused || gm.CurrentState == GameState.GameOver || gm.CurrentState == GameState.Victory))
            {
                return;
            }

            HandleMovement();
            HandleShooting();
            HandleBombInput();
        }

        private void HandleMovement()
        {
            float h = 0f;
            float v = 0f;

            if (simulatedInput != Vector2.zero)
            {
                h = simulatedInput.x;
                v = simulatedInput.y;
            }
            else
            {
                #if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    if (kb.aKey.isPressed || kb.leftArrowKey.isPressed) h -= 1f;
                    if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) h += 1f;
                    if (kb.sKey.isPressed || kb.downArrowKey.isPressed) v -= 1f;
                    if (kb.wKey.isPressed || kb.upArrowKey.isPressed) v += 1f;
                }
                #endif

                #if ENABLE_LEGACY_INPUT_MANAGER
                if (Mathf.Approximately(h, 0f) && Mathf.Approximately(v, 0f))
                {
                    h = Input.GetAxisRaw("Horizontal");
                    v = Input.GetAxisRaw("Vertical");
                }
                #endif
            }

            Vector2 inputDir = new Vector2(h, v);
            if (inputDir.sqrMagnitude > 1f)
            {
                inputDir.Normalize();
            }

            Vector3 targetPos = transform.position + (Vector3)(inputDir * (moveSpeed * Time.deltaTime));

            if (ViewportManager.Instance != null)
            {
                targetPos = ViewportManager.Instance.ClampToViewport(targetPos, clampMarginX, clampMarginY);
            }

            transform.position = targetPos;
        }

        private void HandleShooting()
        {
            fireTimer -= Time.deltaTime;

            bool isFiring = isSimulatedFiring;

            if (!isFiring)
            {
                #if ENABLE_INPUT_SYSTEM
                if (UnityEngine.InputSystem.Keyboard.current != null)
                {
                    var kb = UnityEngine.InputSystem.Keyboard.current;
                    if (kb.spaceKey.isPressed || kb.jKey.isPressed) isFiring = true;
                }
                if (UnityEngine.InputSystem.Mouse.current != null && UnityEngine.InputSystem.Mouse.current.leftButton.isPressed)
                {
                    isFiring = true;
                }
                #endif

                #if ENABLE_LEGACY_INPUT_MANAGER
                if (!isFiring)
                {
                    isFiring = Input.GetKey(KeyCode.Space) || Input.GetKey(KeyCode.J) || Input.GetMouseButton(0);
                }
                #endif
            }

            if (isFiring && fireTimer <= 0f)
            {
                FireBullet();
                fireTimer = fireRate;
            }
        }

        private void HandleBombInput()
        {
            bool bombPressed = false;

            #if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null && UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame)
            {
                bombPressed = true;
            }
            #endif

            #if ENABLE_LEGACY_INPUT_MANAGER
            if (!bombPressed && Input.GetKeyDown(KeyCode.B))
            {
                bombPressed = true;
            }
            #endif

            if (bombPressed)
            {
                TriggerBomb();
            }
        }

        public void FireBullet()
        {
            if (BulletPool.Instance == null) return;

            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.0f;

            switch (powerLevel)
            {
                case 1:
                    // 1 階：單發中央直向主砲
                    BulletPool.Instance.GetBullet(spawnPos, Quaternion.identity);
                    break;
                case 2:
                    // 2 階：雙發平行子彈
                    BulletPool.Instance.GetBullet(spawnPos + Vector3.left * 0.2f, Quaternion.identity);
                    BulletPool.Instance.GetBullet(spawnPos + Vector3.right * 0.2f, Quaternion.identity);
                    break;
                case 3:
                    // 3 階：3 向散射（中央直向 + 左右各 14 度）
                    BulletPool.Instance.GetBullet(spawnPos, Quaternion.identity);
                    BulletPool.Instance.GetBullet(spawnPos + Vector3.left * 0.22f, Quaternion.Euler(0, 0, 14f));
                    BulletPool.Instance.GetBullet(spawnPos + Vector3.right * 0.22f, Quaternion.Euler(0, 0, -14f));
                    break;
                case 4:
                default:
                    // 4 階：5 向寬幅強力散射（-24°, -12°, 0°, +12°, +24°）
                    BulletPool.Instance.GetBullet(spawnPos, Quaternion.identity);
                    BulletPool.Instance.GetBullet(spawnPos + Vector3.left * 0.18f, Quaternion.Euler(0, 0, 12f));
                    BulletPool.Instance.GetBullet(spawnPos + Vector3.right * 0.18f, Quaternion.Euler(0, 0, -12f));
                    BulletPool.Instance.GetBullet(spawnPos + Vector3.left * 0.35f, Quaternion.Euler(0, 0, 24f));
                    BulletPool.Instance.GetBullet(spawnPos + Vector3.right * 0.35f, Quaternion.Euler(0, 0, -24f));
                    break;
            }
        }

        /// <summary>
        /// 升級武器火力等級（1~4階）
        /// </summary>
        public void UpgradeWeapon()
        {
            powerLevel = Mathf.Min(4, powerLevel + 1);
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetPowerLevel(powerLevel);
            }
        }

        /// <summary>
        /// 施放保命炸彈：清除全螢幕敵彈並對全螢幕敵機造成巨額傷害
        /// </summary>
        public bool TriggerBomb()
        {
            if (bombCount <= 0) return false;

            bombCount--;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetBombCount(bombCount);
            }

            // 1. 清除全場敵彈
            if (EnemyBulletPool.Instance != null)
            {
                EnemyBulletPool.Instance.ClearAllBullets();
            }

            // 2. 對畫面上所有敵機與障礙物造成 20 點毀滅傷害
            var damageables = Object.FindObjectsByType<MonoBehaviour>();
            foreach (var mb in damageables)
            {
                if (mb is IDamageable d && mb.gameObject != gameObject)
                {
                    d.TakeDamage(20);
                }
            }

            // 3. 短暫觸發全螢幕白光閃爍反饋
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(BombFlashRoutine());
            }

            return true;
        }

        private IEnumerator BombFlashRoutine()
        {
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.cyan;
            }
            yield return new WaitForSeconds(0.12f);
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isInvulnerable) return;

            bool isEnemyLayer = collision.gameObject.layer == GameLayers.Enemy;
            bool isEnemyBulletLayer = collision.gameObject.layer == GameLayers.EnemyBullet;
            IDamageable damageable = collision.GetComponent<IDamageable>() ?? collision.GetComponentInParent<IDamageable>();

            if (isEnemyLayer || isEnemyBulletLayer || damageable != null)
            {
                TakeDamage(1);

                if (damageable != null)
                {
                    damageable.TakeDamage(1);
                }
            }
        }

        public void TakeDamage(int damage)
        {
            if (isInvulnerable) return;

            currentHp = Mathf.Max(0, currentHp - damage);

            // 受擊稍微降低火力（若高於1級）
            if (powerLevel > 1)
            {
                powerLevel--;
            }

            UpdateAllUI();

            if (currentHp <= 0)
            {
                Die();
                return;
            }

            // 進入無敵時間與高頻閃爍視覺回饋
            if (gameObject.activeInHierarchy)
            {
                StartCoroutine(InvincibilityRoutine());
            }
        }

        private IEnumerator InvincibilityRoutine()
        {
            isInvulnerable = true;
            float elapsed = 0f;
            float flashInterval = 0.08f;

            while (elapsed < invincibilityDuration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(1f, 1f, 1f, 0.2f);
                }
                yield return new WaitForSeconds(flashInterval);

                if (spriteRenderer != null)
                {
                    spriteRenderer.color = Color.white;
                }
                yield return new WaitForSeconds(flashInterval);

                elapsed += flashInterval * 2f;
            }

            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
            isInvulnerable = false;
        }

        private void Die()
        {
            var gm = GameManager.Instance ?? Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.TriggerGameOver();
            }
        }

        public void ResetPlayer()
        {
            currentHp = maxHp;
            powerLevel = 1;
            bombCount = 2;
            isInvulnerable = false;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            UpdateAllUI();
        }

        public void UpdateAllUI()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetHP(currentHp, maxHp);
                UIManager.Instance.SetPowerLevel(powerLevel);
                UIManager.Instance.SetBombCount(bombCount);
            }
        }

        public void SetSimulatedInput(Vector2 dir) => simulatedInput = dir;
        public void SetSimulatedFiring(bool firing) => isSimulatedFiring = firing;
    }
}
