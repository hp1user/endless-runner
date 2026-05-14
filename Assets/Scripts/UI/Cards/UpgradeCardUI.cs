using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeCardUI : MonoBehaviour
{
    [Header("Hierarchy References")]
    public Image backgroundImage;
    public TextMeshProUGUI titleText;
    public Image mainIconImage;
    public TextMeshProUGUI descriptionText;

    // --- NEW: The Value Text ---
    [Tooltip("Drag the text object that displays the number (e.g. '+20') here")]
    public TextMeshProUGUI valueText;

    private UpgradeCard myCardData;
    private Button myButton;

    private void Awake()
    {
        myButton = GetComponent<Button>();
        if (myButton != null)
        {
            myButton.onClick.AddListener(OnCardClicked);
        }
    }

    public void Initialize(UpgradeCard cardData)
    {
        myCardData = cardData;

        // Set the standard text and icon
        titleText.text = cardData.cardName;
        descriptionText.text = cardData.description;
        mainIconImage.sprite = cardData.cardIcon;

        // --- NEW: Format the Stat Value Text ---
        if (valueText != null)
        {
            string suffix = "";

            // Add a cool suffix based on what kind of stat it is!
            if (cardData.upgradeType == UpgradeType.DamageBoost) suffix = "%";
            else if (cardData.upgradeType == UpgradeType.MaxHealth) suffix = " HP";
            else if (cardData.upgradeType == UpgradeType.SpeedBoost) suffix = " SPD";

            valueText.text = "+" + cardData.upgradeValue.ToString() + suffix;
        }

        // RARITY BACKGROUND LOGIC
        if (cardData.rarityBackgroundImage != null)
        {
            backgroundImage.sprite = cardData.rarityBackgroundImage;
            backgroundImage.color = Color.white;
        }
        else
        {
            backgroundImage.sprite = null;
            backgroundImage.color = cardData.rarityColor;
        }
    }

    private void OnCardClicked()
    {
        if (UpgradeManager.Instance != null && myCardData != null)
        {
            UpgradeManager.Instance.PreviewUpgrade(myCardData);
        }
    }
}