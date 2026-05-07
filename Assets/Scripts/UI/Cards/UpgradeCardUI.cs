using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardUI : MonoBehaviour
{
    [Header("Hierarchy References")]
    [Tooltip("Drag the UpgradeCard Image component here (itself)")]
    public Image backgroundImage; // This is the 'UpgradeCard' root image

    [Tooltip("Drag the CardName object here")]
    public TextMeshProUGUI titleText;

    [Tooltip("Drag the CardIcon object here")]
    public Image mainIconImage;

    [Tooltip("Drag the Discription object here (inside the Scroll View)")]
    public TextMeshProUGUI descriptionText;

    private UpgradeCard myCardData;
    private Button myButton;

    private void Awake()
    {
        // Automatically grab the Button component on this object
        myButton = GetComponent<Button>();
        if (myButton != null)
        {
            myButton.onClick.AddListener(OnCardClicked);
        }
        else
        {
            Debug.LogWarning($"<color=yellow>[UI]</color> No Button component found on {gameObject.name}! Add one so it can be clicked.");
        }
    }

    public void Initialize(UpgradeCard cardData)
    {
        myCardData = cardData;

        // 1. Set the standard text and icon
        titleText.text = cardData.cardName;
        descriptionText.text = cardData.description;
        mainIconImage.sprite = cardData.cardIcon;

        // 2. RARITY BACKGROUND LOGIC
        if (cardData.rarityBackgroundImage != null)
        {
            // If we have a fancy rarity border/frame, use it and reset color to white
            backgroundImage.sprite = cardData.rarityBackgroundImage;
            backgroundImage.color = Color.white;
        }
        else
        {
            // If we don't have a sprite, clear the sprite and apply the rarity color
            backgroundImage.sprite = null;
            backgroundImage.color = cardData.rarityColor;
        }
    }

    private void OnCardClicked()
    {
        if (UpgradeManager.Instance != null && myCardData != null)
        {
            UpgradeManager.Instance.SelectUpgrade(myCardData);
        }
    }
}