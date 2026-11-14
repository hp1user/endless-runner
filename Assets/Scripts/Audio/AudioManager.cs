using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner.Audio
{
    [DisallowMultipleComponent]
    public class AudioManager : MonoBehaviour
    {
        public static AudioManager Instance { get; private set; }

        [Header("Configuration Database")]
        [SerializeField] private AudioDatabase database;

        public AudioDatabase Database => database;

        [Header("Runtime Music Sources")]
        [SerializeField] private AudioSource layer0Source;
        [SerializeField] private AudioSource layer1Source;

        // Dedicated Weapon Fire Voice Ring (prevents pool starvation during auto/rapid fire)
        private const int WEAPON_VOICE_COUNT = 6;
        private AudioSource[] weaponFireSources;
        private int weaponVoiceIndex = 0;

        // Capped General SFX Pools with Voice Stealing
        private const int INITIAL_2D_SFX_POOL_SIZE = 8;
        private const int MAX_2D_SFX_POOL_SIZE = 16;

        private const int INITIAL_3D_SFX_POOL_SIZE = 8;
        private const int MAX_3D_SFX_POOL_SIZE = 20;

        private List<AudioSource> sfxPool2D = new List<AudioSource>();
        private List<AudioSource> sfxPool3D = new List<AudioSource>();

        private Transform sfxPoolContainer;
        private Transform weaponAudioContainer;

        private float battleGraceTimer = 0f;
        private bool isExplicitBattle = false;
        private float musicSyncTimer = 0f;

        // Rate-limiting to prevent shotgun/rapid-fire micro stutter or frame audio clipping
        private float lastImpactSoundTime = -1f;
        private const float MIN_IMPACT_INTERVAL = 0.035f;

        private float lastHurtSoundTime = -1f;
        private const float MIN_HURT_INTERVAL = 0.05f;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
                DontDestroyOnLoad(gameObject);
                Initialize();
            }
            else if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
            }
        }

        private void Initialize()
        {
            if (database == null)
            {
                database = Resources.Load<AudioDatabase>("Audio/AudioDatabase");
            }

            // Create or configure AudioSources for Music Layers
            if (layer0Source == null)
            {
                GameObject l0 = new GameObject("Music_Layer0_Base");
                l0.transform.SetParent(this.transform);
                layer0Source = l0.AddComponent<AudioSource>();
            }
            ConfigureMusicSource(layer0Source);

            if (layer1Source == null)
            {
                GameObject l1 = new GameObject("Music_Layer1_Battle");
                l1.transform.SetParent(this.transform);
                layer1Source = l1.AddComponent<AudioSource>();
            }
            ConfigureMusicSource(layer1Source);

            // Initialize Dedicated Weapon Audio Voice Ring
            if (weaponFireSources == null || weaponFireSources.Length == 0)
            {
                GameObject weaponGO = new GameObject("Weapon_Audio_Voices");
                weaponGO.transform.SetParent(this.transform);
                weaponAudioContainer = weaponGO.transform;

                weaponFireSources = new AudioSource[WEAPON_VOICE_COUNT];
                for (int i = 0; i < WEAPON_VOICE_COUNT; i++)
                {
                    GameObject voiceGO = new GameObject($"WeaponVoice_{i}");
                    voiceGO.transform.SetParent(weaponAudioContainer);
                    AudioSource src = voiceGO.AddComponent<AudioSource>();
                    src.playOnAwake = false;
                    src.loop = false;
                    src.spatialBlend = 0f; // Crisp 2D gun audio
                    src.priority = 32;     // High priority
                    weaponFireSources[i] = src;
                }
            }

            // Create SFX Pool container
            if (sfxPoolContainer == null)
            {
                GameObject poolGO = new GameObject("SFX_Pool");
                poolGO.transform.SetParent(this.transform);
                sfxPoolContainer = poolGO.transform;

                for (int i = 0; i < INITIAL_2D_SFX_POOL_SIZE; i++)
                {
                    sfxPool2D.Add(CreateSFXSource(false));
                }

                for (int i = 0; i < INITIAL_3D_SFX_POOL_SIZE; i++)
                {
                    sfxPool3D.Add(CreateSFXSource(true));
                }
            }

            StartPerimeterBreachMusic();
        }

        private void ConfigureMusicSource(AudioSource source)
        {
            if (source == null) return;
            source.playOnAwake = false;
            source.loop = true;
            source.spatialBlend = 0f; // 2D Stereo
            source.bypassEffects = true;
            source.bypassListenerEffects = true;
            source.bypassReverbZones = true;
            source.priority = 0; // Highest priority in entire engine
        }

        private AudioSource CreateSFXSource(bool is3D)
        {
            GameObject go = new GameObject(is3D ? "SFX_Source_3D" : "SFX_Source_2D");
            go.transform.SetParent(sfxPoolContainer);
            AudioSource src = go.AddComponent<AudioSource>();
            src.playOnAwake = false;
            src.loop = false;
            src.spatialBlend = is3D ? 1f : 0f;
            src.priority = 64; // Standard SFX priority
            if (is3D)
            {
                src.rolloffMode = AudioRolloffMode.Logarithmic;
                src.minDistance = 2f;
                src.maxDistance = 45f;
                src.dopplerLevel = 0f;
            }
            return src;
        }

        public void StartPerimeterBreachMusic()
        {
            if (database == null || layer0Source == null || layer1Source == null) return;

            if (database.perimeterBreachLayer0 != null && database.perimeterBreachLayer1 != null)
            {
                layer0Source.clip = database.perimeterBreachLayer0;
                layer1Source.clip = database.perimeterBreachLayer1;

                layer0Source.loop = true;
                layer1Source.loop = true;

                layer0Source.timeSamples = 0;
                layer1Source.timeSamples = 0;

                layer0Source.volume = database.layer0Volume * database.musicVolume * database.masterVolume;
                layer1Source.volume = 0f; // Start with battle layer silent

                layer0Source.Play();
                layer1Source.Play();

                Debug.Log("<color=green>[AudioManager]</color> Started Perimeter Breach synchronized dual-layer soundtrack (Looping).");
            }
        }

        public void SetBattleMode(bool active)
        {
            isExplicitBattle = active;
        }

        private void Update()
        {
            if (database == null || layer0Source == null || layer1Source == null) return;

            // Ensure music loops reliably and never stays stopped after boss fight
            if (!layer0Source.loop) layer0Source.loop = true;
            if (!layer1Source.loop) layer1Source.loop = true;

            if (!layer0Source.isPlaying && database.perimeterBreachLayer0 != null)
            {
                layer0Source.Play();
            }

            if (!layer1Source.isPlaying && database.perimeterBreachLayer1 != null)
            {
                if (layer0Source.isPlaying)
                {
                    layer1Source.timeSamples = layer0Source.timeSamples;
                }
                layer1Source.Play();
            }

            // Throttled phase-drift correction (checked once every 1.5s, avoided near loop points)
            musicSyncTimer += Time.unscaledDeltaTime;
            if (musicSyncTimer >= 1.5f)
            {
                musicSyncTimer = 0f;
                if (layer0Source.isPlaying && layer1Source.isPlaying && layer0Source.clip != null)
                {
                    int totalSamples = layer0Source.clip.samples;
                    int s0 = layer0Source.timeSamples;
                    int s1 = layer1Source.timeSamples;
                    int margin = (int)(layer0Source.clip.frequency * 2f); // 2s loop boundary safety margin

                    if (s0 > margin && s0 < totalSamples - margin)
                    {
                        int diff = Mathf.Abs(s0 - s1);
                        if (diff > 4400) // ~100ms drift threshold
                        {
                            layer1Source.timeSamples = s0;
                        }
                    }
                }
            }

            // Detect Game States
            bool isCardPopupOpen = UpgradeManager.Instance != null && UpgradeManager.Instance.IsSelectionOpen;
            bool isWheelOpen = WeaponWheelToolkitManager.Instance != null && WeaponWheelToolkitManager.Instance.IsOpen;
            bool isBossActive = GameManager.Instance != null && GameManager.Instance.isBossFightActive;
            bool isEnemiesActive = EnemyManager.Instance != null && EnemyManager.Instance.ActiveEnemyCount > 0;
            bool inCombat = isBossActive || isEnemiesActive || isExplicitBattle;

            if (inCombat)
            {
                battleGraceTimer = database.battleGracePeriod;
            }
            else if (battleGraceTimer > 0f)
            {
                battleGraceTimer -= Time.unscaledDeltaTime;
            }

            bool battleMusicShouldPlay = inCombat || (battleGraceTimer > 0f);

            float master = database.masterVolume;
            float music = database.musicVolume;

            float targetLayer0Vol;
            float targetLayer1Vol;
            float targetPitch = 1.0f;

            // Priority 1: Card popup is active -> Mute Layer 1, Duck Layer 0, gradually slow Layer 0 pitch
            if (isCardPopupOpen)
            {
                targetLayer1Vol = 0f;
                targetLayer0Vol = database.layer0Volume * database.cardPopupDuckVolume * music * master;
                targetPitch = database.cardPopupSlowPitch;
            }
            // Priority 2: Weapon Wheel open -> Mute Layer 1, Slow Layer 0 pitch (tape-stop effect)
            else if (isWheelOpen)
            {
                targetLayer1Vol = 0f;
                targetLayer0Vol = database.layer0Volume * music * master;
                targetPitch = database.weaponWheelSlowPitch;
            }
            // Priority 3: Normal gameplay -> Layer 0 full, Layer 1 active only in combat
            else
            {
                targetLayer0Vol = database.layer0Volume * music * master;
                targetLayer1Vol = battleMusicShouldPlay ? (database.layer1Volume * music * master) : 0f;
                targetPitch = 1.0f;
            }

            // Butter-smooth, frame-rate independent exponential transition using unscaledDeltaTime (functions even when timeScale == 0)
            float dt = Mathf.Min(Time.unscaledDeltaTime, 0.1f);
            float fadeT = 1f - Mathf.Exp(-database.musicFadeSpeed * dt);
            float pitchT = 1f - Mathf.Exp(-database.pitchSmoothSpeed * dt);

            layer0Source.volume = Mathf.Lerp(layer0Source.volume, targetLayer0Vol, fadeT);
            layer1Source.volume = Mathf.Lerp(layer1Source.volume, targetLayer1Vol, fadeT);

            layer0Source.pitch = Mathf.Lerp(layer0Source.pitch, targetPitch, pitchT);
            layer1Source.pitch = layer0Source.pitch;
        }

        // =========================================================================
        // SFX PLAYBACK API
        // =========================================================================

        public void PlaySFX(AudioClip clip, float volumeScale = 1.0f, float pitch = 1.0f)
        {
            if (clip == null || database == null) return;
            AudioSource src = GetAvailable2DSource();
            if (src == null) return;

            src.pitch = pitch;
            float finalVolume = volumeScale * database.sfxVolume * database.masterVolume;
            src.PlayOneShot(clip, finalVolume);
        }

        public void PlaySFXRandomPitch(AudioClip clip, float volumeScale = 1.0f, float minPitch = 0.94f, float maxPitch = 1.06f)
        {
            if (clip == null) return;
            PlaySFX(clip, volumeScale, Random.Range(minPitch, maxPitch));
        }

        public void PlaySFXAtPosition(AudioClip clip, Vector3 position, float volumeScale = 1.0f, float minDistance = 2f, float maxDistance = 45f)
        {
            if (clip == null || database == null) return;
            AudioSource src = GetAvailable3DSource();
            if (src == null) return;

            src.transform.position = position;
            src.minDistance = minDistance;
            src.maxDistance = maxDistance;
            src.pitch = Random.Range(0.95f, 1.05f);

            float finalVolume = volumeScale * database.sfxVolume * database.masterVolume;
            src.PlayOneShot(clip, finalVolume);
        }

        // --- UI Sounds ---
        public void PlayUISound(UISoundType type)
        {
            if (database == null) return;
            AudioClip clip = database.GetUISound(type);
            if (clip != null)
            {
                PlaySFX(clip, 1.0f, 1.0f);
            }
        }

        // --- Weapon Sounds ---
        public void PlayWeaponSound(string weaponID, WeaponSoundType type, AudioClip fallbackClip = null)
        {
            if (database == null) return;
            WeaponAudioProfile profile = database.GetWeaponProfile(weaponID);

            if (type == WeaponSoundType.Fire)
            {
                AudioClip fireClip = profile != null ? profile.GetRandomFireClip() : null;
                if (fireClip == null) fireClip = fallbackClip;

                if (fireClip != null)
                {
                    float vol = profile != null ? profile.volume : 1f;
                    PlayWeaponFireDirect(fireClip, vol);
                }
                return;
            }

            AudioClip clipToPlay = type switch
            {
                WeaponSoundType.Reload => profile?.reloadClip ?? fallbackClip,
                WeaponSoundType.MagOut => profile?.magOutClip ?? fallbackClip,
                WeaponSoundType.MagIn => profile?.magInClip ?? fallbackClip,
                WeaponSoundType.EmptyClick => profile?.emptyClickClip ?? fallbackClip,
                WeaponSoundType.Equip => profile?.equipClip ?? fallbackClip,
                _ => null
            };

            if (clipToPlay != null)
            {
                float vol = profile != null ? profile.volume : 1f;
                PlaySFXRandomPitch(clipToPlay, vol, 0.95f, 1.05f);
            }
        }

        /// <summary>
        /// Plays rapid weapon fire sounds using a dedicated round-robin voice ring.
        /// Guaranteed never to drop shots or voice-starve under rapid fire / skills.
        /// </summary>
        private void PlayWeaponFireDirect(AudioClip clip, float volumeScale)
        {
            if (clip == null || database == null) return;
            if (weaponFireSources == null || weaponFireSources.Length == 0) return;

            AudioSource src = weaponFireSources[weaponVoiceIndex];
            weaponVoiceIndex = (weaponVoiceIndex + 1) % weaponFireSources.Length;

            if (src != null)
            {
                src.pitch = Random.Range(0.95f, 1.05f);
                float finalVolume = volumeScale * database.sfxVolume * database.masterVolume;
                src.clip = clip;
                src.volume = finalVolume;
                src.Play();
            }
        }

        // --- Boss Sounds ---
        public void PlayBossSound(BossSoundType type, Vector3 position = default)
        {
            if (database == null) return;

            // Rate-limit hurt groans to avoid frame-clumping on shotgun/rapid-fire impacts
            if (type == BossSoundType.Hurt)
            {
                if (Time.unscaledTime - lastHurtSoundTime < MIN_HURT_INTERVAL) return;
                lastHurtSoundTime = Time.unscaledTime;
            }

            AudioClip clip = database.GetBossSound(type);
            if (clip == null) return;

            if (position != default)
            {
                PlaySFXAtPosition(clip, position, 1.0f, 4f, 60f);
            }
            else
            {
                PlaySFX(clip, 1.0f, 1.0f);
            }
        }

        // --- Bug & Minion Sounds ---
        public void PlayBugSound(BugSoundType type, Vector3 position = default)
        {
            if (database == null) return;

            if (type == BugSoundType.Hurt)
            {
                if (Time.unscaledTime - lastHurtSoundTime < MIN_HURT_INTERVAL) return;
                lastHurtSoundTime = Time.unscaledTime;
            }

            AudioClip clip = database.GetRandomBugSound(type);
            if (clip == null) return;

            if (position != default)
            {
                PlaySFXAtPosition(clip, position, 1.0f, 2f, 40f);
            }
            else
            {
                PlaySFX(clip, 1.0f, 1.0f);
            }
        }

        // --- Impact Sounds ---
        public void PlayImpactSound(ImpactSoundType type, Vector3 position)
        {
            if (database == null) return;

            // Rate-limit impacts (e.g. 8 shotgun pellets hitting in same frame -> 1-2 distinct impacts)
            if (Time.unscaledTime - lastImpactSoundTime < MIN_IMPACT_INTERVAL) return;
            lastImpactSoundTime = Time.unscaledTime;

            AudioClip clip = database.GetRandomImpactSound(type);
            if (clip == null) return;

            PlaySFXAtPosition(clip, position, 0.9f, 2f, 35f);
        }

        // =========================================================================
        // VOLUME SETTERS
        // =========================================================================

        public void SetMasterVolume(float vol)
        {
            if (database != null) database.masterVolume = Mathf.Clamp01(vol);
        }

        public void SetMusicVolume(float vol)
        {
            if (database != null) database.musicVolume = Mathf.Clamp01(vol);
        }

        public void SetLayer0Volume(float vol)
        {
            if (database != null) database.layer0Volume = Mathf.Clamp01(vol);
        }

        public void SetLayer1Volume(float vol)
        {
            if (database != null) database.layer1Volume = Mathf.Clamp01(vol);
        }

        public void SetSFXVolume(float vol)
        {
            if (database != null) database.sfxVolume = Mathf.Clamp01(vol);
        }

        // =========================================================================
        // INTERNAL POOL HELPERS (Bounded with Voice Stealing)
        // =========================================================================

        private AudioSource GetAvailable2DSource()
        {
            for (int i = 0; i < sfxPool2D.Count; i++)
            {
                if (sfxPool2D[i] != null && !sfxPool2D[i].isPlaying) return sfxPool2D[i];
            }

            if (sfxPool2D.Count < MAX_2D_SFX_POOL_SIZE)
            {
                AudioSource newSrc = CreateSFXSource(false);
                sfxPool2D.Add(newSrc);
                return newSrc;
            }

            // Voice Stealing: Steal the source that has been playing longest
            AudioSource oldest = sfxPool2D[0];
            float maxTime = -1f;
            for (int i = 0; i < sfxPool2D.Count; i++)
            {
                if (sfxPool2D[i] != null && sfxPool2D[i].time > maxTime)
                {
                    maxTime = sfxPool2D[i].time;
                    oldest = sfxPool2D[i];
                }
            }
            if (oldest != null)
            {
                oldest.Stop();
                return oldest;
            }
            return sfxPool2D[0];
        }

        private AudioSource GetAvailable3DSource()
        {
            for (int i = 0; i < sfxPool3D.Count; i++)
            {
                if (sfxPool3D[i] != null && !sfxPool3D[i].isPlaying) return sfxPool3D[i];
            }

            if (sfxPool3D.Count < MAX_3D_SFX_POOL_SIZE)
            {
                AudioSource newSrc = CreateSFXSource(true);
                sfxPool3D.Add(newSrc);
                return newSrc;
            }

            // Voice Stealing: Steal the source that has been playing longest
            AudioSource oldest = sfxPool3D[0];
            float maxTime = -1f;
            for (int i = 0; i < sfxPool3D.Count; i++)
            {
                if (sfxPool3D[i] != null && sfxPool3D[i].time > maxTime)
                {
                    maxTime = sfxPool3D[i].time;
                    oldest = sfxPool3D[i];
                }
            }
            if (oldest != null)
            {
                oldest.Stop();
                return oldest;
            }
            return sfxPool3D[0];
        }
    }
}
