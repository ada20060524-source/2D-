using UnityEngine;

namespace ShooterGame.Core
{
    /// <summary>
    /// 三層視差無縫循環背景：遠景底層、中景裝飾與近景元素
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

        [SerializeField] private ParallaxLayerData[] layers;

        private void Update()
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
    }
}
