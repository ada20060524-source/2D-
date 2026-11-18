using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Bullet;

namespace ShooterGame.Pool
{
    /// <summary>
    /// 玩家子彈物件池管理器：防止高頻發射產生的 GC Alloc 與記憶體碎片化
    /// </summary>
    public class BulletPool : MonoBehaviour
    {
        public static BulletPool Instance { get; private set; }

        [Header("Pool Configuration")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private int initialPoolSize = 30;

        private readonly Queue<PlayerBullet> poolQueue = new Queue<PlayerBullet>();
        private readonly List<PlayerBullet> activeBullets = new List<PlayerBullet>();

        public int ActiveCount => activeBullets.Count;
        public int PooledCount => poolQueue.Count;
        public int TotalCount => ActiveCount + PooledCount;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            InitializePool();
        }

        public void InitializePool()
        {
            if (bulletPrefab == null) return;

            // 清理場景中可能殘留的舊子物件
            for (int i = transform.childCount - 1; i >= 0; i--)
            {
                GameObject child = transform.GetChild(i).gameObject;
                if (Application.isPlaying)
                {
                    Destroy(child);
                }
                else
                {
                    DestroyImmediate(child);
                }
            }

            poolQueue.Clear();
            activeBullets.Clear();

            for (int i = 0; i < initialPoolSize; i++)
            {
                CreateNewBullet();
            }
        }

        private PlayerBullet CreateNewBullet()
        {
            GameObject obj = Instantiate(bulletPrefab, transform);
            obj.SetActive(false);
            PlayerBullet bullet = obj.GetComponent<PlayerBullet>();
            if (bullet == null)
            {
                bullet = obj.AddComponent<PlayerBullet>();
            }
            poolQueue.Enqueue(bullet);
            return bullet;
        }

        public PlayerBullet GetBullet(Vector3 position, Quaternion rotation)
        {
            PlayerBullet bullet = null;
            if (poolQueue.Count > 0)
            {
                bullet = poolQueue.Dequeue();
            }
            else
            {
                bullet = CreateNewBullet();
                poolQueue.Dequeue();
            }

            bullet.transform.position = position;
            bullet.transform.rotation = rotation;
            bullet.gameObject.SetActive(true);
            activeBullets.Add(bullet);
            return bullet;
        }

        public void ReturnBullet(PlayerBullet bullet)
        {
            if (bullet == null) return;

            bullet.gameObject.SetActive(false);
            activeBullets.Remove(bullet);

            if (!poolQueue.Contains(bullet))
            {
                poolQueue.Enqueue(bullet);
            }
        }

        public void ReturnAllBullets()
        {
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                ReturnBullet(activeBullets[i]);
            }
        }
    }
}
