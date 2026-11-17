using UnityEngine;
using TMPro;

namespace EndlessRunner.UI
{
    [CreateAssetMenu(fileName = "BuffModalSettings", menuName = "Endless Runner/UI/Buff Modal Settings")]
    public class BuffModalSettings : ScriptableObject
    {
        private static BuffModalSettings _instance;
        public static BuffModalSettings Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = Resources.Load<BuffModalSettings>("BuffModalSettings");
#if UNITY_EDITOR
                    if (_instance == null)
                    {
                        _instance = CreateInstance<BuffModalSettings>();
                        if (!UnityEditor.AssetDatabase.IsValidFolder("Assets/Resources"))
                        {
                            UnityEditor.AssetDatabase.CreateFolder("Assets", "Resources");
                        }
                        UnityEditor.AssetDatabase.CreateAsset(_instance, "Assets/Resources/BuffModalSettings.asset");
                        UnityEditor.AssetDatabase.SaveAssets();
                    }
#endif
                }
                return _instance;
            }
        }

        [Header("Chassis & Frame Settings")]
        [Range(700f, 1000f)]
        public float dialogWidth = 860f;
        [Range(1100f, 1600f)]
        public float dialogHeight = 1420f;
        public Color frameBorderColor = new Color(0.13f, 0.72f, 0.88f, 1f); // #22D3EE Cyan
        public Color frameBackgroundColor = new Color(0.04f, 0.07f, 0.11f, 0.96f);
        public Sprite frameBackgroundSprite;
        public Sprite closeButtonSprite;

        [Header("Stat Icons & Palette")]
        public Sprite hpIcon;
        public Color hpColor = new Color(0.13f, 0.77f, 0.37f, 1f); // #22C55E
        public Sprite damageIcon;
        public Color damageColor = new Color(0.96f, 0.62f, 0.04f, 1f); // #F59E0B
        public Sprite critRateIcon;
        public Color critRateColor = new Color(0.66f, 0.33f, 0.97f, 1f); // #A855F7
        public Sprite critDmgIcon;
        public Color critDmgColor = new Color(0.75f, 0.52f, 0.99f, 1f); // #C084FC
        public Sprite speedIcon;
        public Color speedColor = new Color(0.02f, 0.71f, 0.83f, 1f); // #06B6D4
        public Sprite armorIcon;
        public Color armorColor = new Color(0.23f, 0.51f, 0.96f, 1f); // #3B82F6

        [Header("Category Colors & Sprites")]
        public Color survivalColor = new Color(0.13f, 0.77f, 0.37f, 1f); // Green
        public Sprite survivalIcon;
        public Color offenseColor = new Color(0.96f, 0.62f, 0.04f, 1f); // Orange
        public Sprite offenseIcon;
        public Color mobilityColor = new Color(0.02f, 0.71f, 0.83f, 1f); // Cyan
        public Sprite mobilityIcon;
        public Color defenseColor = new Color(0.23f, 0.51f, 0.96f, 1f); // Blue
        public Sprite defenseIcon;
        public Color criticalColor = new Color(0.66f, 0.33f, 0.97f, 1f); // Purple
        public Sprite criticalIcon;

        [Header("Card Tile Styling")]
        public Sprite cardBackgroundSprite;
        public Sprite socketBackgroundSprite;
        public Color cardTileBackgroundColor = new Color(0.06f, 0.09f, 0.14f, 0.95f);
        public Color socketBorderColor = new Color(0.13f, 0.72f, 0.88f, 0.9f);

        [Header("HUD Trigger Buttons")]
        [Range(40f, 80f)]
        public float hudButtonSize = 54f;
        public Sprite buffButtonSprite;
        public Sprite pauseButtonSprite;
        public Color hudButtonColor = new Color(0.08f, 0.11f, 0.16f, 0.95f);
    }
}

