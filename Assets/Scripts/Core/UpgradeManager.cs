using System.Collections.Generic;
using UnityEngine;

public class UpgradeManager : MonoBehaviour
{
    public static UpgradeManager Instance { get; private set; }

    [Header("The Deck")]
    public List<UpgradeCard> allAvailableCards = new List<UpgradeCard>();

    [Header("Dynamic UI References")]
    public GameObject cardSelectionPanel;   // The dark background panel
    public Transform cardContainer;         // The Horizontal Layout Group (usually the panel itself)
    public GameObject cardUIPrefab;         // The Card Prefab from your project folder

    private List<GameObject> activeUICards = new List<GameObject>();
    private Dictionary<CardRarity, List<UpgradeCard>> deckByRarity = new Dictionary<CardRarity, List<UpgradeCard>>();

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        foreach (CardRarity rarity in System.Enum.GetValues(typeof(CardRarity)))
            deckByRarity[rarity] = new List<UpgradeCard>();

        foreach (UpgradeCard card in allAvailableCards)
            deckByRarity[card.rarity].Add(card);

        if (cardSelectionPanel != null) cardSelectionPanel.SetActive(false);
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
        Time.timeScale = 0f;
        float timeTaken = GameManager.Instance != null ? GameManager.Instance.levelClearTimer : 60f;

        List<UpgradeCard> drawnCards = DrawCards(3, currentLevel, timeTaken, false);
        DisplayCardsOnScreen(drawnCards);
    }

    private void HandleBossDefeated()
    {
        Time.timeScale = 0f;
        int currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : 5;

        List<UpgradeCard> drawnCards = DrawCards(3, currentLevel, 0f, true);
        DisplayCardsOnScreen(drawnCards);
    }

    // --- THE MATH BRAIN ---
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

    // --- THE DYNAMIC UI GENERATOR ---
    private void DisplayCardsOnScreen(List<UpgradeCard> cardsToShow)
    {
        if (cardSelectionPanel != null) cardSelectionPanel.SetActive(true);

        // 1. Destroy any old cards sitting in the menu
        foreach (GameObject oldCard in activeUICards)
        {
            if (oldCard != null) Destroy(oldCard);
        }
        activeUICards.Clear();

        // 2. Spawn fresh new cards!
        foreach (UpgradeCard cardData in cardsToShow)
        {
            if (cardData == null || cardUIPrefab == null || cardContainer == null) continue;

            GameObject newCardObj = Instantiate(cardUIPrefab, cardContainer);

            UpgradeCardUI uiScript = newCardObj.GetComponent<UpgradeCardUI>();
            if (uiScript != null)
            {
                uiScript.Initialize(cardData);
            }

            activeUICards.Add(newCardObj);
        }
    }

    public void SelectUpgrade(UpgradeCard chosenCard)
    {
        if (Player.Control.PlayerController.Instance != null)
        {
            Player.Control.PlayerController.Instance.ApplyUpgrade(chosenCard);
        }

        if (cardSelectionPanel != null) cardSelectionPanel.SetActive(false);
        Time.timeScale = 1f;
    }
}