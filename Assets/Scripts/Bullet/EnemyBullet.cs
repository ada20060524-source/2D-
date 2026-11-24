using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Player;
using ShooterGame.Pool;

namespace ShooterGame.Bullet
{
    /// <summary>
    /// 敵機子彈：支援方向速度移動、物件池回收、撞擊玩家扣血並回收
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class EnemyBullet : MonoBehaviour
    {
        [Header("Bullet Settings")]
        [SerializeField] private float defaultSpeed = 6f;
        [SerializeField] private int damage = 1;

        private Vector2 moveDirection = Vector2.down;
        private float currentSpeed = 6f;
        private bool isPooled = false;

        public bool IsPooled => isPooled;
        public int Damage => damage;

        public void Initialize(Vector2 direction, float speed)
        {
            moveDirection = direction.normalized;
            currentSpeed = speed > 0f ? speed : defaultSpeed;
            isPooled = false;

            // 計算旋轉角使子彈朝向運動方向
            float angle = Mathf.Atan2(moveDirection.y, moveDirection.x) * Mathf.Rad2Deg + 90f;
            transform.rotation = Quaternion.Euler(0f, 0f, angle);
        }

        private void OnEnable()
        {
            isPooled = false;
        }

        private void Update()
        {
            // 等速朝指定方向飛行
            transform.position += (Vector3)(moveDirection * (currentSpeed * Time.deltaTime));

            // 視口邊界檢測：超出畫面立即自動回收
            if (ViewportManager.Instance != null && ViewportManager.Instance.IsOutOfViewport(transform.position, 0.8f))
            {
                Recycle();
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isPooled) return;

            // 撞擊玩家機體
            if (collision.gameObject.layer == GameLayers.Player || collision.CompareTag("Player"))
            {
                var player = PlayerController.Instance ?? collision.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    if (player.IsShieldActive)
                    {
                        // 護盾存在：由護盾完全吸收並銷毀子彈，機身完全不扣血且無受傷反應
                        player.TakeDamage(damage);
                        Recycle();
                    }
                    else if (!player.IsInvulnerable)
                    {
                        // 無護盾且非無敵狀態：機體承受傷害
                        player.TakeDamage(damage);
                        Recycle();
                    }
                    else
                    {
                        // 機體無敵期間：子彈依然吸收回收，防止子彈穿透卡住
                        Recycle();
                    }
                }
            }
        }

        public void Recycle()
        {
            if (isPooled) return;
            isPooled = true;

            if (EnemyBulletPool.Instance != null)
            {
                EnemyBulletPool.Instance.ReturnBullet(this);
            }
            else
            {
                gameObject.SetActive(false);
            }
        }
    }
}
