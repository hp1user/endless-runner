using UnityEngine;
using TMPro;

namespace EndlessRunner.Data
{
    [CreateAssetMenu(fileName = "New HUD Theme", menuName = "Endless Runner/HUD Theme Profile")]
    public class HUDThemeProfile : ScriptableObject
    {
        [Header("Theme Info")]
        public string themeName = "Neon Sci-Fi Cyan";
        [TextArea(2, 3)]
        public string description = "High-tech vertical side docks with cyan and emerald neon vitals and flush screen bezels.";

        [Header("Side Wings HUD (Vertical Docks)")]
        [Tooltip("Background frame for the left status dock (vitals).")]
        public Sprite dockLeftPanel;
        [Tooltip("Background frame for the right ammo dock.")]
        public Sprite dockRightPanel;
        [Tooltip("Vertical sunken channel track sprite for meter backgrounds.")]
        public Sprite verticalTrack;
        [Tooltip("Vertical fill sprite for health, armor, and ammo meters.")]
        public Sprite verticalFill;
        [Tooltip("Heart icon for health meter.")]
        public Sprite heartIcon;
        [Tooltip("Shield icon for armor meter.")]
        public Sprite shieldIcon;
        [Tooltip("Bullet icon for current loaded ammo meter.")]
        public Sprite bulletIcon;
        [Tooltip("Ammo box / reserve icon for reserve ammo meter.")]
        public Sprite ammoBoxIcon;
        [Tooltip("Default weapon silhouette icon.")]
        public Sprite defaultWeaponIcon;

        [Header("Side Wings Dimensions & Offsets")]
        [Range(80f, 220f)]
        public float dockWidth = 110f;
        [Range(300f, 700f)]
        public float dockHeight = 520f;
        public float dockOffsetY = -120f;
        public float dockMarginX = 0f;

        [Header("Top Bar HUD (Classic Horizontal)")]
        [Tooltip("Panel background for top-left vitals card.")]
        public Sprite topStatusPanel;
        [Tooltip("Panel background for top-right ammo card.")]
        public Sprite topAmmoPanel;
        [Tooltip("Horizontal track for health and armor.")]
        public Sprite horizontalTrack;
        [Tooltip("Horizontal fill for health and armor.")]
        public Sprite horizontalFill;

        [Header("Boss Health HUD (Top Center)")]
        [Tooltip("Hazard frame panel for top-center boss health bar.")]
        public Sprite bossPanel;
        [Tooltip("Cyber skull crest icon for boss card.")]
        public Sprite bossSkullIcon;
        [Tooltip("Boss health bar track.")]
        public Sprite bossTrack;
        [Tooltip("Boss health bar fill.")]
        public Sprite bossFill;

        [Header("Theme Colors")]
        public Color healthColor = new Color(0.12f, 0.95f, 0.58f, 1f);     // Neon emerald
        public Color armorColor = new Color(0.15f, 0.85f, 1.0f, 1f);       // Electric cyan
        public Color ammoColor = new Color(1.0f, 0.72f, 0.12f, 1f);         // Amber gold
        public Color ghostDamageColor = new Color(0.95f, 0.22f, 0.22f, 1f); // Crimson lag
        public Color bossHealthColor = new Color(0.95f, 0.15f, 0.20f, 1f);  // Boss Crimson
        public Color bossGhostColor = new Color(1.0f, 0.78f, 0.15f, 1f);   // Boss Gold lag

        [Header("Typography")]
        public TMP_FontAsset fontAsset;
    }
}
