using UnityEngine;

namespace ShooterGame.Core
{
    /// <summary>
    /// 9:16 直向視口管理器：確保正交攝影機全螢幕 Solid Color 清理，並維護 9:16 核心安全邊界。
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DefaultExecutionOrder(-100)]
    public class ViewportManager : MonoBehaviour
    {
        public static ViewportManager Instance { get; private set; }

        [Header("Target Aspect Ratio (9:16)")]
        [SerializeField] private float targetAspectWidth = 9f;
        [SerializeField] private float targetAspectHeight = 16f;

        [Header("Camera Settings")]
        [SerializeField] private float defaultOrthographicSize = 8f;

        private Camera targetCamera;
        private float targetAspect;
        private Bounds worldBounds;

        public Camera Cam => targetCamera;
        public float TargetAspect => targetAspect;
        public Bounds WorldBounds => worldBounds;
        public float MinX => worldBounds.min.x;
        public float MaxX => worldBounds.max.x;
        public float MinY => worldBounds.min.y;
        public float MaxY => worldBounds.max.y;
        public float Width => worldBounds.size.x;
        public float Height => worldBounds.size.y;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;

            targetCamera = GetComponent<Camera>();
            targetCamera.orthographic = true;
            targetCamera.orthographicSize = defaultOrthographicSize;
            targetCamera.clearFlags = CameraClearFlags.SolidColor;
            targetCamera.backgroundColor = new Color(0.04f, 0.04f, 0.08f, 1f);
            targetCamera.rect = new Rect(0f, 0f, 1f, 1f); // 確保全螢幕完整清理每一幀緩衝區，杜絕殘影

            targetAspect = targetAspectWidth / targetAspectHeight;

            UpdateViewport();
        }

        private void Update()
        {
            UpdateViewport();
        }

        /// <summary>
        /// 計算 9:16 核心世界座標安全邊界，攝影機強制全螢幕 Solid Color 清除
        /// </summary>
        public void UpdateViewport()
        {
            if (targetCamera == null)
            {
                targetCamera = GetComponent<Camera>();
            }

            // 確保攝影機 Rect 始終為 (0, 0, 1, 1)，絕不縮成黑邊導致未清除像素疊影
            targetCamera.rect = new Rect(0f, 0f, 1f, 1f);
            targetCamera.clearFlags = CameraClearFlags.SolidColor;

            // 以 9:16 固定基準計算核心安全視口大小
            float vertExtent = targetCamera.orthographicSize;
            float horizExtent = vertExtent * targetAspect;
            Vector3 camPos = targetCamera.transform.position;

            worldBounds = new Bounds(
                new Vector3(camPos.x, camPos.y, 0),
                new Vector3(horizExtent * 2f, vertExtent * 2f, 10f)
            );
        }

        /// <summary>
        /// 限制座標於安全視口之內
        /// </summary>
        public Vector3 ClampToViewport(Vector3 position, float marginX = 0f, float marginY = 0f)
        {
            float clampedX = Mathf.Clamp(position.x, MinX + marginX, MaxX - marginX);
            float clampedY = Mathf.Clamp(position.y, MinY + marginY, MaxY - marginY);
            return new Vector3(clampedX, clampedY, position.z);
        }

        /// <summary>
        /// 判斷座標是否已超出視口邊界
        /// </summary>
        public bool IsOutOfViewport(Vector3 position, float margin = 0.5f)
        {
            return position.x < (MinX - margin) ||
                   position.x > (MaxX + margin) ||
                   position.y < (MinY - margin) ||
                   position.y > (MaxY + margin);
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(worldBounds.center, worldBounds.size);
        }
    }
}
