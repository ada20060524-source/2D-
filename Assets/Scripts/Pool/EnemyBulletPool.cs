using System.Collections.Generic;
using UnityEngine;
using ShooterGame.Bullet;

namespace ShooterGame.Pool
{
    /// <summary>
    /// 敵機子彈物件池：管理 EnemyBullet，避免大量子彈生成銷毀造成卡頓
    /// </summary>
    public class EnemyBulletPool : MonoBehaviour
    {
        public static EnemyBulletPool Instance { get; private set; }

        [Header("Pool Configuration")]
        [SerializeField] private GameObject bulletPrefab;
        [SerializeField] private int initialPoolSize = 60;

        private readonly Queue<EnemyBullet> poolQueue = new Queue<EnemyBullet>();
        private readonly List<EnemyBullet> activeBullets = new List<EnemyBullet>();

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

            // 清理既有子物件
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

        private EnemyBullet CreateNewBullet()
        {
            GameObject obj = Instantiate(bulletPrefab, transform);
            obj.SetActive(false);
            EnemyBullet bullet = obj.GetComponent<EnemyBullet>();
            if (bullet == null)
            {
                bullet = obj.AddComponent<EnemyBullet>();
            }
            poolQueue.Enqueue(bullet);
            return bullet;
        }

        public EnemyBullet GetBullet(Vector3 position, Vector2 direction, float speed)
        {
            EnemyBullet bullet = null;
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
            bullet.gameObject.SetActive(true);
            bullet.Initialize(direction, speed);
            activeBullets.Add(bullet);
            return bullet;
        }

        public void ReturnBullet(EnemyBullet bullet)
        {
            if (bullet == null) return;

            activeBullets.Remove(bullet);
            bullet.gameObject.SetActive(false);
            bullet.transform.SetParent(transform);

            if (!poolQueue.Contains(bullet))
            {
                poolQueue.Enqueue(bullet);
            }
        }

        /// <summary>
        /// 清除場上所有活躍敵彈（保命炸彈或重置遊戲時使用）
        /// </summary>
        public void ClearAllBullets()
        {
            for (int i = activeBullets.Count - 1; i >= 0; i--)
            {
                EnemyBullet b = activeBullets[i];
                if (b != null)
                {
                    b.gameObject.SetActive(false);
                    if (!poolQueue.Contains(b))
                    {
                        poolQueue.Enqueue(b);
                    }
                }
            }
            activeBullets.Clear();
        }
    }
}
