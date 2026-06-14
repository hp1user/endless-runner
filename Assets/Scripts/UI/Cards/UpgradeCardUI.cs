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

        SetSelected(false);

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

    }

    private void OnCardClicked()
    {
        if (UpgradeManager.Instance != null && myCardData != null)
        {
            UpgradeManager.Instance.PreviewUpgrade(this, myCardData);
        }
    }

    public void SetSelected(bool isSelected)
    {
        UpdateBackgroundVisuals(isSelected);

        // Fallback visual if you still want the scaling
        transform.localScale = isSelected ? new Vector3(1.05f, 1.05f, 1.05f) : Vector3.one;
    }

    private void UpdateBackgroundVisuals(bool isSelected)
    {
        if (myCardData == null || backgroundImage == null) return;

        if (isSelected)
        {
            if (myCardData.selectedCardSprite != null)
            {
                backgroundImage.sprite = myCardData.selectedCardSprite;
                backgroundImage.color = Color.white;
            }
            else
            {
                backgroundImage.sprite = myCardData.rarityBackgroundImage;
                backgroundImage.color = myCardData.selectedCardColor;
            }
        }
        else
        {
            if (myCardData.rarityBackgroundImage != null)
            {
                backgroundImage.sprite = myCardData.rarityBackgroundImage;
                backgroundImage.color = Color.white;
            }
            else
            {
                backgroundImage.sprite = null;
                backgroundImage.color = myCardData.rarityColor;
            }
        }
    }
}