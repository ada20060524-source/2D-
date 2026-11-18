using UnityEngine;
using ShooterGame.Core;

namespace ShooterGame.Enemy
{
    /// <summary>
    /// 敵人與障礙物生成器：於安全視口頂部隨機水平座標生成雜兵與隕石進場
    /// </summary>
    public class EnemySpawner : MonoBehaviour
    {
        [Header("Prefabs")]
        [SerializeField] private GameObject enemyPrefab;
        [SerializeField] private GameObject[] obstaclePrefabs;

        [Header("Spawner Settings")]
        [SerializeField] private float spawnInterval = 1.8f;
        [SerializeField] private float horizontalMargin = 0.8f;
        [SerializeField] private bool autoSpawn = true;

        [Header("Spawn Probabilities")]
        [Range(0f, 1f)]
        [SerializeField] private float obstacleChance = 0.35f; // 35% 機率生成隕石障礙物

        private float spawnTimer = 0f;

        public bool AutoSpawn
        {
            get => autoSpawn;
            set => autoSpawn = value;
        }

        private void Update()
        {
            if (!autoSpawn || ViewportManager.Instance == null) return;

            spawnTimer -= Time.deltaTime;
            if (spawnTimer <= 0f)
            {
                SpawnRandomEntity();
                spawnTimer = spawnInterval;
            }
        }

        public GameObject SpawnRandomEntity()
        {
            bool spawnObstacle = obstaclePrefabs != null && obstaclePrefabs.Length > 0 && Random.value < obstacleChance;

            if (spawnObstacle)
            {
                int idx = Random.Range(0, obstaclePrefabs.Length);
                return SpawnPrefab(obstaclePrefabs[idx], Quaternion.identity);
            }
            else if (enemyPrefab != null)
            {
                return SpawnPrefab(enemyPrefab, Quaternion.Euler(0, 0, 180));
            }

            return null;
        }

        public GameObject SpawnEnemy(Vector3? customPosition = null)
        {
            if (enemyPrefab == null) return null;
            return SpawnPrefab(enemyPrefab, Quaternion.Euler(0, 0, 180), customPosition);
        }

        private GameObject SpawnPrefab(GameObject prefab, Quaternion rotation, Vector3? customPosition = null)
        {
            if (prefab == null) return null;

            Vector3 spawnPos;
            if (customPosition.HasValue)
            {
                spawnPos = customPosition.Value;
            }
            else
            {
                float minX = ViewportManager.Instance.MinX + horizontalMargin;
                float maxX = ViewportManager.Instance.MaxX - horizontalMargin;
                float posX = Random.Range(minX, maxX);
                float posY = ViewportManager.Instance.MaxY + 1.2f;
                spawnPos = new Vector3(posX, posY, 0f);
            }

            return Instantiate(prefab, spawnPos, rotation);
        }
    }
}
