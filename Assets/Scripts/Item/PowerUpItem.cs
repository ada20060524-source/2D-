using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Player;

namespace ShooterGame.Item
{
    public enum DropItemType
    {
        PowerUp, // P: 火力強化
        Bomb,    // B: 保命炸彈 +1
        Shield   // H: 能量護盾 (持續 20s 或抵擋 3 次子彈)
    }

    /// <summary>
    /// 掉落物道具：支援 P 火力強化、B 保命炸彈與 H 能量護盾
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PowerUpItem : MonoBehaviour
    {
        [Header("Item Type")]
        [SerializeField] private DropItemType itemType = DropItemType.PowerUp;

        [Header("Movement Settings")]
        [SerializeField] private float fallSpeed = 1.8f;
        [SerializeField] private float wobbleFrequency = 3f;
        [SerializeField] private float wobbleAmplitude = 0.5f;
        [SerializeField] private int scoreValue = 200;

        public DropItemType ItemType => itemType;

        private float spawnTime = 0f;
        private float initialX = 0f;
        private bool isCollected = false;

        private void Start()
        {
            spawnTime = Time.time;
            initialX = transform.position.x;
            gameObject.layer = GameLayers.Item;
        }

        private void Update()
        {
            if (isCollected) return;

            float elapsed = Time.time - spawnTime;
            float newX = initialX + Mathf.Sin(elapsed * wobbleFrequency) * wobbleAmplitude;
            float newY = transform.position.y - fallSpeed * Time.deltaTime;

            transform.position = new Vector3(newX, newY, transform.position.z);

            // 超出視口底部自動銷毀
            if (ViewportManager.Instance != null && transform.position.y < ViewportManager.Instance.MinY - 1.2f)
            {
                Destroy(gameObject);
            }
        }

        private void OnTriggerEnter2D(Collider2D collision)
        {
            if (isCollected) return;

            if (collision.gameObject.layer == GameLayers.Player || collision.CompareTag("Player"))
            {
                isCollected = true;
                var player = PlayerController.Instance ?? collision.GetComponentInParent<PlayerController>();
                if (player != null)
                {
                    switch (itemType)
                    {
                        case DropItemType.PowerUp:
                            player.UpgradeWeapon();
                            scoreValue = 200;
                            break;
                        case DropItemType.Bomb:
                            player.CollectBomb();
                            scoreValue = 300;
                            break;
                        case DropItemType.Shield:
                            player.ActivateShield(20f, 3);
                            scoreValue = 300;
                            break;
                    }
                }

                if (AudioManager.Instance != null)
                {
                    AudioManager.Instance.PlayPowerUp();
                }

                var gm = GameManager.Instance ?? Object.FindAnyObjectByType<GameManager>();
                if (gm != null)
                {
                    gm.AddScore(scoreValue);
                }

                Destroy(gameObject);
            }
        }
    }
}
