using System;
using System.IO;
using UnityEngine;

namespace ShooterGame.Core
{
    [System.Serializable]
    public class GameSettingsData
    {
        public float bgmVolume = 1.0f;
        public float sfxVolume = 1.0f;
        public bool bgmMute = false;
        public bool sfxMute = false;
    }

    [System.Serializable]
    public class PlayerSaveData
    {
        public int highScore = 0;
        public int totalKills = 0;
        public int totalGamesPlayed = 0;
        public string lastPlayedTimestamp = "";
        public GameSettingsData settings = new GameSettingsData();
    }

    /// <summary>
    /// 本機資料持久化管理器：支援 WebGL (localStorage / IndexedDB 原生相容) 與本機磁碟 JSON 存取
    /// </summary>
    public static class DataPersistenceManager
    {
        private const string PrefsSaveKey = "ShooterGame_SaveData";
        private static readonly string LocalFilePath = Path.Combine(Application.persistentDataPath, "save_data.json");

        private static PlayerSaveData cachedSaveData;

        public static PlayerSaveData CurrentData
        {
            get
            {
                if (cachedSaveData == null)
                {
                    LoadData();
                }
                return cachedSaveData;
            }
        }

        public static void LoadData()
        {
            string json = "";

            // 1. 優先嘗試讀取 PlayerPrefs (在 WebGL 平台會自動對應至 browser localStorage / IndexedDB)
            if (PlayerPrefs.HasKey(PrefsSaveKey))
            {
                json = PlayerPrefs.GetString(PrefsSaveKey);
            }
            // 2. 若無則嘗試讀取本機 save_data.json
            else if (File.Exists(LocalFilePath))
            {
                try
                {
                    json = File.ReadAllText(LocalFilePath);
                }
                catch (Exception e)
                {
                    Debug.LogWarning($"[DataPersistenceManager] Failed to read local save file: {e.Message}");
                }
            }

            if (!string.IsNullOrEmpty(json))
            {
                try
                {
                    cachedSaveData = JsonUtility.FromJson<PlayerSaveData>(json);
                }
                catch (Exception e)
                {
                    Debug.LogError($"[DataPersistenceManager] Failed to parse save json: {e.Message}");
                    cachedSaveData = new PlayerSaveData();
                }
            }
            else
            {
                cachedSaveData = new PlayerSaveData();
            }

            if (cachedSaveData.settings == null)
            {
                cachedSaveData.settings = new GameSettingsData();
            }
        }

        public static void SaveData()
        {
            if (cachedSaveData == null)
            {
                cachedSaveData = new PlayerSaveData();
            }

            cachedSaveData.lastPlayedTimestamp = DateTime.UtcNow.ToString("o");
            string json = JsonUtility.ToJson(cachedSaveData, true);

            // 儲存至 PlayerPrefs (WebGL IndexedDB / localStorage)
            PlayerPrefs.SetString(PrefsSaveKey, json);
            PlayerPrefs.Save();

            // 同步寫入檔案
            try
            {
                string dir = Path.GetDirectoryName(LocalFilePath);
                if (!string.IsNullOrEmpty(dir) && !Directory.Exists(dir))
                {
                    Directory.CreateDirectory(dir);
                }
                File.WriteAllText(LocalFilePath, json);
            }
            catch (Exception e)
            {
                Debug.LogWarning($"[DataPersistenceManager] Could not write file to {LocalFilePath}: {e.Message}");
            }
        }

        public static bool ImportFromJson(string json)
        {
            if (string.IsNullOrEmpty(json)) return false;

            try
            {
                PlayerSaveData imported = JsonUtility.FromJson<PlayerSaveData>(json);
                if (imported != null)
                {
                    // 若匯入包含設定或分數則套用
                    if (imported.settings == null) imported.settings = new GameSettingsData();
                    cachedSaveData = imported;
                    SaveData();
                    return true;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"[DataPersistenceManager] Import error: {e.Message}");
            }
            return false;
        }

        public static string ExportToJson()
        {
            if (cachedSaveData == null)
            {
                LoadData();
            }
            return JsonUtility.ToJson(cachedSaveData, true);
        }

        public static void UpdateScoreAndKills(int score, int kills)
        {
            var data = CurrentData;
            data.totalGamesPlayed++;
            data.totalKills += kills;
            if (score > data.highScore)
            {
                data.highScore = score;
            }
            SaveData();
        }

        public static void UpdateSettings(float bgmVol, float sfxVol, bool bgmMute, bool sfxMute)
        {
            var data = CurrentData;
            data.settings.bgmVolume = bgmVol;
            data.settings.sfxVolume = sfxVol;
            data.settings.bgmMute = bgmMute;
            data.settings.sfxMute = sfxMute;
            SaveData();
        }
    }
}
