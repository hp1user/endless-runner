using System.Collections.Generic;
using UnityEngine;

namespace EndlessRunner.Audio
{
    [System.Serializable]
    public class WeaponAudioProfile
    {
        public string weaponID = "M4";
        public string displayName = "Assault Rifle";
        public AudioClip[] fireClips;
        public AudioClip reloadClip;
        public AudioClip magOutClip;
        public AudioClip magInClip;
        public AudioClip emptyClickClip;
        public AudioClip equipClip;
        [Range(0f, 1f)] public float volume = 1f;

        public AudioClip GetRandomFireClip()
        {
            if (fireClips == null || fireClips.Length == 0) return null;
            return fireClips[Random.Range(0, fireClips.Length)];
        }
    }

    [CreateAssetMenu(fileName = "AudioDatabase", menuName = "Game Data/Audio Database")]
    public class AudioDatabase : ScriptableObject
    {
        [Header("Master Volume Controls")]
        [Range(0f, 1f)] public float masterVolume = 1.0f;
        [Range(0f, 1f)] public float musicVolume = 0.85f;
        [Range(0f, 1f)] public float sfxVolume = 1.0f;

        [Header("Perimeter Breach Dynamic Music Layers")]
        [Tooltip("Layer 0: Ambient / Base groove that plays continuously in loop.")]
        public AudioClip perimeterBreachLayer0;
        [Range(0f, 1f)] public float layer0Volume = 1.0f;

        [Tooltip("Layer 1: Battle / Combat groove that plays only during active combat.")]
        public AudioClip perimeterBreachLayer1;
        [Range(0f, 1f)] public float layer1Volume = 1.0f;

        [Header("Adaptive Music Settings")]
        [Tooltip("Volume multiplier applied to Layer 0 when an upgrade card selection popup appears.")]
        [Range(0.05f, 0.8f)] public float cardPopupDuckVolume = 0.35f;

        [Tooltip("Pitch multiplier applied to Layer 0 when an upgrade card selection popup appears (subtle gradual slow down).")]
        [Range(0.7f, 1.0f)] public float cardPopupSlowPitch = 0.88f;

        [Tooltip("Pitch applied to Layer 0 when player opens the Weapon Wheel to match game slow-mo.")]
        [Range(0.2f, 0.9f)] public float weaponWheelSlowPitch = 0.55f;

        [Tooltip("Speed of gradual volume ducking and fading (lower = smoother and more gradual).")]
        public float musicFadeSpeed = 2.2f;

        [Tooltip("Speed of gradual pitch transitions (tape-stop slow-down effect).")]
        public float pitchSmoothSpeed = 3.2f;

        [Tooltip("Grace period (seconds) before battle Layer 1 fades out after last combat encounter.")]
        public float battleGracePeriod = 2.0f;

        [Header("UI Sound Effects")]
        public AudioClip buttonClickSound;
        public AudioClip cardPopupSound;
        public AudioClip cardClickSound;
        public AudioClip cardSwipeConfirmSound;
        public AudioClip levelUpBannerSound;
        public AudioClip weaponWheelOpenSound;
        public AudioClip weaponWheelCloseSound;
        public AudioClip weaponWheelSelectSound;

        [Header("Weapons Sound Profiles")]
        public List<WeaponAudioProfile> weaponProfiles = new List<WeaponAudioProfile>();

        [Header("Bug & Minion Sound Effects")]
        public AudioClip[] bugSkitterClips;
        public AudioClip[] bugAttackClips;
        public AudioClip[] bugHurtClips;
        public AudioClip[] bugDeathClips;

        [Header("Boss Sound Effects")]
        public AudioClip bossRoarClip;
        public AudioClip bossSpitClip;
        public AudioClip bossHurtClip;
        public AudioClip bossDeathClip;
        public AudioClip bossPhaseChangeClip;

        [Header("Impact & Damage Sound Effects")]
        public AudioClip[] bulletHitFleshClips;
        public AudioClip[] bulletHitSurfaceClips;
        public AudioClip[] playerHurtClips;
        public AudioClip playerDeathClip;

        public WeaponAudioProfile GetWeaponProfile(string weaponID)
        {
            if (weaponProfiles == null) return null;
            for (int i = 0; i < weaponProfiles.Count; i++)
            {
                if (weaponProfiles[i] != null && weaponProfiles[i].weaponID == weaponID)
                    return weaponProfiles[i];
            }
            return null;
        }

        public AudioClip GetUISound(UISoundType type)
        {
            return type switch
            {
                UISoundType.ButtonClick => buttonClickSound,
                UISoundType.CardPopup => cardPopupSound,
                UISoundType.CardClick => cardClickSound,
                UISoundType.CardSwipeConfirm => cardSwipeConfirmSound,
                UISoundType.LevelUpBanner => levelUpBannerSound,
                UISoundType.WeaponWheelOpen => weaponWheelOpenSound,
                UISoundType.WeaponWheelClose => weaponWheelCloseSound,
                UISoundType.WeaponWheelSelect => weaponWheelSelectSound,
                _ => null
            };
        }

        public AudioClip GetRandomBugSound(BugSoundType type)
        {
            AudioClip[] array = type switch
            {
                BugSoundType.Skitter => bugSkitterClips,
                BugSoundType.Attack => bugAttackClips,
                BugSoundType.Hurt => bugHurtClips,
                BugSoundType.Death => bugDeathClips,
                _ => null
            };
            if (array == null || array.Length == 0) return null;
            return array[Random.Range(0, array.Length)];
        }

        public AudioClip GetBossSound(BossSoundType type)
        {
            return type switch
            {
                BossSoundType.Roar => bossRoarClip,
                BossSoundType.Spit => bossSpitClip,
                BossSoundType.Hurt => bossHurtClip,
                BossSoundType.Death => bossDeathClip,
                BossSoundType.PhaseChange => bossPhaseChangeClip,
                _ => null
            };
        }

        public AudioClip GetRandomImpactSound(ImpactSoundType type)
        {
            return type switch
            {
                ImpactSoundType.BulletHitFlesh => (bulletHitFleshClips != null && bulletHitFleshClips.Length > 0) ? bulletHitFleshClips[Random.Range(0, bulletHitFleshClips.Length)] : null,
                ImpactSoundType.BulletHitSurface => (bulletHitSurfaceClips != null && bulletHitSurfaceClips.Length > 0) ? bulletHitSurfaceClips[Random.Range(0, bulletHitSurfaceClips.Length)] : null,
                ImpactSoundType.PlayerHurt => (playerHurtClips != null && playerHurtClips.Length > 0) ? playerHurtClips[Random.Range(0, playerHurtClips.Length)] : null,
                ImpactSoundType.PlayerDeath => playerDeathClip,
                _ => null
            };
        }
    }
}

