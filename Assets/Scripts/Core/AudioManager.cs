using UnityEngine;

namespace ShooterGame.Core
{
    /// <summary>
    /// 音訊管理器：集中管理背景音樂 (BGM) 與打擊音效 (SFX) 管道
    /// 支援程序化合成測試音效（無音訊資產時亦可無破綻發聲），並支援音量與靜音控制
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Audio Sources")]
        [SerializeField] private AudioSource bgmSource;
        [SerializeField] private AudioSource sfxSource;

        [Header("Audio Clips (設定指定音訊資產，未指定則自動載入或回退合成)")]
        [SerializeField] private AudioClip normalBgmClip;
        [SerializeField] private AudioClip bossBgmClip;
        [SerializeField] private AudioClip warningClip;
        [SerializeField] private AudioClip passClip;
        [SerializeField] private AudioClip failClip;

        [Header("Volume & Mute States")]
        [Range(0f, 1f)] [SerializeField] private float bgmVolume = 1.0f;
        [Range(0f, 1f)] [SerializeField] private float sfxVolume = 1.0f;
        [SerializeField] private bool bgmMute = false;
        [SerializeField] private bool sfxMute = false;

        [Header("Music & SFX Gain Multipliers (音量增益倍率)")]
        [SerializeField] private float bgmGain = 1.35f;
        [SerializeField] private float warningGain = 1.5f;
        [SerializeField] private float passGain = 1.4f;
        [SerializeField] private float failGain = 1.4f;

        private AudioClip synthLaserClip;
        private AudioClip synthExplosionClip;
        private AudioClip synthHitClip;
        private AudioClip synthPowerUpClip;
        private AudioClip synthPlayerHurtClip;
        private AudioClip synthPlayerDeathClip;
        private AudioClip synthWarningClip;
        private AudioClip synthBossLaserClip;
        private AudioClip synthNormalBgmClip;
        private AudioClip synthBossBgmClip;

        public float BgmVolume => bgmVolume;
        public float SfxVolume => sfxVolume;
        public bool BgmMute => bgmMute;
        public bool SfxMute => sfxMute;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            DontDestroyOnLoad(gameObject);

            InitializeAudioSources();
            LoadAudioClipsFromMusic();
            CreateProceduralClips();
            ApplySettingsFromPersistence();
        }

        private void LoadAudioClipsFromMusic()
        {
            // 優先從 Resources/Music 載入（兼顧 Editor 與 Standalone/WebGL 建置）
            if (normalBgmClip == null) normalBgmClip = Resources.Load<AudioClip>("Music/BGM");
            if (bossBgmClip == null) bossBgmClip = Resources.Load<AudioClip>("Music/BossBGM");
            if (warningClip == null) warningClip = Resources.Load<AudioClip>("Music/Warning2");
            if (passClip == null) passClip = Resources.Load<AudioClip>("Music/Pass");
            if (failClip == null) failClip = Resources.Load<AudioClip>("Music/Fail");

            #if UNITY_EDITOR
            if (normalBgmClip == null)
                normalBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Music/BGM.mp3");
            if (bossBgmClip == null)
                bossBgmClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Music/BossBGM.mp3");
            if (warningClip == null)
                warningClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Music/Warning2.mp3");
            if (passClip == null)
                passClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Music/Pass.mp3");
            if (failClip == null)
                failClip = UnityEditor.AssetDatabase.LoadAssetAtPath<AudioClip>("Assets/Music/Fail.mp3");
            #endif
        }

        private void InitializeAudioSources()
        {
            if (bgmSource == null)
            {
                bgmSource = gameObject.AddComponent<AudioSource>();
                bgmSource.loop = true;
                bgmSource.playOnAwake = false;
                bgmSource.spatialBlend = 0f; // 2D 純全域音效，確保最大立體聲響度
                bgmSource.priority = 0;       // 最高優先級，不被其他音效擠壓
            }

            if (sfxSource == null)
            {
                sfxSource = gameObject.AddComponent<AudioSource>();
                sfxSource.loop = false;
                sfxSource.playOnAwake = false;
                sfxSource.spatialBlend = 0f; // 2D 純全域音效
                sfxSource.priority = 10;
            }
        }

        private void ApplySettingsFromPersistence()
        {
            var settings = DataPersistenceManager.CurrentData.settings;
            if (settings != null)
            {
                bgmVolume = settings.bgmVolume;
                sfxVolume = settings.sfxVolume;
                bgmMute = settings.bgmMute;
                sfxMute = settings.sfxMute;
            }

            UpdateVolumes();
        }

        public void SetBgmVolume(float volume)
        {
            bgmVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
            SaveSettings();
        }

        public void SetSfxVolume(float volume)
        {
            sfxVolume = Mathf.Clamp01(volume);
            UpdateVolumes();
            SaveSettings();
        }

        public void SetBgmMute(bool mute)
        {
            bgmMute = mute;
            UpdateVolumes();
            SaveSettings();
        }

        public void SetSfxMute(bool mute)
        {
            sfxMute = mute;
            UpdateVolumes();
            SaveSettings();
        }

        public void ToggleBgmMute()
        {
            SetBgmMute(!bgmMute);
        }

        public void ToggleSfxMute()
        {
            SetSfxMute(!sfxMute);
        }

        private void UpdateVolumes()
        {
            if (bgmSource != null)
            {
                bgmSource.volume = bgmMute ? 0f : Mathf.Clamp01(bgmVolume * bgmGain);
            }
            if (sfxSource != null)
            {
                sfxSource.volume = sfxMute ? 0f : sfxVolume;
            }
        }

        private void SaveSettings()
        {
            DataPersistenceManager.UpdateSettings(bgmVolume, sfxVolume, bgmMute, sfxMute);
        }

        public void PlayBgm(AudioClip clip)
        {
            if (bgmSource == null || clip == null) return;
            if (bgmSource.clip == clip && bgmSource.isPlaying) return;
            bgmSource.clip = clip;
            bgmSource.Play();
        }

        public void PlayNormalBgm()
        {
            AudioClip clip = normalBgmClip != null ? normalBgmClip : synthNormalBgmClip;
            if (clip != null)
            {
                PlayBgm(clip);
            }
        }

        public void PlayBossBgm()
        {
            AudioClip clip = bossBgmClip != null ? bossBgmClip : synthBossBgmClip;
            if (clip != null)
            {
                PlayBgm(clip);
            }
        }

        public void StopBgm()
        {
            if (bgmSource != null) bgmSource.Stop();
        }

        public void PlaySfx(AudioClip clip, float volumeScale = 1f)
        {
            if (sfxSource == null || clip == null || sfxMute) return;
            sfxSource.PlayOneShot(clip, sfxVolume * volumeScale);
        }

        public void PlayLaser()
        {
            PlaySfx(synthLaserClip, 0.15f); // 降低玩家射擊音量，避免連續射擊時刺耳
        }

        public void PlayExplosion()
        {
            PlaySfx(synthExplosionClip, 0.28f); // 降低敵人爆炸音量，維持打擊感同時不掩蓋 BGM
        }

        public void PlayHit()
        {
            PlaySfx(synthHitClip, 0.5f);
        }

        public void PlayPowerUp()
        {
            PlaySfx(synthPowerUpClip, 0.35f); // 降低獲得道具音量，使拾取回饋清亮而不刺耳
        }

        public void PlayPlayerHurt()
        {
            PlaySfx(synthPlayerHurtClip, 0.35f); // 降低玩家受擊音量，避免撞擊過度刺耳
        }

        public void PlayPlayerDeath()
        {
            PlaySfx(synthPlayerDeathClip, 0.45f); // 降低玩家死亡音量，使遊戲結束更加舒適平衡
        }

        public void PlayWarningSiren()
        {
            AudioClip clip = warningClip != null ? warningClip : synthWarningClip;
            PlaySfx(clip, warningGain);
        }

        public void PlayPass()
        {
            if (passClip != null)
            {
                PlaySfx(passClip, passGain);
            }
        }

        public void PlayFail()
        {
            if (failClip != null)
            {
                PlaySfx(failClip, failGain);
            }
        }

        public void PlayBossLaser()
        {
            PlaySfx(synthBossLaserClip, 0.95f);
        }

        /// <summary>
        /// 程序化生成純淨 8-bit / Arcade 風格音效與雙軌無縫 BGM，無需外部音訊檔依賴即可正常響起
        /// </summary>
        private void CreateProceduralClips()
        {
            synthLaserClip = GenerateSynthClip("Laser", 0.12f, t =>
            {
                float freq = Mathf.Lerp(880f, 220f, t);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * (1f - t);
            });

            synthExplosionClip = GenerateSynthClip("Explosion", 0.35f, t =>
            {
                float noise = Random.Range(-1f, 1f);
                return noise * Mathf.Pow(1f - t, 2f);
            });

            synthHitClip = GenerateSynthClip("Hit", 0.08f, t =>
            {
                float freq = Mathf.Lerp(300f, 100f, t);
                return (Random.value > 0.5f ? 1f : -1f) * (1f - t) * 0.7f;
            });

            synthPowerUpClip = GenerateSynthClip("PowerUp", 0.25f, t =>
            {
                float freq = Mathf.Lerp(330f, 880f, t);
                return Mathf.Sin(2f * Mathf.PI * freq * t) * Mathf.Sin(Mathf.PI * t);
            });

            // 玩家受傷音效：重濁衝擊與震顫感
            synthPlayerHurtClip = GenerateSynthClip("PlayerHurt", 0.22f, t =>
            {
                float freq = Mathf.Lerp(260f, 90f, t);
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0f ? 0.6f : -0.6f;
                float noise = Random.Range(-0.3f, 0.3f);
                return (wave + noise) * (1f - t);
            });

            // 玩家死亡音效：重型下行爆炸碎裂聲
            synthPlayerDeathClip = GenerateSynthClip("PlayerDeath", 0.75f, t =>
            {
                float freq = Mathf.Lerp(220f, 40f, t);
                float tone = Mathf.Sin(2f * Mathf.PI * freq * t) * 0.4f;
                float noise = Random.Range(-0.8f, 0.8f);
                return (tone + noise) * Mathf.Pow(1f - t, 1.5f);
            });

            // 警告音效：急促高亢雙音雙頻防空空襲警報
            synthWarningClip = GenerateSynthClip("WarningSiren", 0.45f, t =>
            {
                float freq = (t < 0.22f) ? 880f : 660f;
                float wave = Mathf.Sin(2f * Mathf.PI * freq * t) > 0f ? 0.7f : -0.7f;
                return wave * (1f - t * 0.3f);
            });

            // Boss 高能雷射音效：低頻震顫與高能裂解共鳴
            synthBossLaserClip = GenerateSynthClip("BossLaser", 1.25f, t =>
            {
                float bass = Mathf.Sin(2f * Mathf.PI * 65f * t) * 0.5f;
                float buzz = Mathf.Sin(2f * Mathf.PI * 180f * t + Mathf.Sin(2f * Mathf.PI * 25f * t) * 3f) * 0.35f;
                float noise = Random.Range(-0.25f, 0.25f);
                return (bass + buzz + noise) * Mathf.Min(1f, (1.25f - t) * 2f);
            });

            // 常規戰鬥 BGM：明快輕盈的太空節奏 (130 BPM 循環 8 秒)
            synthNormalBgmClip = GenerateSynthClip("NormalBgm", 8.0f, t =>
            {
                // 130 BPM -> 每拍約 0.46 秒
                float beatTime = 60f / 130f;
                float beatPhase = (t % beatTime) / beatTime;
                // Bassline: 循環四個和弦低音 (110Hz, 130Hz, 146Hz, 165Hz)
                int chordIndex = (int)(t / (beatTime * 4)) % 4;
                float[] rootFreqs = { 110f, 130.8f, 146.8f, 164.8f };
                float root = rootFreqs[chordIndex];
                float bass = Mathf.Sin(2f * Mathf.PI * root * t) * Mathf.Pow(1f - beatPhase, 2f) * 0.35f;

                // 踩鈸節奏 Hi-hat
                float subPhase = (t % (beatTime * 0.5f)) / (beatTime * 0.5f);
                float hihat = Random.Range(-0.1f, 0.1f) * Mathf.Pow(1f - subPhase, 4f);

                // 旋律主音
                float melodyFreq = root * 2f;
                float lead = Mathf.Sin(2f * Mathf.PI * melodyFreq * t) * 0.15f;

                return Mathf.Clamp(bass + hihat + lead, -0.9f, 0.9f) * 0.6f;
            });

            // Boss 決戰 BGM：激昂熱血雙重重低音 (160 BPM 快速重拍循環 6 秒)
            synthBossBgmClip = GenerateSynthClip("BossBgm", 6.0f, t =>
            {
                // 160 BPM -> 每拍約 0.375 秒
                float beatTime = 60f / 160f;
                float beatPhase = (t % beatTime) / beatTime;
                // 緊湊熱血下壓雙低音 (82Hz -> 73Hz -> 65Hz -> 98Hz)
                int chordIndex = (int)(t / (beatTime * 4)) % 4;
                float[] bossRoots = { 82.4f, 73.4f, 65.4f, 98.0f };
                float root = bossRoots[chordIndex];

                // 重低音鼓重擊 Kick
                float kick = Mathf.Sin(2f * Mathf.PI * Mathf.Lerp(140f, 45f, beatPhase) * beatPhase) * Mathf.Pow(1f - beatPhase, 3f) * 0.55f;

                // 高速琶音 Arpeggio (16 分音符)
                float arpStep = (t % (beatTime * 0.25f)) / (beatTime * 0.25f);
                int stepIdx = (int)(t / (beatTime * 0.25f)) % 4;
                float[] intervals = { 1f, 1.2f, 1.5f, 1.8f };
                float leadFreq = root * 4f * intervals[stepIdx];
                float lead = (Mathf.Sin(2f * Mathf.PI * leadFreq * t) > 0f ? 0.2f : -0.2f) * (1f - arpStep);

                // 緊湊小鼓碎拍 Snare
                float snare = 0f;
                if ((int)(t / beatTime) % 2 == 1)
                {
                    snare = Random.Range(-0.25f, 0.25f) * Mathf.Pow(1f - beatPhase, 2f);
                }

                return Mathf.Clamp(kick + lead + snare, -0.95f, 0.95f) * 0.75f;
            });
        }

        private AudioClip GenerateSynthClip(string name, float duration, System.Func<float, float> generator)
        {
            int sampleRate = 44100;
            int totalSamples = Mathf.FloorToInt(sampleRate * duration);
            float[] samples = new float[totalSamples];

            for (int i = 0; i < totalSamples; i++)
            {
                float t = (float)i / totalSamples;
                samples[i] = generator(t);
            }

            AudioClip clip = AudioClip.Create(name, totalSamples, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }
    }
}
