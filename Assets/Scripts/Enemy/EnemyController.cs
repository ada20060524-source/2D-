using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Enemy
{
    /// <summary>
    /// 基礎敵人控制器：直線進場、血量扣除、命中立即加分、擊毀獎勵與受擊閃爍
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyController : MonoBehaviour, IDamageable
    {
        [Header("Enemy Stats")]
        [SerializeField] private int maxHp = 3;
        [SerializeField] private int currentHp = 3;
        [SerializeField] private float moveSpeed = 3f;
        [SerializeField] private int hitScoreValue = 20;   // 每次擊中立即加分
        [SerializeField] private int killScoreValue = 100; // 擊毀加分

        private SpriteRenderer spriteRenderer;

        public int CurrentHP => currentHp;
        public int MaxHP => maxHp;
        public int HitScoreValue => hitScoreValue;
        public int KillScoreValue => killScoreValue;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            currentHp = maxHp;
        }

        private void OnEnable()
        {
            currentHp = maxHp;
            if (spriteRenderer != null)
            {
                spriteRenderer.color = Color.white;
            }
        }

        private void Update()
        {
            // 向下平移進場
            transform.position += Vector3.down * (moveSpeed * Time.deltaTime);

            // 超出畫面下方邊界自動銷毀
            if (ViewportManager.Instance != null && transform.position.y < ViewportManager.Instance.MinY - 1.5f)
            {
                SafelyDestroy();
            }
        }

        /// <summary>
        /// 受擊扣除血量並即時加算得分
        /// </summary>
        public void TakeDamage(int damage)
        {
            currentHp = Mathf.Max(0, currentHp - damage);

            // 每次命中立即獲得打擊分數
            var gm = GameManager.Instance ?? Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.AddScore(hitScoreValue);
            }

            // 受擊閃爍視覺反饋
            if (spriteRenderer != null && gameObject.activeInHierarchy)
            {
                #if UNITY_EDITOR
                if (Application.isPlaying)
                {
                    StartCoroutine(HitFlashRoutine());
                }
                #else
                StartCoroutine(HitFlashRoutine());
                #endif
            }

            if (currentHp <= 0)
            {
                Die();
            }
        }

        private System.Collections.IEnumerator HitFlashRoutine()
        {
            if (spriteRenderer != null) spriteRenderer.color = Color.red;
            yield return new WaitForSeconds(0.08f);
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }

        private void Die()
        {
            var gm = GameManager.Instance ?? Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.AddScore(killScoreValue);
            }

            SafelyDestroy();
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
