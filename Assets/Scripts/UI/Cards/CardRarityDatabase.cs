using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class RaritySettingsData
{
    public CardRarity rarity;
    
    [Header("Rarity Background")]
    public Sprite rarityBackgroundImage;
    public Color rarityColor = Color.white;
    
    [Header("Selection Visuals")]
    public Sprite selectedCardSprite;
    public Color selectedCardColor = Color.yellow;
}

[CreateAssetMenu(fileName = "CardRarityDatabase", menuName = "Endless Runner/Card Rarity Database")]
public class CardRarityDatabase : ScriptableObject
{
    public RaritySettingsData commonSettings = new RaritySettingsData { rarity = CardRarity.Common };
    public RaritySettingsData uncommonSettings = new RaritySettingsData { rarity = CardRarity.Uncommon };
    public RaritySettingsData rareSettings = new RaritySettingsData { rarity = CardRarity.Rare };
    public RaritySettingsData epicSettings = new RaritySettingsData { rarity = CardRarity.Epic };
    public RaritySettingsData legendarySettings = new RaritySettingsData { rarity = CardRarity.Legendary };
    public RaritySettingsData mythicSettings = new RaritySettingsData { rarity = CardRarity.Mythic };
    
    public RaritySettingsData GetSettingsForRarity(CardRarity rarity)
    {
        return rarity switch
        {
            CardRarity.Common => commonSettings,
            CardRarity.Uncommon => uncommonSettings,
            CardRarity.Rare => rareSettings,
            CardRarity.Epic => epicSettings,
            CardRarity.Legendary => legendarySettings,
            CardRarity.Mythic => mythicSettings,
            _ => null
        };
    }
}
