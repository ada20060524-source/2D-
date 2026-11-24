using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Player;

namespace ShooterGame.Item
{
    /// <summary>
    /// 武器升級 Power-Up 道具：緩慢飄落、正弦擺盪、玩家拾取後升級火力
    /// </summary>
    [RequireComponent(typeof(Collider2D))]
    public class PowerUpItem : MonoBehaviour
    {
        [Header("Movement Settings")]
        [SerializeField] private float fallSpeed = 1.8f;
        [SerializeField] private float wobbleFrequency = 3f;
        [SerializeField] private float wobbleAmplitude = 0.5f;
        [SerializeField] private int scoreValue = 200;

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
                    player.UpgradeWeapon();
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
