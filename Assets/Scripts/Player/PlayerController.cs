using System.Collections;
using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Pool;
using ShooterGame.UI;

namespace ShooterGame.Player
{
    /// <summary>
    /// 玩家機體控制器：8方向移動、視口邊界Clamp、核心微型Hitbox、連續射擊與被撞擊受傷無敵時間
    /// </summary>
    [RequireComponent(typeof(Rigidbody2D))]
    public class PlayerController : MonoBehaviour
    {
        public static PlayerController Instance { get; private set; }

        [Header("Movement Settings")]
        [SerializeField] private float moveSpeed = 8.5f;
        [SerializeField] private float clampMarginX = 0.6f;
        [SerializeField] private float clampMarginY = 0.8f;

        [Header("Combat & Health")]
        [SerializeField] private int maxHp = 3;
        [SerializeField] private int currentHp = 3;
        [SerializeField] private Transform firePoint;
        [SerializeField] private float fireRate = 0.15f;
        [SerializeField] private float invincibilityDuration = 1.5f; // 受擊後無敵時間（秒）

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

            // 確保 Rigidbody2D 設置正確
            Rigidbody2D rb = GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.bodyType = RigidbodyType2D.Kinematic;
                rb.useFullKinematicContacts = true;
            }
        }

        private void Start()
        {
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetHP(currentHp, maxHp);
            }
        }

        private void Update()
        {
            HandleMovement();
            HandleShooting();
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

        public void FireBullet()
        {
            Vector3 spawnPos = firePoint != null ? firePoint.position : transform.position + Vector3.up * 1.4f;
            if (BulletPool.Instance != null)
            {
                BulletPool.Instance.GetBullet(spawnPos, Quaternion.identity);
            }
        }

        /// <summary>
        /// 碰撞偵測：被敵機或隕石撞擊時扣除 1 點 HP，並進入無敵閃爍狀態
        /// </summary>
        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isInvulnerable) return;

            // 判斷是否為敵機或障礙物（Layer 為 Enemy 或包含 IDamageable）
            bool isEnemyLayer = collision.gameObject.layer == GameLayers.Enemy;
            IDamageable damageable = collision.GetComponent<IDamageable>() ?? collision.GetComponentInParent<IDamageable>();

            if (isEnemyLayer || damageable != null)
            {
                // 扣除玩家 1 點 HP
                TakeDamage(1);

                // 同時對碰撞的敵方目標造成傷害
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

            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetHP(currentHp, maxHp);
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
            float flashInterval = 0.1f;

            while (elapsed < invincibilityDuration)
            {
                if (spriteRenderer != null)
                {
                    spriteRenderer.color = new Color(1f, 1f, 1f, 0.25f);
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

        public void SetSimulatedInput(Vector2 dir)
        {
            simulatedInput = dir;
        }

        public void SetSimulatedFiring(bool firing)
        {
            isSimulatedFiring = firing;
        }

        public void ResetHP()
        {
            currentHp = maxHp;
            isInvulnerable = false;
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
            if (UIManager.Instance != null)
            {
                UIManager.Instance.SetHP(currentHp, maxHp);
            }
        }
    }
}
