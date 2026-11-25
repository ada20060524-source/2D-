using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Enemy
{
    /// <summary>
    /// 隕石障礙物：隨機自轉並緩慢向下漂移，可受擊阻擋子彈並提供分數
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class AsteroidObstacle : MonoBehaviour, IDamageable
    {
        [Header("Asteroid Stats")]
        [SerializeField] private int maxHp = 4;
        [SerializeField] private int currentHp = 4;
        [SerializeField] private float driftSpeed = 2f;
        [SerializeField] private float rotationSpeed = 30f;
        [SerializeField] private int hitScoreValue = 10;
        [SerializeField] private int killScoreValue = 50;

        private SpriteRenderer spriteRenderer;

        public int CurrentHP => currentHp;

        private void Awake()
        {
            spriteRenderer = GetComponent<SpriteRenderer>();
            currentHp = maxHp;
            // 隨機自轉方向與速率
            rotationSpeed = Random.Range(-45f, 45f);
            driftSpeed = Random.Range(1.5f, 2.8f);
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
            // 向下緩慢漂移
            transform.position += Vector3.down * (driftSpeed * Time.deltaTime);
            // 自轉
            transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);

            // 超出畫面下方邊界自動銷毀
            if (ViewportManager.Instance != null && transform.position.y < ViewportManager.Instance.MinY - 2.0f)
            {
                SafelyDestroy();
            }
        }

        public void TakeDamage(int damage)
        {
            currentHp = Mathf.Max(0, currentHp - damage);

            // 打擊加分
            var gm = GameManager.Instance ?? Object.FindAnyObjectByType<GameManager>();
            if (gm != null)
            {
                gm.AddScore(hitScoreValue);
            }

            // 受擊閃白
            if (spriteRenderer != null && gameObject.activeInHierarchy)
            {
                #if UNITY_EDITOR
                if (Application.isPlaying) StartCoroutine(FlashRoutine());
                #else
                StartCoroutine(FlashRoutine());
                #endif
            }

            if (currentHp <= 0)
            {
                Die();
            }
        }

        private System.Collections.IEnumerator FlashRoutine()
        {
            if (spriteRenderer != null) spriteRenderer.color = new Color(1f, 0.4f, 0.4f, 1f);
            yield return new WaitForSeconds(0.06f);
            if (spriteRenderer != null) spriteRenderer.color = Color.white;
        }

        private void Die()
        {
            if (AudioManager.Instance != null)
            {
                AudioManager.Instance.PlayExplosion();
            }

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
