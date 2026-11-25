using System;
using System.Collections.Generic;
using UnityEngine;

namespace ShooterGame.Wave
{
    [Serializable]
    public class WaveEventData
    {
        public float time;                // 觸發時間點（秒）
        public string eventType;          // "Minion", "Elite", "Formation", "Asteroid", "Boss"
        public float posX;                // 0 表示隨機，非 0 表示指定 x 座標
        public int count = 1;             // 生成數量
    }

    [Serializable]
    public class WaveConfigData
    {
        public string waveName = "Custom Wave";
        public float bossSpawnTime = 90f;
        public float asteroidInterval = 6f;
        public List<WaveEventData> events = new List<WaveEventData>();
    }

    public static class WaveConfigUtility
    {
        public static WaveConfigData CreateDefaultConfig()
        {
            var config = new WaveConfigData
            {
                waveName = "Default Campaign",
                bossSpawnTime = 90f,
                asteroidInterval = 6f
            };

            // 預設波次排程事件
            config.events.Add(new WaveEventData { time = 2f, eventType = "Minion", posX = 0f, count = 1 });
            config.events.Add(new WaveEventData { time = 5f, eventType = "Minion", posX = 0f, count = 2 });
            config.events.Add(new WaveEventData { time = 10f, eventType = "Minion", posX = 0f, count = 2 });
            config.events.Add(new WaveEventData { time = 15f, eventType = "Minion", posX = 0f, count = 3 });
            config.events.Add(new WaveEventData { time = 20f, eventType = "Formation", posX = 0f, count = 1 });
            config.events.Add(new WaveEventData { time = 32f, eventType = "Elite", posX = 0f, count = 1 });
            config.events.Add(new WaveEventData { time = 45f, eventType = "Elite", posX = 0f, count = 1 });
            config.events.Add(new WaveEventData { time = 60f, eventType = "Formation", posX = 0f, count = 1 });
            config.events.Add(new WaveEventData { time = 70f, eventType = "Formation", posX = 0f, count = 1 });
            config.events.Add(new WaveEventData { time = 75f, eventType = "Elite", posX = 0f, count = 1 });
            config.events.Add(new WaveEventData { time = 90f, eventType = "Boss", posX = 0f, count = 1 });

            return config;
        }

        public static string ExportToJson(WaveConfigData config)
        {
            if (config == null) config = CreateDefaultConfig();
            return JsonUtility.ToJson(config, true);
        }

        public static WaveConfigData ImportFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return null;
            try
            {
                return JsonUtility.FromJson<WaveConfigData>(json);
            }
            catch (Exception e)
            {
                Debug.LogError($"[WaveConfigUtility] Failed to parse wave json: {e.Message}");
                return null;
            }
        }
    }
}
