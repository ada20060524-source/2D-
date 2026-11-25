using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.TestTools;
using ShooterGame.Core;
using ShooterGame.Wave;
using ShooterGame.UI;

namespace ShooterGame.Tests
{
    public class Stage3VerificationTests
    {
        [SetUp]
        public void Setup()
        {
            PlayerPrefs.DeleteAll();
            DataPersistenceManager.LoadData();
        }

        [TearDown]
        public void TearDown()
        {
            PlayerPrefs.DeleteAll();
        }

        [Test]
        public void Test01_Persistence_ReadWriteAndHighScore()
        {
            // 初始分數為 0
            Assert.AreEqual(0, DataPersistenceManager.CurrentData.highScore);

            // 更新分數與擊墜數
            DataPersistenceManager.UpdateScoreAndKills(1500, 25);
            Assert.AreEqual(1500, DataPersistenceManager.CurrentData.highScore);
            Assert.AreEqual(25, DataPersistenceManager.CurrentData.totalKills);

            // 模擬重新讀取（如刷新瀏覽器頁面）
            DataPersistenceManager.LoadData();
            Assert.AreEqual(1500, DataPersistenceManager.CurrentData.highScore);
            Assert.AreEqual(25, DataPersistenceManager.CurrentData.totalKills);

            // 測試分數較低時不覆蓋最高分
            DataPersistenceManager.UpdateScoreAndKills(800, 10);
            Assert.AreEqual(1500, DataPersistenceManager.CurrentData.highScore);
            Assert.AreEqual(35, DataPersistenceManager.CurrentData.totalKills);
        }

        [Test]
        public void Test02_SaveData_ExportAndImportJson()
        {
            DataPersistenceManager.UpdateScoreAndKills(2500, 40);
            DataPersistenceManager.UpdateSettings(0.5f, 0.7f, true, false);

            string exportedJson = DataPersistenceManager.ExportToJson();
            Assert.IsFalse(string.IsNullOrEmpty(exportedJson));
            Assert.IsTrue(exportedJson.Contains("highScore"));
            Assert.IsTrue(exportedJson.Contains("settings"));
            Assert.IsTrue(exportedJson.Contains("2500"));

            // 清空本地存檔
            PlayerPrefs.DeleteAll();
            DataPersistenceManager.LoadData();
            Assert.AreEqual(0, DataPersistenceManager.CurrentData.highScore);

            // 模擬匯入存檔
            bool importResult = DataPersistenceManager.ImportFromJson(exportedJson);
            Assert.IsTrue(importResult);
            Assert.AreEqual(2500, DataPersistenceManager.CurrentData.highScore);
            Assert.AreEqual(40, DataPersistenceManager.CurrentData.totalKills);
            Assert.AreEqual(0.5f, DataPersistenceManager.CurrentData.settings.bgmVolume);
            Assert.IsTrue(DataPersistenceManager.CurrentData.settings.bgmMute);
        }

        [Test]
        public void Test03_WaveConfig_ImportAndExecution()
        {
            var testConfig = new WaveConfigData
            {
                waveName = "Automated Test Wave",
                bossSpawnTime = 45f,
                asteroidInterval = 4f
            };
            testConfig.events.Add(new WaveEventData { time = 5f, eventType = "Minion", posX = 1.0f, count = 2 });
            testConfig.events.Add(new WaveEventData { time = 10f, eventType = "Elite", posX = -1.0f, count = 1 });

            string json = WaveConfigUtility.ExportToJson(testConfig);
            Assert.IsFalse(string.IsNullOrEmpty(json));

            var imported = WaveConfigUtility.ImportFromJson(json);
            Assert.IsNotNull(imported);
            Assert.AreEqual("Automated Test Wave", imported.waveName);
            Assert.AreEqual(2, imported.events.Count);
            Assert.AreEqual(45f, imported.bossSpawnTime);
        }

        [Test]
        public void Test04_AudioManager_SettingsAndSynthClips()
        {
            var audioGo = new GameObject("TestAudioManager");
            var audioManager = audioGo.AddComponent<AudioManager>();

            audioManager.SetBgmVolume(0.6f);
            audioManager.SetSfxVolume(0.9f);
            audioManager.SetBgmMute(false);
            audioManager.SetSfxMute(true);

            Assert.AreEqual(0.6f, audioManager.BgmVolume);
            Assert.AreEqual(0.9f, audioManager.SfxVolume);
            Assert.IsFalse(audioManager.BgmMute);
            Assert.IsTrue(audioManager.SfxMute);

            // 測試程序化音效觸發不會擲出異常
            Assert.DoesNotThrow(() =>
            {
                audioManager.PlayLaser();
                audioManager.PlayExplosion();
                audioManager.PlayHit();
                audioManager.PlayPowerUp();
            });

            Object.DestroyImmediate(audioGo);
        }

        [Test]
        public void Test05_ObjectPoolAndMemoryResetLoop()
        {
            // 建立測試用 GameManager 與 Pool
            var gmGo = new GameObject("TestGameManager");
            var gm = gmGo.AddComponent<GameManager>();

            var bulletPoolGo = new GameObject("BulletPool");
            var bulletPool = bulletPoolGo.AddComponent<ShooterGame.Pool.BulletPool>();
            bulletPool.InitializePool();

            int initialBulletCount = bulletPoolGo.transform.childCount;

            // 執行 10 次連續 Restart 迴圈
            for (int i = 0; i < 10; i++)
            {
                gm.RestartGame();
                // 檢查 Object Pool 節點總數保持恆定，無洩漏或重複無效物件堆積
                int currentBulletCount = bulletPoolGo.transform.childCount;
                Assert.AreEqual(initialBulletCount, currentBulletCount, $"Loop {i}: Object pool count changed!");
            }

            Object.DestroyImmediate(gmGo);
            Object.DestroyImmediate(bulletPoolGo);
        }
    }
}
