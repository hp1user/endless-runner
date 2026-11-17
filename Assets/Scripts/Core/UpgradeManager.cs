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

    [Header("Boss Drops")]

    [Tooltip("Pool of guaranteed cards that drop when a boss is defeated.")]

    public List<UpgradeCard> bossDropPool = new List<UpgradeCard>();

    

    [Tooltip("Percent chance a non-guaranteed boss card is Mythic")]

    [Range(0f, 100f)] public float bossMythicChance = 5f;

    [Tooltip("Percent chance a non-guaranteed boss card is Legendary")]

    [Range(0f, 100f)] public float bossLegendaryChance = 30f;

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

    [Tooltip("If true, upgrade cards can be selected and confirmed by swiping left or right, bypassing the confirm button.")]
    public bool useSwipeToConfirm = true;

    public bool IsSelectionOpen => (cardSelectionPanel != null && cardSelectionPanel.activeSelf) || isSequenceRunning;

    private bool isSequenceRunning = false;

    private List<GameObject> activeUICards = new List<GameObject>();

    private Dictionary<CardRarity, List<UpgradeCard>> deckByRarity = new Dictionary<CardRarity, List<UpgradeCard>>();

    // Tracks non-stackable cards (e.g. Crit Rate, Crit Damage, Weapon Unlocks) claimed this run to prevent them from dropping again
    private HashSet<UpgradeCard> claimedUniqueCards = new HashSet<UpgradeCard>();

    // Remembers what card we tapped on

    private UpgradeCard pendingCard;

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);

        claimedUniqueCards.Clear();

#if UNITY_EDITOR
        // Auto-discover all UpgradeCard assets in the project so newly created cards are immediately in the drop pool
        string[] cardGuids = UnityEditor.AssetDatabase.FindAssets("t:UpgradeCard");
        foreach (string guid in cardGuids)
        {
            string path = UnityEditor.AssetDatabase.GUIDToAssetPath(guid);
            UpgradeCard card = UnityEditor.AssetDatabase.LoadAssetAtPath<UpgradeCard>(path);
            if (card != null && !allAvailableCards.Contains(card))
            {
                allAvailableCards.Add(card);
            }
        }
#endif

        foreach (CardRarity rarity in System.Enum.GetValues(typeof(CardRarity)))
            deckByRarity[rarity] = new List<UpgradeCard>();

        foreach (UpgradeCard card in allAvailableCards)
        {
            if (card != null && deckByRarity.ContainsKey(card.rarity))
            {
                deckByRarity[card.rarity].Add(card);
            }
        }

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

        isSequenceRunning = false;

    }

    private void HandleLevelUp(int currentLevel)

    {

        // When a boss is defeated, HandleBossDefeated already triggers LevelUpSequence with boss drops
        if (GameManager.Instance != null && GameManager.Instance.IsBossLevel(currentLevel - 1))
        {
            return;
        }

        if (isSequenceRunning) return;

        StartCoroutine(LevelUpSequence(currentLevel, false));

    }

    private void HandleBossDefeated()

    {

        if (isSequenceRunning) return;

        int currentLevel = GameManager.Instance != null ? GameManager.Instance.currentLevel : 5;

        StartCoroutine(LevelUpSequence(currentLevel, true));

    }

    private IEnumerator LevelUpSequence(int level, bool isBoss)

    {

        isSequenceRunning = true;

        Time.timeScale = 0f; // Freeze the game

        // 1. Show the Banner

        if (levelUpBannerPanel != null)

        {

            levelUpBannerPanel.SetActive(true);

            if (levelUpBannerText != null)

            {

                levelUpBannerText.text = isBoss 
                    ? "<color=#FFB830>★ BOSS DEFEATED ★</color>" 
                    : $"<color=#00E5FF>LEVEL {level} REACHED</color>";

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

    /// <summary>
    /// Checks whether an identical upgrade card (same reference, asset name, or display title) is already in the current hand.
    /// </summary>
    private bool IsCardAlreadyInHand(UpgradeCard card, List<UpgradeCard> hand)
    {
        if (card == null || hand == null || hand.Count == 0) return false;
        foreach (var h in hand)
        {
            if (h == null) continue;
            if (h == card) return true;
            if (h.name == card.name) return true;
            if (!string.IsNullOrEmpty(card.cardName) && !string.IsNullOrEmpty(h.cardName) && h.cardName == card.cardName) return true;
        }
        return false;
    }

    private List<UpgradeCard> DrawCards(int amountToDraw, int level, float clearTime, bool isBossDrop)

    {

        List<UpgradeCard> hand = new List<UpgradeCard>();

        for (int i = 0; i < amountToDraw; i++)

        {

            UpgradeCard drawnCard = null;

            CardRarity chosenRarity = CardRarity.Common;

            if (isBossDrop)

            {

                if (i == 0 && bossDropPool != null && bossDropPool.Count > 0)

                {

                    drawnCard = GetWeightedBossDrop(hand);

                }

                else

                {

                    float bossRoll = Random.Range(0f, 100f);

                    if (bossRoll <= bossMythicChance) chosenRarity = CardRarity.Mythic;

                    else if (bossRoll <= bossMythicChance + bossLegendaryChance) chosenRarity = CardRarity.Legendary;

                    else chosenRarity = CardRarity.Epic;

                    drawnCard = GetRandomCardOfRarity(chosenRarity, hand);

                }

            }

            else

            {

                chosenRarity = CalculateNormalRarity(level, clearTime);

                drawnCard = GetRandomCardOfRarity(chosenRarity, hand);

                // Fallback 1: Step down through lower rarities
                CardRarity stepDownRarity = chosenRarity;
                while (drawnCard == null && stepDownRarity > CardRarity.Common)
                {
                    stepDownRarity--;
                    drawnCard = GetRandomCardOfRarity(stepDownRarity, hand);
                }

                // Fallback 2: Step up through higher rarities if lower rarities had no unselected cards
                if (drawnCard == null)
                {
                    CardRarity stepUpRarity = chosenRarity;
                    while (drawnCard == null && stepUpRarity < CardRarity.Mythic)
                    {
                        stepUpRarity++;
                        drawnCard = GetRandomCardOfRarity(stepUpRarity, hand);
                    }
                }

            }

            // Universal Fallback: If still null (e.g. boss drop was empty or normal roll found nothing in chosen rarities),
            // scan all available cards for ANY unselected, unclaimed card
            if (drawnCard == null)
            {
                List<UpgradeCard> anyValid = allAvailableCards.FindAll(c => c != null && !IsCardAlreadyClaimed(c) && !IsCardAlreadyInHand(c, hand));
                if (anyValid.Count > 0)
                {
                    drawnCard = anyValid[Random.Range(0, anyValid.Count)];
                }
            }

            if (drawnCard != null) hand.Add(drawnCard);

        }

        return hand;

    }

    /// <summary>
    /// Checks whether an upgrade card contains unique non-stackable mechanics (Crit Rate, Crit Damage, Weapon Unlocks).
    /// </summary>
    public static bool IsCardNonStackable(UpgradeCard card)
    {
        if (card == null || card.effects == null) return false;
        foreach (var effect in card.effects)
        {
            if (effect.upgradeType == UpgradeType.CritRate ||
                effect.upgradeType == UpgradeType.CritDamage ||
                effect.upgradeType == UpgradeType.WeaponUnlock)
            {
                return true;
            }
        }
        return false;
    }

    /// <summary>
    /// Returns true if this card has already been claimed and should not be offered to the player again.
    /// Only applies to non-stackable cards (Crit Rate, Crit Damage, Weapon Unlocks).
    /// </summary>
    public bool IsCardAlreadyClaimed(UpgradeCard card)
    {
        if (card == null) return false;
        if (!IsCardNonStackable(card)) return false;

        foreach (var claimed in claimedUniqueCards)
        {
            if (claimed == null) continue;
            if (claimed == card || claimed.name == card.name || (!string.IsNullOrEmpty(claimed.cardName) && claimed.cardName == card.cardName))
                return true;
        }

        if (IsWeaponCardAlreadyOwned(card)) return true;
        if (Player.Control.PlayerController.Instance != null && Player.Control.PlayerController.Instance.HasAppliedCard(card)) return true;
        return false;
    }

    /// <summary>
    /// Marks a non-stackable card as claimed for this run, permanently removing it from all active drop pools.
    /// </summary>
    public void RegisterCardClaimed(UpgradeCard card)
    {
        if (card == null) return;
        if (IsCardNonStackable(card))
        {
            claimedUniqueCards.Add(card);

            // Remove from active deck so it cannot roll again
            if (deckByRarity.ContainsKey(card.rarity))
            {
                deckByRarity[card.rarity].Remove(card);
            }

            if (bossDropPool != null)
            {
                bossDropPool.Remove(card);
            }

            Debug.Log($"<color=cyan>[UpgradeManager]</color> Removed non-stackable card <b>{card.cardName}</b> ({card.rarity}) from active drop pools.");
        }
    }

    private bool IsWeaponCardAlreadyOwned(UpgradeCard card)

    {

        if (card == null || Player.Control.PlayerController.Instance == null) return false;

        foreach (var effect in card.effects)

        {

            if (effect.upgradeType == UpgradeType.WeaponUnlock && !string.IsNullOrEmpty(effect.weaponID))

            {

                if (Player.Control.PlayerController.Instance.HasWeapon(effect.weaponID))

                    return true;

            }

        }

        return false;

    }

    private UpgradeCard GetWeightedBossDrop(List<UpgradeCard> hand = null)

    {

        if (bossDropPool == null || bossDropPool.Count == 0) return null;

        // 1. Prioritize unowned weapon unlock cards not in hand and not claimed
        List<UpgradeCard> candidates = new List<UpgradeCard>();

        foreach (var card in bossDropPool)

        {

            if (card == null) continue;

            if (IsCardAlreadyInHand(card, hand)) continue;

            if (IsCardAlreadyClaimed(card)) continue;

            bool isWeaponUnlock = false;
            if (card.effects != null)
            {
                foreach (var effect in card.effects)
                {
                    if (effect.upgradeType == UpgradeType.WeaponUnlock)
                    {
                        isWeaponUnlock = true;
                        break;
                    }
                }
            }

            if (isWeaponUnlock)
            {
                candidates.Add(card);
            }

        }

        // 2. If no weapon drops available, pick other boss cards not in hand and not claimed
        if (candidates.Count == 0)

        {

            foreach (var card in bossDropPool)

            {

                if (card == null) continue;

                if (IsCardAlreadyInHand(card, hand)) continue;

                if (IsCardAlreadyClaimed(card)) continue;

                candidates.Add(card);

            }

        }

        if (candidates.Count == 0) return null;

        float totalWeight = 0f;

        List<KeyValuePair<UpgradeCard, float>> weightedCards = new List<KeyValuePair<UpgradeCard, float>>();

        foreach (var card in candidates)

        {

            if (card == null) continue;

            float weight = 0f;

            switch (card.rarity)

            {

                case CardRarity.Common: weight = 100f; break;

                case CardRarity.Uncommon: weight = 50f; break;

                case CardRarity.Rare: weight = 25f; break;

                case CardRarity.Epic: weight = 10f; break;

                case CardRarity.Legendary: weight = 4f; break;

                case CardRarity.Mythic: weight = 1f; break;

            }

            weightedCards.Add(new KeyValuePair<UpgradeCard, float>(card, weight));

            totalWeight += weight;

        }

        if (totalWeight <= 0f) return candidates[0];

        float roll = Random.Range(0f, totalWeight);

        float currentWeight = 0f;

        foreach (var pair in weightedCards)

        {

            currentWeight += pair.Value;

            if (roll <= currentWeight)

            {

                return pair.Key;

            }

        }

        return candidates[0];

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

    private UpgradeCard GetRandomCardOfRarity(CardRarity rarity, List<UpgradeCard> hand = null)
    {
        if (!deckByRarity.ContainsKey(rarity)) return null;

        List<UpgradeCard> specificPile = deckByRarity[rarity];
        if (specificPile == null || specificPile.Count == 0) return null;

        List<UpgradeCard> validCards = specificPile.FindAll(c => c != null && !IsCardAlreadyClaimed(c) && !IsCardAlreadyInHand(c, hand));
        if (validCards.Count == 0) return null;

        return validCards[Random.Range(0, validCards.Count)];
    }

    // --- UI DISPLAY LOGIC ---

    private void DisplayCardsOnScreen(List<UpgradeCard> cardsToShow)

    {

        TouchManager.IsInputBlocked = true;

        UpgradeCardUI.ResetSwipeState();

        EndlessRunner.Audio.AudioManager.Instance?.PlayUISound(EndlessRunner.Audio.UISoundType.CardPopup);

        if (cardSelectionPanel != null) cardSelectionPanel.SetActive(true);

        if (confirmButton != null) confirmButton.gameObject.SetActive(false);

        pendingCard = null;

        foreach (GameObject oldCard in activeUICards)

        {

            if (oldCard != null) Destroy(oldCard);

        }

        activeUICards.Clear();

        if (cardContainer != null)
        {
            for (int i = cardContainer.childCount - 1; i >= 0; i--)
            {
                Destroy(cardContainer.GetChild(i).gameObject);
            }
        }

        foreach (UpgradeCard cardData in cardsToShow)

        {

            if (cardData == null || cardUIPrefab == null || cardContainer == null) continue;

            GameObject newCardObj = Instantiate(cardUIPrefab, cardContainer);

            UpgradeCardUI uiScript = newCardObj.GetComponent<UpgradeCardUI>();

            if (uiScript != null) uiScript.Initialize(cardData);

            activeUICards.Add(newCardObj);

        }

        if (cardSelectionPanel != null)
        {
            var responsiveLayout = cardSelectionPanel.GetComponent<UpgradeCardResponsiveLayout>();
            if (responsiveLayout == null)
            {
                responsiveLayout = cardSelectionPanel.AddComponent<UpgradeCardResponsiveLayout>();
            }
            responsiveLayout.UpdateLayout();
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

        if (confirmButton != null && !useSwipeToConfirm) confirmButton.gameObject.SetActive(true);

        Debug.Log($"<color=cyan>[UI]</color> Card Selected: {chosenCard.cardName}. Swipe left or right to confirm!");

    }

    /// <summary>
    /// Confirms and applies the selected upgrade card (called directly via card swipe gestures).
    /// </summary>
    public void ConfirmUpgrade(UpgradeCard card)
    {
        if (card != null)
        {
            pendingCard = card;
        }
        ConfirmSelection();
    }

    // Called when you press Confirm
    public void ConfirmSelection()
    {
        if (pendingCard == null) return;

        if (Player.Control.PlayerController.Instance != null)
        {
            Player.Control.PlayerController.Instance.ApplyUpgrade(pendingCard);
        }

        RegisterCardClaimed(pendingCard);

        if (cardSelectionPanel != null) cardSelectionPanel.SetActive(false);
        if (confirmButton != null) confirmButton.gameObject.SetActive(false);
        isSequenceRunning = false;
        TouchManager.IsInputBlocked = false;
        Time.timeScale = 1f;
    }

    /// <summary>
    /// For Checkpoint / Testing: Automatically applies random upgrade cards directly to the player without opening UI.
    /// </summary>
    public List<UpgradeCard> ApplyRandomUpgrades(int count)
    {
        List<UpgradeCard> appliedCards = new List<UpgradeCard>();

        if (allAvailableCards == null || allAvailableCards.Count == 0)
        {
            Debug.LogWarning("[UpgradeManager] Cannot apply random upgrades: allAvailableCards is empty!");
            return appliedCards;
        }

        if (Player.Control.PlayerController.Instance == null)
        {
            Debug.LogWarning("[UpgradeManager] Cannot apply random upgrades: PlayerController Instance is null!");
            return appliedCards;
        }

        List<UpgradeCard> candidatePool = new List<UpgradeCard>();
        foreach (var c in allAvailableCards)
        {
            if (c != null && !IsCardAlreadyClaimed(c))
            {
                candidatePool.Add(c);
            }
        }

        for (int i = 0; i < count; i++)
        {
            if (candidatePool.Count == 0) break;

            int randomIndex = Random.Range(0, candidatePool.Count);
            UpgradeCard randomCard = candidatePool[randomIndex];

            if (randomCard != null)
            {
                Player.Control.PlayerController.Instance.ApplyUpgrade(randomCard);
                RegisterCardClaimed(randomCard);
                appliedCards.Add(randomCard);

                if (IsCardNonStackable(randomCard))
                {
                    candidatePool.RemoveAt(randomIndex);
                }

                Debug.Log($"<color=green>[Checkpoint]</color> Awarded Card {i + 1}/{count}: <b>{randomCard.cardName}</b> ({randomCard.rarity})");
            }
        }

        return appliedCards;
    }

}