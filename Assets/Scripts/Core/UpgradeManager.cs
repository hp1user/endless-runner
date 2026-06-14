using System.Collections.Generic;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("The Deck")]
    public List<UpgradeCard> allAvailableCards = new List<UpgradeCard>();

    [Header("Dynamic UI References")]
    public GameObject cardSelectionPanel;
    public Transform cardContainer;
    public GameObject cardUIPrefab;

    [Header("New UX Features")]
    [Tooltip("The panel that flashes 'Level 2 Reached!'")]
    public GameObject levelUpBannerPanel;
    public TextMeshProUGUI levelUpBannerText;

    [Tooltip("The button the player presses after picking a card")]
    public Button confirmButton;

    private List<GameObject> activeUICards = new List<GameObject>();
    private Dictionary<CardRarity, List<UpgradeCard>> deckByRarity = new Dictionary<CardRarity, List<UpgradeCard>>();

    // Remembers what card we tapped on
    private UpgradeCard pendingCard;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (CardRarity rarity in System.Enum.GetValues(typeof(CardRarity)))
            deckByRarity[rarity] = new List<UpgradeCard>();

        foreach (UpgradeCard card in allAvailableCards)
            deckByRarity[card.rarity].Add(card);

        if (cardSelectionPanel != null) cardSelectionPanel.SetActive(false);
        if (levelUpBannerPanel != null) levelUpBannerPanel.SetActive(false);
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);
    }

    private void OnEnable()
    {
        GameManager.OnLevelCompleted += HandleLevelUp;
        GameManager.OnBossDefeated += HandleBossDefeated;
    }

    private void OnDisable()
    {
        GameManager.OnLevelCompleted -= HandleLevelUp;
        GameManager.OnBossDefeated -= HandleBossDefeated;
    }

    private void HandleLevelUp(int currentLevel)
    {
        StartCoroutine(LevelUpSequence(currentLevel, false));
    }

    private void HandleBossDefeated()
    {
        int currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : 5;
        StartCoroutine(LevelUpSequence(currentLevel, true));
    }

    private IEnumerator LevelUpSequence(int level, bool isBoss)
    {
        Time.timeScale = 0f; // Freeze the game

        // 1. Show the Banner
        if (levelUpBannerPanel != null)
        {
            levelUpBannerPanel.SetActive(true);
            if (levelUpBannerText != null)
            {
                levelUpBannerText.text = isBoss ? "BOSS DEFEATED!" : $"LEVEL {level} REACHED!";
            }
        }

        // 2. Wait for 1.5 seconds in REAL TIME
        yield return new WaitForSecondsRealtime(1.5f);

        // 3. Hide banner, show cards
        if (levelUpBannerPanel != null) levelUpBannerPanel.SetActive(false);

        float timeTaken = GameManager.Instance != null ? GameManager.Instance.levelClearTimer : 60f;
        List<UpgradeCard> drawnCards = DrawCards(3, level, timeTaken, isBoss);

        DisplayCardsOnScreen(drawnCards);
    }

    // --- THE FULL MATH BRAIN ---
    private List<UpgradeCard> DrawCards(int amountToDraw, int level, float clearTime, bool isBossDrop)
    {
        List<UpgradeCard> hand = new List<UpgradeCard>();

        for (int i = 0; i < amountToDraw; i++)
        {
            CardRarity chosenRarity;

            if (isBossDrop)
            {
                if (i == 0) chosenRarity = CardRarity.Epic;
                else
                {
                    float bossRoll = Random.Range(0f, 100f);
                    if (bossRoll <= 5f) chosenRarity = CardRarity.Mythic;
                    else if (bossRoll <= 35f) chosenRarity = CardRarity.Legendary;
                    else chosenRarity = CardRarity.Epic;
                }
            }
            else
            {
                chosenRarity = CalculateNormalRarity(level, clearTime);
            }

            UpgradeCard drawnCard = GetRandomCardOfRarity(chosenRarity);

            while (drawnCard == null && chosenRarity > CardRarity.Common)
            {
                chosenRarity--;
                drawnCard = GetRandomCardOfRarity(chosenRarity);
            }

            if (drawnCard != null) hand.Add(drawnCard);
        }

        return hand;
    }

    private CardRarity CalculateNormalRarity(int level, float clearTime)
    {
        float commonWeight = 60f;
        float uncommonWeight = 30f;
        float rareWeight = 10f;
        float epicWeight = 0f;
        float legendaryWeight = 0f;

        epicWeight += level * 1.5f;
        legendaryWeight += level * 0.2f;

        commonWeight -= level * 2f;
        commonWeight = Mathf.Max(10f, commonWeight);

        if (clearTime < 15f)
        {
            rareWeight += 15f;
            epicWeight += 5f;
        }

        float totalWeight = commonWeight + uncommonWeight + rareWeight + epicWeight + legendaryWeight;
        float roll = Random.Range(0f, totalWeight);

        if (roll <= legendaryWeight) return CardRarity.Legendary;
        roll -= legendaryWeight;

        if (roll <= epicWeight) return CardRarity.Epic;
        roll -= epicWeight;

        if (roll <= rareWeight) return CardRarity.Rare;
        roll -= rareWeight;

        if (roll <= uncommonWeight) return CardRarity.Uncommon;

        return CardRarity.Common;
    }

    private UpgradeCard GetRandomCardOfRarity(CardRarity rarity)
    {
        List<UpgradeCard> specificPile = deckByRarity[rarity];
        if (specificPile == null || specificPile.Count == 0) return null;

        return specificPile[Random.Range(0, specificPile.Count)];
    }

    // --- UI DISPLAY LOGIC ---
    private void DisplayCardsOnScreen(List<UpgradeCard> cardsToShow)
    {
        if (cardSelectionPanel != null) cardSelectionPanel.SetActive(true);
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);
        pendingCard = null;

        foreach (GameObject oldCard in activeUICards)
        {
            if (oldCard != null) Destroy(oldCard);
        }
        activeUICards.Clear();

        foreach (UpgradeCard cardData in cardsToShow)
        {
            if (cardData == null || cardUIPrefab == null || cardContainer == null) continue;

            GameObject newCardObj = Instantiate(cardUIPrefab, cardContainer);

            UpgradeCardUI uiScript = newCardObj.GetComponent<UpgradeCardUI>();
            if (uiScript != null) uiScript.Initialize(cardData);

            activeUICards.Add(newCardObj);
        }
    }

    // Called when you tap a card
    public void PreviewUpgrade(UpgradeCardUI clickedUI, UpgradeCard chosenCard)
    {
        pendingCard = chosenCard;

        // Update visuals for all active cards
        foreach (GameObject cardObj in activeUICards)
        {
            if (cardObj != null)
            {
                UpgradeCardUI uiScript = cardObj.GetComponent<UpgradeCardUI>();
                if (uiScript != null)
                {
                    uiScript.SetSelected(uiScript == clickedUI);
                }
            }
        }

        if (confirmButton != null) confirmButton.gameObject.SetActive(true);

        Debug.Log($"<color=cyan>[UI]</color> Card Selected: {chosenCard.cardName}. Waiting for confirmation...");
    }

    // Called when you press Confirm
    public void ConfirmSelection()
    {
        if (pendingCard == null) return;

        if (Player.Control.PlayerController.Instance != null)
        {
            Player.Control.PlayerController.Instance.ApplyUpgrade(pendingCard);
        }

        if (cardSelectionPanel != null) cardSelectionPanel.SetActive(false);
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);

        Time.timeScale = 1f;
    }
}