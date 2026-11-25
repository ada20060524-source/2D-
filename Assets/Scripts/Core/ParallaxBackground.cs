using System.Collections.Generic;
using UnityEngine;

namespace ShooterGame.Core
{
    /// <summary>
    /// 三層視差無縫循環背景：遠景底層、中景裝飾與隨機近景碎片元素
    /// </summary>
    public class ParallaxBackground : MonoBehaviour
    {
        [System.Serializable]
        public class ParallaxLayerData
        {
            public string layerName;
            public Transform transform1;
            public Transform transform2;
            public float scrollSpeed = 1f;
            public float spriteHeight = 20.48f;
        }

        private class NearDebrisInstance
        {
            public GameObject gameObject;
            public Transform transform;
            public SpriteRenderer renderer;
            public float speed;
            public float rotationSpeed;
            public bool isActive;
        }

        [Header("Tiled Parallax Layers")]
        [SerializeField] private ParallaxLayerData[] layers;

        [Header("Near Debris (Random Foreground Elements)")]
        [SerializeField] private bool enableNearDebris = true;
        [SerializeField] private Sprite[] nearDebrisSprites;
        [SerializeField] private Material nearDebrisMaterial;
        [SerializeField] private int nearDebrisPoolSize = 8;
        [SerializeField] private int initialNearDebrisCount = 2;
        [SerializeField] private float nearDebrisMinSpeed = 3.5f;
        [SerializeField] private float nearDebrisMaxSpeed = 6.5f;
        [SerializeField] private float nearSpawnIntervalMin = 0.6f;
        [SerializeField] private float nearSpawnIntervalMax = 1.8f;
        [SerializeField] private Vector2 nearScaleRange = new Vector2(0.8f, 1.5f);
        [SerializeField] private Vector2 nearAlphaRange = new Vector2(0.45f, 0.75f);
        [SerializeField] private Vector2 nearRotationSpeedRange = new Vector2(-45f, 45f);
        [SerializeField] private int nearSortingOrder = -5;
        [SerializeField] private float nearViewportMargin = 1.5f;

        private readonly List<NearDebrisInstance> nearPool = new List<NearDebrisInstance>();
        private float nearSpawnTimer;

        private void Start()
        {
            InitializeNearDebrisPool();
        }

        private void Update()
        {
            UpdateTiledLayers();
            UpdateNearDebris();
        }

        private void UpdateTiledLayers()
        {
            if (layers == null) return;

            for (int i = 0; i < layers.Length; i++)
            {
                ParallaxLayerData layer = layers[i];
                if (layer.transform1 == null || layer.transform2 == null) continue;

                float dy = layer.scrollSpeed * Time.deltaTime;
                layer.transform1.position += Vector3.down * dy;
                layer.transform2.position += Vector3.down * dy;

                // 若圖層1移出視口下方，移至圖層2上方
                if (layer.transform1.position.y <= -layer.spriteHeight)
                {
                    layer.transform1.position = new Vector3(
                        layer.transform2.position.x,
                        layer.transform2.position.y + layer.spriteHeight,
                        layer.transform1.position.z
                    );
                }

                // 若圖層2移出視口下方，移至圖層1上方
                if (layer.transform2.position.y <= -layer.spriteHeight)
                {
                    layer.transform2.position = new Vector3(
                        layer.transform1.position.x,
                        layer.transform1.position.y + layer.spriteHeight,
                        layer.transform2.position.z
                    );
                }
            }
        }

        private void InitializeNearDebrisPool()
        {
            if (!enableNearDebris || nearDebrisSprites == null || nearDebrisSprites.Length == 0)
            {
                return;
            }

            if (nearDebrisMaterial == null)
            {
                SpriteRenderer childRenderer = GetComponentInChildren<SpriteRenderer>();
                if (childRenderer != null)
                {
                    nearDebrisMaterial = childRenderer.sharedMaterial;
                }
            }

            int poolCount = Mathf.Max(1, nearDebrisPoolSize);
            for (int i = 0; i < poolCount; i++)
            {
                GameObject debrisObj = new GameObject($"NearDebris_{i}");
                debrisObj.transform.SetParent(transform, false);
                debrisObj.SetActive(false);

                SpriteRenderer sr = debrisObj.AddComponent<SpriteRenderer>();
                sr.sortingOrder = nearSortingOrder;
                if (nearDebrisMaterial != null)
                {
                    sr.sharedMaterial = nearDebrisMaterial;
                }

                nearPool.Add(new NearDebrisInstance
                {
                    gameObject = debrisObj,
                    transform = debrisObj.transform,
                    renderer = sr,
                    isActive = false
                });
            }

            GetViewportBounds(out _, out _, out float minY, out float maxY);
            int prewarmCount = Mathf.Clamp(initialNearDebrisCount, 0, poolCount);
            for (int i = 0; i < prewarmCount; i++)
            {
                SpawnNearDebris(Random.Range(minY, maxY));
            }

            ResetSpawnTimer();
        }

        private void UpdateNearDebris()
        {
            if (!enableNearDebris || nearPool.Count == 0) return;

            GetViewportBounds(out _, out _, out float minY, out float maxY);
            float despawnY = minY - nearViewportMargin;

            for (int i = 0; i < nearPool.Count; i++)
            {
                NearDebrisInstance item = nearPool[i];
                if (!item.isActive) continue;

                item.transform.position += Vector3.down * (item.speed * Time.deltaTime);
                item.transform.Rotate(0f, 0f, item.rotationSpeed * Time.deltaTime);

                if (item.transform.position.y <= despawnY)
                {
                    item.isActive = false;
                    item.gameObject.SetActive(false);
                }
            }

            nearSpawnTimer -= Time.deltaTime;
            if (nearSpawnTimer <= 0f)
            {
                SpawnNearDebris(maxY + nearViewportMargin);
                ResetSpawnTimer();
            }
        }

        private void SpawnNearDebris(float spawnY)
        {
            NearDebrisInstance item = GetInactiveDebris();
            if (item == null) return;

            Sprite sprite = PickRandomDebrisSprite();
            if (sprite == null) return;

            GetViewportBounds(out float minX, out float maxX, out _, out _);
            float spawnX = Random.Range(minX, maxX);

            float minScale = Mathf.Min(nearScaleRange.x, nearScaleRange.y);
            float maxScale = Mathf.Max(nearScaleRange.x, nearScaleRange.y);
            float scale = Random.Range(minScale, maxScale);

            float minAlpha = Mathf.Clamp01(Mathf.Min(nearAlphaRange.x, nearAlphaRange.y));
            float maxAlpha = Mathf.Clamp01(Mathf.Max(nearAlphaRange.x, nearAlphaRange.y));
            float alpha = Random.Range(minAlpha, maxAlpha);

            float minSpeed = Mathf.Max(0.1f, Mathf.Min(nearDebrisMinSpeed, nearDebrisMaxSpeed));
            float maxSpeed = Mathf.Max(minSpeed, Mathf.Max(nearDebrisMinSpeed, nearDebrisMaxSpeed));

            float minRot = Mathf.Min(nearRotationSpeedRange.x, nearRotationSpeedRange.y);
            float maxRot = Mathf.Max(nearRotationSpeedRange.x, nearRotationSpeedRange.y);

            item.renderer.sprite = sprite;
            item.renderer.color = new Color(1f, 1f, 1f, alpha);
            item.renderer.flipX = Random.value > 0.5f;
            item.renderer.flipY = Random.value > 0.5f;
            item.renderer.sortingOrder = nearSortingOrder;

            item.transform.position = new Vector3(spawnX, spawnY, 0f);
            item.transform.rotation = Quaternion.Euler(0f, 0f, Random.Range(0f, 360f));
            item.transform.localScale = new Vector3(scale, scale, 1f);

            item.speed = Random.Range(minSpeed, maxSpeed);
            item.rotationSpeed = Random.Range(minRot, maxRot);
            item.isActive = true;
            item.gameObject.SetActive(true);
        }

        private NearDebrisInstance GetInactiveDebris()
        {
            for (int i = 0; i < nearPool.Count; i++)
            {
                if (!nearPool[i].isActive)
                {
                    return nearPool[i];
                }
            }
            return null;
        }

        private Sprite PickRandomDebrisSprite()
        {
            if (nearDebrisSprites == null || nearDebrisSprites.Length == 0) return null;

            int startIndex = Random.Range(0, nearDebrisSprites.Length);
            for (int offset = 0; offset < nearDebrisSprites.Length; offset++)
            {
                Sprite candidate = nearDebrisSprites[(startIndex + offset) % nearDebrisSprites.Length];
                if (candidate != null) return candidate;
            }
            return null;
        }

        private void ResetSpawnTimer()
        {
            float minInterval = Mathf.Max(0.05f, Mathf.Min(nearSpawnIntervalMin, nearSpawnIntervalMax));
            float maxInterval = Mathf.Max(minInterval, Mathf.Max(nearSpawnIntervalMin, nearSpawnIntervalMax));
            nearSpawnTimer = Random.Range(minInterval, maxInterval);
        }

        private void GetViewportBounds(out float minX, out float maxX, out float minY, out float maxY)
        {
            if (ViewportManager.Instance != null)
            {
                minX = ViewportManager.Instance.MinX;
                maxX = ViewportManager.Instance.MaxX;
                minY = ViewportManager.Instance.MinY;
                maxY = ViewportManager.Instance.MaxY;
                return;
            }

            minX = -4.5f;
            maxX = 4.5f;
            minY = -8f;
            maxY = 8f;
        }
    }
}
