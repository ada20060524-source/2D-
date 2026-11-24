using System.Collections;
using UnityEngine;
using ShooterGame.Core;
using ShooterGame.Player;

namespace ShooterGame.Enemy
{
    /// <summary>
    /// Boss 直線高能光束能量砲 (Beam Energy Cannon)
    /// 包含預警瞄準線（0.8s 紅色細光束）與高能貫穿雷射（1.2s 青藍色能量巨柱）
    /// </summary>
    public class BossLaserCannon : MonoBehaviour
    {
        [Header("Beam Render Settings")]
        [SerializeField] private LineRenderer lineRenderer;
        [SerializeField] private Material beamMaterial;
        [SerializeField] private Transform firePoint;

        [Header("Timing Settings")]
        [SerializeField] private float chargeDuration = 0.8f;
        [SerializeField] private float fireDuration = 1.25f;

        [Header("Beam Appearance")]
        [SerializeField] private float chargeWidth = 0.08f;
        [SerializeField] private float beamWidth = 0.72f;
        [SerializeField] private Color chargeColor = new Color(1f, 0.25f, 0.2f, 0.65f);
        [SerializeField] private Color beamColor = new Color(0.25f, 0.95f, 1f, 0.95f);

        [Header("Combat Settings")]
        [SerializeField] private float damageTickInterval = 0.4f;

        private bool isFiring = false;
        private Coroutine laserCoroutine;

        public bool IsFiring => isFiring;

        private void Awake()
        {
            SetupLineRenderer();
        }

        private void SetupLineRenderer()
        {
            if (lineRenderer == null)
            {
                lineRenderer = GetComponent<LineRenderer>();
            }

            if (lineRenderer == null)
            {
                var beamChild = new GameObject("LaserBeamRenderer");
                beamChild.transform.SetParent(transform, false);
                lineRenderer = beamChild.AddComponent<LineRenderer>();
            }

            lineRenderer.positionCount = 2;
            lineRenderer.useWorldSpace = true;
            lineRenderer.sortingLayerName = "Default";
            lineRenderer.sortingOrder = 5;

            if (beamMaterial != null)
            {
                lineRenderer.material = beamMaterial;
            }
            else
            {
                // 動態嘗試載入 Beam.mat 或使用 Sprites-Default
                var mat = Resources.Load<Material>("Beam");
                if (mat == null)
                {
                    Shader defaultShader = Shader.Find("Sprites/Default");
                    if (defaultShader != null) mat = new Material(defaultShader);
                }
                if (mat != null) lineRenderer.material = mat;
            }

            lineRenderer.enabled = false;
        }

        public void FireLaser()
        {
            if (isFiring || !gameObject.activeInHierarchy) return;
            laserCoroutine = StartCoroutine(LaserRoutine());
        }

        private IEnumerator LaserRoutine()
        {
            isFiring = true;
            if (lineRenderer == null) SetupLineRenderer();

            lineRenderer.enabled = true;

            float bottomY = ViewportManager.Instance != null ? ViewportManager.Instance.MinY - 2.5f : -12f;

            // ================= 階段 1：蓄力瞄準預警 (細紅線 0.8s) =================
            float elapsed = 0f;
            lineRenderer.startWidth = chargeWidth;
            lineRenderer.endWidth = chargeWidth;
            lineRenderer.startColor = chargeColor;
            lineRenderer.endColor = chargeColor;

            while (elapsed < chargeDuration)
            {
                elapsed += Time.deltaTime;
                Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.down * 0.8f;
                Vector3 target = new Vector3(origin.x, bottomY, 0f);

                lineRenderer.SetPosition(0, origin);
                lineRenderer.SetPosition(1, target);

                // 微弱閃爍加強緊迫感
                float alpha = Mathf.PingPong(elapsed * 12f, 0.4f) + 0.4f;
                Color blinkCol = new Color(chargeColor.r, chargeColor.g, chargeColor.b, alpha);
                lineRenderer.startColor = blinkCol;
                lineRenderer.endColor = blinkCol;

                yield return null;
            }

            // ================= 階段 2：高能光束爆發 (持續 1.25s) =================
            elapsed = 0f;
            float damageTimer = 0f;

            lineRenderer.startWidth = beamWidth;
            lineRenderer.endWidth = beamWidth;
            lineRenderer.startColor = beamColor;
            lineRenderer.endColor = beamColor;

            while (elapsed < fireDuration)
            {
                elapsed += Time.deltaTime;
                damageTimer -= Time.deltaTime;

                Vector3 origin = firePoint != null ? firePoint.position : transform.position + Vector3.down * 0.8f;
                Vector3 target = new Vector3(origin.x, bottomY, 0f);

                lineRenderer.SetPosition(0, origin);
                lineRenderer.SetPosition(1, target);

                // 光束脈動特效
                float currentWidth = beamWidth * (1f + Mathf.Sin(elapsed * 30f) * 0.08f);
                lineRenderer.startWidth = currentWidth;
                lineRenderer.endWidth = currentWidth;

                // 傷害檢測：直線覆蓋範圍判定
                if (damageTimer <= 0f)
                {
                    CheckPlayerHit(origin.x, origin.y, bottomY, beamWidth * 0.7f);
                    damageTimer = damageTickInterval;
                }

                yield return null;
            }

            // ================= 階段 3：光束衰減收斂 =================
            float fadeTime = 0.12f;
            elapsed = 0f;
            while (elapsed < fadeTime)
            {
                elapsed += Time.deltaTime;
                float w = Mathf.Lerp(beamWidth, 0f, elapsed / fadeTime);
                lineRenderer.startWidth = w;
                lineRenderer.endWidth = w;
                yield return null;
            }

            lineRenderer.enabled = false;
            isFiring = false;
            laserCoroutine = null;
        }

        private void CheckPlayerHit(float beamX, float topY, float bottomY, float halfWidth)
        {
            var player = PlayerController.Instance ?? Object.FindAnyObjectByType<PlayerController>();
            if (player == null || !player.gameObject.activeInHierarchy) return;

            Vector3 pPos = player.transform.position;

            // 判斷玩家是否在光束的 X 軸與 Y 軸射程覆蓋範圍內
            if (pPos.y <= topY && pPos.y >= bottomY)
            {
                if (Mathf.Abs(pPos.x - beamX) <= halfWidth)
                {
                    player.TakeDamage(1);
                }
            }
        }

        private void OnDisable()
        {
            if (lineRenderer != null)
            {
                lineRenderer.enabled = false;
            }
            isFiring = false;
            if (laserCoroutine != null)
            {
                StopCoroutine(laserCoroutine);
                laserCoroutine = null;
            }
        }
    }
}
