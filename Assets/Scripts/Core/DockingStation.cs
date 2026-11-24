using UnityEngine;

namespace ShooterGame.Core
{
    /// <summary>
    /// 休息停靠站（Carrier 1 航空母艦）：Boss 擊破後飛入戰場，供玩家戰機自動返航停靠
    /// </summary>
    public class DockingStation : MonoBehaviour
    {
        [Header("Docking Runway Settings")]
        [SerializeField] private Vector3 dockingOffset = new Vector3(0.2f, 0.35f, 0f); // Carrier 1 右側甲板跑道
        [SerializeField] private Transform dockingBay;

        public Vector3 DockingWorldPosition => dockingBay != null ? dockingBay.position : transform.TransformPoint(dockingOffset);
        public Vector3 DockingOffset => dockingOffset;
    }
}
