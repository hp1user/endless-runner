using System.Collections.Generic;
using UnityEngine;

public enum UpgradeType { MaxHealth, SpeedBoost, DamageBoost, WeaponUnlock }
public enum CardRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

[System.Serializable]
public class CardEffect
{
    public UpgradeType upgradeType;
    public float upgradeValue;
}

[CreateAssetMenu(fileName = "New Upgrade Card", menuName = "Game Data/Upgrade Card")]
public class UpgradeCard : ScriptableObject
{
    [Header("UI Visuals")]
    public string cardName = "New Upgrade";
    [TextArea(2, 4)]
    public string description = "What does this card do?";
    public Sprite cardIcon;

    [Header("Card Rules")]
    public CardRarity rarity = CardRarity.Common;
    
    [Header("Card Effects")]
    public List<CardEffect> effects = new List<CardEffect>();

    // --- NEW: RARITY BACKGROUND VISUALS ---
    [Header("Rarity Background")]
    [Tooltip("The specific background frame for this rarity. If left empty, it will use the color below instead.")]
    public Sprite rarityBackgroundImage;
    [Tooltip("The color to tint the background if no Sprite is provided.")]
    public Color rarityColor = Color.white;

    [Header("Selection Visuals")]
    [Tooltip("The specific background frame when this card is selected.")]
    public Sprite selectedCardSprite;
    [Tooltip("The color to tint the background when selected, if no Selected Card Sprite is provided.")]
    public Color selectedCardColor = Color.yellow;

#if UNITY_EDITOR
    private void OnValidate()
    {
        // Don't run during play mode or while Unity is recompiling/updating
        if (Application.isPlaying || UnityEditor.EditorApplication.isUpdating) return;
        
        // Find the CardRarityDatabase in the project
        string[] guids = UnityEditor.AssetDatabase.FindAssets("t:CardRarityDatabase");
        if (guids.Length > 0)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guids[0]);
            CardRarityDatabase db = UnityEditor.AssetDatabase.LoadAssetAtPath<CardRarityDatabase>(path);
            if (db != null)
            {
                var settings = db.GetSettingsForRarity(rarity);
                if (settings != null)
                {
                    rarityBackgroundImage = settings.rarityBackgroundImage;
                    rarityColor = settings.rarityColor;
                    selectedCardSprite = settings.selectedCardSprite;
                    selectedCardColor = settings.selectedCardColor;
                }
            }
        }
    }
#endif
}