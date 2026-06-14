using UnityEngine;

public enum UpgradeType { MaxHealth, SpeedBoost, DamageBoost, WeaponUnlock }
public enum CardRarity { Common, Uncommon, Rare, Epic, Legendary, Mythic }

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
    public UpgradeType upgradeType;

    [Header("Stat Values")]
    public float upgradeValue;

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
}