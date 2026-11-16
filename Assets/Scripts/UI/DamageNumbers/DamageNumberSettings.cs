using UnityEngine;
using TMPro;

namespace UI.DamageNumbers
{
    [CreateAssetMenu(fileName = "DamageNumberSettings", menuName = "Endless Runner/Combat/Damage Number Settings")]
    public class DamageNumberSettings : ScriptableObject
    {
        [Header("Font Settings")]
        public TMP_FontAsset customFont;
        [Range(2f, 12f)]
        public float baseFontSize = 5.2f;

        [Header("Position & Offset Controls")]
        [Tooltip("Height above regular enemy / minion pivot to spawn the floating damage text.")]
        [Range(0.2f, 4.0f)]
        public float enemyHeadHeightOffset = 1.25f;

        [Tooltip("Offset distance towards the camera from the boss impact point, preventing text from clipping into 3D meshes.")]
        [Range(0.05f, 1.0f)]
        public float bossCameraOffset = 0.25f;

        [Tooltip("Vertical upward lift applied to the impact point on the boss.")]
        [Range(0.0f, 1.0f)]
        public float bossVerticalOffset = 0.20f;

        [Tooltip("Random horizontal / vertical fanning spread so consecutive hits (shotguns/auto-fire) do not overlap.")]
        [Range(0.0f, 0.8f)]
        public float randomSpreadRadius = 0.25f;

        [Header("Scale Multipliers")]
        [Tooltip("Scale multiplier for normal enemy hits.")]
        [Range(0.5f, 2.5f)]
        public float normalScale = 1.0f;

        [Tooltip("Scale multiplier for Boss Head / Critical hits.")]
        [Range(0.5f, 3.0f)]
        public float bossHeadScale = 1.45f;

        [Tooltip("Scale multiplier for Boss Leg hits.")]
        [Range(0.5f, 2.5f)]
        public float bossLegScale = 1.25f;

        [Tooltip("Scale multiplier for Boss generic body hits.")]
        [Range(0.5f, 2.5f)]
        public float bossGeneralScale = 1.15f;

        [Tooltip("Scale multiplier for already broken boss parts.")]
        [Range(0.5f, 2.0f)]
        public float bossBrokenScale = 0.95f;

        [Tooltip("Scale multiplier for Critical Strike hits.")]
        [Range(0.5f, 3.0f)]
        public float critScale = 1.55f;

        [Header("Animation & Motion")]
        [Tooltip("How fast the damage number ascends upward in world space.")]
        [Range(0.5f, 5.0f)]
        public float floatUpSpeed = 1.8f;

        [Tooltip("Horizontal drift speed for spreading numbers outward.")]
        [Range(0.0f, 2.0f)]
        public float driftSpeed = 0.45f;

        [Tooltip("Total duration (in seconds) the damage number stays alive before recycling.")]
        [Range(0.3f, 2.0f)]
        public float duration = 0.75f;

        [Header("Color Palette")]
        public Color normalColor = new Color(1f, 0.96f, 0.65f, 1f);       // Warm bright yellow/white
        public Color bossHeadColor = new Color(1f, 0.22f, 0.22f, 1f);     // Fiery crimson/red
        public Color bossLegColor = new Color(1f, 0.62f, 0.05f, 1f);      // Vibrant amber/orange
        public Color bossGeneralColor = new Color(1f, 0.85f, 0.25f, 1f);  // Boss body gold
        public Color bossBrokenColor = new Color(0.72f, 0.72f, 0.72f, 0.9f); // Dim silver
        public Color critColor = new Color(0.75f, 0.35f, 1.0f, 1f);       // Vibrant neon purple (#BF5AF2)
    }
}

