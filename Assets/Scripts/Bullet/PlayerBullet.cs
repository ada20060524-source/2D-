using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Pool;

namespace ShooterGame.Bullet
{
    /// <summary>
    /// 玩家直向單發子彈：向上飛行、超出視口回收、撞擊 IDamageable 目標（敵機/隕石）扣血並回收
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PlayerBullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float speed = 16f;
        [SerializeField] private int damage = 1;

        private bool isPooled = false;

        public bool IsPooled => isPooled;
        public int Damage => damage;

        private void OnEnable()
        {
            isPooled = false;
        }

        private void Update()
        {
            // 等速向上飛行
            transform.position += Vector3.up * (speed * Time.deltaTime);

            // 邊界檢查：超出視口立即回收
            if (ViewportManager.Instance != null && ViewportManager.Instance.IsOutOfViewport(transform.position, 0.5f))
            {
                Recycle();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isPooled) return;

            // 偵測可受擊實體（Enemy 或 Asteroid 等）
            IDamageable damageable = collision.GetComponent<IDamageable>();
            if (damageable == null)
            {
                damageable = collision.GetComponentInParent<IDamageable>();
            }

            if (damageable != null)
            {
                damageable.TakeDamage(damage);
                Recycle();
            }
        }

        /// <summary>
        /// 回收至物件池
        /// </summary>
        public void Recycle()
        {
            if (isPooled) return;
            isPooled = true;

            if (BulletPool.Instance != null)
            {
                BulletPool.Instance.ReturnBullet(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
