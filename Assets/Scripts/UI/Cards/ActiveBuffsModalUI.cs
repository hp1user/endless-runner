using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using EndlessRunner.Audio;

namespace EndlessRunner.UI
{
    public enum BuffCategory
    {
        Survival,
        Offense,
        Mobility,
        Defense,
        Critical
    }

    public class ActiveBuffsModalUI : MonoBehaviour
    {
        public static ActiveBuffsModalUI Instance { get; private set; }

        [Header("Settings Reference")]
        [SerializeField] private BuffModalSettings settings;

        [Header("HUD Trigger Button (Top-Left)")]
        [SerializeField] private Button buffHUDButton;
        [SerializeField] private TextMeshProUGUI badgeCountText;
        [SerializeField] private GameObject badgeRoot;

        [Header("Modal Windows")]
        [SerializeField] private GameObject modalOverlay;
        [SerializeField] private Transform cardListContent;
        [SerializeField] private GameObject emptyStateNotice;
        [SerializeField] private TextMeshProUGUI activeCountSubtitle;

        [Header("Stat Readouts")]
        [SerializeField] private TextMeshProUGUI healthValText;
        [SerializeField] private Transform healthPipsContainer;
        [SerializeField] private TextMeshProUGUI damageValText;
        [SerializeField] private Transform damagePipsContainer;
        [SerializeField] private TextMeshProUGUI critRateValText;
        [SerializeField] private Transform critRatePipsContainer;
        [SerializeField] private TextMeshProUGUI critDmgValText;
        [SerializeField] private Transform critDmgPipsContainer;
        [SerializeField] private TextMeshProUGUI speedValText;
        [SerializeField] private Transform speedPipsContainer;
        [SerializeField] private TextMeshProUGUI armorValText;
        [SerializeField] private Transform armorPipsContainer;

        [Header("Close Button")]
        [SerializeField] private Button closeButton;

        public bool IsOpen => modalOverlay != null && modalOverlay.activeSelf;

        private void Awake()
        {
            if (Instance == null)
            {
                Instance = this;
            }
            else if (Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            if (settings == null) settings = BuffModalSettings.Instance;

            EnsureUIHierarchy();
        }

        private void OnEnable()
        {
            Player.Control.PlayerController.OnActiveCardsUpdated += HandleActiveCardsChanged;
        }

        private void OnDisable()
        {
            Player.Control.PlayerController.OnActiveCardsUpdated -= HandleActiveCardsChanged;
        }

        private void Start()
        {
            UpdateBadge();
            if (modalOverlay != null) modalOverlay.SetActive(false);
        }

        private void Update()
        {
#if ENABLE_INPUT_SYSTEM
            if (UnityEngine.InputSystem.Keyboard.current != null)
            {
                if (UnityEngine.InputSystem.Keyboard.current.bKey.wasPressedThisFrame ||
                    UnityEngine.InputSystem.Keyboard.current.cKey.wasPressedThisFrame)
                {
                    if (IsOpen) CloseModal();
                    else OpenModal();
                }
            }
#else
            if (Input.GetKeyDown(KeyCode.B) || Input.GetKeyDown(KeyCode.C))
            {
                if (IsOpen) CloseModal();
                else OpenModal();
            }
#endif
        }

        public void OpenModal()
        {
            if (UpgradeManager.Instance != null && UpgradeManager.Instance.IsSelectionOpen) return;

            TouchManager.IsInputBlocked = true;
            Time.timeScale = 0f;

            AudioManager.Instance?.PlayUISound(UISoundType.CardPopup);

            if (modalOverlay != null) modalOverlay.SetActive(true);

            RefreshStats();
            RefreshCardsList();
        }

        public void CloseModal()
        {
            AudioManager.Instance?.PlayUISound(UISoundType.ButtonClick);

            if (modalOverlay != null) modalOverlay.SetActive(false);

            TouchManager.IsInputBlocked = false;
            Time.timeScale = 1f;
        }

        private void HandleActiveCardsChanged()
        {
            UpdateBadge();
            if (IsOpen)
            {
                RefreshStats();
                RefreshCardsList();
            }
        }

        public void UpdateBadge()
        {
            int count = 0;
            if (Player.Control.PlayerController.Instance != null && Player.Control.PlayerController.Instance.ActiveCards != null)
            {
                count = Player.Control.PlayerController.Instance.ActiveCards.Count;
            }

            if (badgeCountText != null)
            {
                badgeCountText.text = count.ToString();
            }

            if (badgeRoot != null)
            {
                badgeRoot.SetActive(count > 0);
            }
        }

        private void RefreshStats()
        {
            var player = Player.Control.PlayerController.Instance;
            if (player == null) return;
            var s = settings != null ? settings : BuffModalSettings.Instance;

            // 1. HP
            if (healthValText != null)
                healthValText.text = $"{player.CurrentHealth:F0} / {player.RuntimeMaxHealth:F0}";
            float hpRatio = player.RuntimeMaxHealth > 0 ? Mathf.Clamp01(player.CurrentHealth / player.RuntimeMaxHealth) : 1f;
            UpdateSegmentedPips(healthPipsContainer, hpRatio, s != null ? s.hpColor : new Color(0.13f, 0.77f, 0.37f));

            // 2. Damage
            if (damageValText != null)
            {
                float bonusDmgPct = (player.damageMultiplier - 1.0f) * 100f;
                string sign = bonusDmgPct >= 0 ? "+" : "";
                damageValText.text = $"{sign}{bonusDmgPct:F0}% ({player.damageMultiplier:F2}x)";
            }
            float dmgRatio = Mathf.Clamp01((player.damageMultiplier - 0.8f) / 1.2f);
            UpdateSegmentedPips(damagePipsContainer, dmgRatio, s != null ? s.damageColor : new Color(0.96f, 0.62f, 0.04f));

            // 3. Crit Rate
            if (critRateValText != null)
                critRateValText.text = $"{player.TotalCritChance:F0}%";
            float critRateRatio = Mathf.Clamp01(player.TotalCritChance / 50f);
            UpdateSegmentedPips(critRatePipsContainer, critRateRatio, s != null ? s.critRateColor : new Color(0.66f, 0.33f, 0.97f));

            // 4. Crit DMG
            if (critDmgValText != null)
                critDmgValText.text = $"{player.TotalCritMultiplier:F2}x";
            float critDmgRatio = Mathf.Clamp01((player.TotalCritMultiplier - 1.0f) / 1.5f);
            UpdateSegmentedPips(critDmgPipsContainer, critDmgRatio, s != null ? s.critDmgColor : new Color(0.75f, 0.52f, 0.99f));

            // 5. Speed
            if (speedValText != null)
                speedValText.text = $"{player.RuntimeMovementSpeed:F1} m/s";
            float speedRatio = Mathf.Clamp01(player.RuntimeMovementSpeed / 10f);
            UpdateSegmentedPips(speedPipsContainer, speedRatio, s != null ? s.speedColor : new Color(0.02f, 0.71f, 0.83f));

            // 6. Armor
            if (armorValText != null)
            {
                string siphon = player.armorPerKill > 0 ? $" (+{player.armorPerKill:F0}/k)" : "";
                armorValText.text = $"{player.CurrentArmor:F0}{siphon}";
            }
            float armorRatio = Mathf.Clamp01(player.CurrentArmor / 100f);
            UpdateSegmentedPips(armorPipsContainer, armorRatio, s != null ? s.armorColor : new Color(0.23f, 0.51f, 0.96f));
        }

        private void UpdateSegmentedPips(Transform container, float ratio, Color activeColor)
        {
            if (container == null) return;
            int childCount = container.childCount;
            if (childCount == 0) return;

            int activeCount = Mathf.Clamp(Mathf.RoundToInt(ratio * childCount), 1, childCount);
            Color darkOffColor = new Color(0.12f, 0.17f, 0.24f, 0.9f);

            for (int i = 0; i < childCount; i++)
            {
                Image pipImg = container.GetChild(i).GetComponent<Image>();
                if (pipImg != null)
                {
                    pipImg.color = (i < activeCount) ? activeColor : darkOffColor;
                }
            }
        }

        private void RefreshCardsList()
        {
            if (cardListContent == null) return;

            for (int i = cardListContent.childCount - 1; i >= 0; i--)
            {
                Destroy(cardListContent.GetChild(i).gameObject);
            }

            var player = Player.Control.PlayerController.Instance;
            int totalActive = (player != null && player.ActiveCards != null) ? player.ActiveCards.Count : 0;

            if (activeCountSubtitle != null)
            {
                activeCountSubtitle.text = $"<color=#64748B><b>{totalActive} ACTIVE</b></color> <color=#0E7490>////</color>";
            }

            if (player == null || player.ActiveCards == null || player.ActiveCards.Count == 0)
            {
                if (emptyStateNotice != null) emptyStateNotice.SetActive(true);
                return;
            }

            if (emptyStateNotice != null) emptyStateNotice.SetActive(false);

            // Group cards by identity to show stack counts (x1, x2, etc.)
            Dictionary<string, (UpgradeCard card, int count)> groupedCards = new Dictionary<string, (UpgradeCard, int)>();
            foreach (var card in player.ActiveCards)
            {
                if (card == null) continue;
                string key = string.IsNullOrEmpty(card.cardName) ? card.name : card.cardName;
                if (groupedCards.ContainsKey(key))
                {
                    var existing = groupedCards[key];
                    groupedCards[key] = (existing.card, existing.count + 1);
                }
                else
                {
                    groupedCards[key] = (card, 1);
                }
            }

            foreach (var kvp in groupedCards.Values)
            {
                CreateSciFiCardTile(kvp.card, kvp.count);
            }
        }

        public static BuffCategory GetCardCategory(UpgradeCard card)
        {
            if (card == null || card.effects == null || card.effects.Count == 0) return BuffCategory.Offense;

            foreach (var eff in card.effects)
            {
                if (eff.upgradeType == UpgradeType.CritRate || eff.upgradeType == UpgradeType.CritDamage)
                    return BuffCategory.Critical;
                if (eff.upgradeType == UpgradeType.MaxHealth)
                    return BuffCategory.Survival;
                if (eff.upgradeType == UpgradeType.ArmorPerKill)
                    return BuffCategory.Defense;
                if (eff.upgradeType == UpgradeType.SpeedBoost)
                    return BuffCategory.Mobility;
                if (eff.upgradeType == UpgradeType.DamageBoost || eff.upgradeType == UpgradeType.WeaponUnlock)
                    return BuffCategory.Offense;
            }

            string name = (card.cardName + " " + card.name).ToLowerInvariant();
            if (name.Contains("health") || name.Contains("vitality") || name.Contains("heal") || name.Contains("life")) return BuffCategory.Survival;
            if (name.Contains("armor") || name.Contains("shield") || name.Contains("dampen")) return BuffCategory.Defense;
            if (name.Contains("speed") || name.Contains("kinetic") || name.Contains("adrenaline")) return BuffCategory.Mobility;
            if (name.Contains("crit") || name.Contains("deadly") || name.Contains("fatal") || name.Contains("lethal") || name.Contains("precision")) return BuffCategory.Critical;
            return BuffCategory.Offense;
        }

        private void CreateSciFiCardTile(UpgradeCard card, int count)
        {
            if (card == null || cardListContent == null) return;
            var s = settings != null ? settings : BuffModalSettings.Instance;

            BuffCategory cat = GetCardCategory(card);
            Color themeColor = GetRarityColor(card.rarity);
            string rarityLabel = card.rarity.ToString().ToUpper();

            // Main Card Tile Container
            GameObject tile = new GameObject($"CardItem_{card.name}", typeof(RectTransform), typeof(Image));
            tile.transform.SetParent(cardListContent, false);
            RectTransform rect = tile.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(800f, 112f);

            Image bg = tile.GetComponent<Image>();
            if (s != null && s.cardBackgroundSprite != null)
            {
                bg.sprite = s.cardBackgroundSprite;
                bg.type = Image.Type.Sliced;
                bg.color = Color.white;
            }
            else
            {
                bg.color = (s != null && s.cardTileBackgroundColor.a > 0.01f)
                    ? s.cardTileBackgroundColor
                    : new Color(0.12f, 0.14f, 0.18f, 0.96f);
            }

            // Glowing Accent Border (colored by Rarity)
            Outline outline = tile.AddComponent<Outline>();
            outline.effectColor = new Color(themeColor.r, themeColor.g, themeColor.b, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            // Left Socket (Hexagonal/chamfered icon socket)
            GameObject socketObj = new GameObject("IconSocket", typeof(RectTransform), typeof(Image));
            socketObj.transform.SetParent(tile.transform, false);
            RectTransform sockRect = socketObj.GetComponent<RectTransform>();
            sockRect.anchorMin = new Vector2(0f, 0.5f);
            sockRect.anchorMax = new Vector2(0f, 0.5f);
            sockRect.pivot = new Vector2(0.5f, 0.5f);
            sockRect.anchoredPosition = new Vector2(58f, 0f);
            sockRect.sizeDelta = new Vector2(80f, 80f);

            Image sockImg = socketObj.GetComponent<Image>();
            sockImg.color = new Color(themeColor.r * 0.18f, themeColor.g * 0.18f, themeColor.b * 0.18f, 0.9f);

            Outline sockOutline = socketObj.AddComponent<Outline>();
            sockOutline.effectColor = themeColor;
            sockOutline.effectDistance = new Vector2(1.5f, -1.5f);

            // Socket Icon
            Sprite iconSprite = card.cardIcon != null ? card.cardIcon : GetCategorySprite(cat, s);
            if (iconSprite != null)
            {
                GameObject iconObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                iconObj.transform.SetParent(socketObj.transform, false);
                RectTransform icRect = iconObj.GetComponent<RectTransform>();
                icRect.anchorMin = new Vector2(0.5f, 0.5f);
                icRect.anchorMax = new Vector2(0.5f, 0.5f);
                icRect.pivot = new Vector2(0.5f, 0.5f);
                icRect.sizeDelta = new Vector2(52f, 52f);
                Image ic = iconObj.GetComponent<Image>();
                ic.sprite = iconSprite;
                ic.color = themeColor;
                ic.preserveAspect = true;
                ic.raycastTarget = false;
            }
            else
            {
                // Procedural symbol fallback (using supported TMP characters)
                GameObject symObj = new GameObject("Symbol", typeof(RectTransform), typeof(TextMeshProUGUI));
                symObj.transform.SetParent(socketObj.transform, false);
                RectTransform syRect = symObj.GetComponent<RectTransform>();
                syRect.anchorMin = Vector2.zero;
                syRect.anchorMax = Vector2.one;
                syRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI sym = symObj.GetComponent<TextMeshProUGUI>();
                sym.alignment = TextAlignmentOptions.Center;
                sym.fontSize = 28;
                sym.fontStyle = FontStyles.Bold;
                sym.color = themeColor;
                sym.text = cat switch
                {
                    BuffCategory.Survival => "+",
                    BuffCategory.Offense => "▲",
                    BuffCategory.Critical => "%",
                    BuffCategory.Mobility => ">>",
                    BuffCategory.Defense => "#",
                    _ => "*"
                };
            }

            // Rarity Pill Tag (auto-sizes to fit any rarity tier string cleanly)
            GameObject pillObj = new GameObject("RarityPill", typeof(RectTransform), typeof(Image), typeof(HorizontalLayoutGroup), typeof(ContentSizeFitter));
            pillObj.transform.SetParent(tile.transform, false);
            RectTransform pillRect = pillObj.GetComponent<RectTransform>();
            pillRect.anchorMin = new Vector2(0f, 1f);
            pillRect.anchorMax = new Vector2(0f, 1f);
            pillRect.pivot = new Vector2(0f, 1f);
            pillRect.anchoredPosition = new Vector2(118f, -14f);

            HorizontalLayoutGroup hlg = pillObj.GetComponent<HorizontalLayoutGroup>();
            hlg.padding = new RectOffset(10, 10, 2, 2);
            hlg.childAlignment = TextAnchor.MiddleCenter;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = false;
            hlg.childForceExpandHeight = false;

            ContentSizeFitter csf = pillObj.GetComponent<ContentSizeFitter>();
            csf.horizontalFit = ContentSizeFitter.FitMode.PreferredSize;
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            Image pillImg = pillObj.GetComponent<Image>();
            pillImg.color = new Color(themeColor.r, themeColor.g, themeColor.b, 0.95f);

            GameObject pillTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            pillTextObj.transform.SetParent(pillObj.transform, false);
            TextMeshProUGUI pt = pillTextObj.GetComponent<TextMeshProUGUI>();
            pt.alignment = TextAlignmentOptions.Center;
            pt.fontSize = 12;
            pt.fontStyle = FontStyles.Bold;
            pt.color = Color.black;
            pt.text = rarityLabel;

            // Card Title
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(tile.transform, false);
            RectTransform titleRect = titleObj.GetComponent<RectTransform>();
            titleRect.anchorMin = new Vector2(0f, 1f);
            titleRect.anchorMax = new Vector2(1f, 1f);
            titleRect.pivot = new Vector2(0f, 1f);
            titleRect.anchoredPosition = new Vector2(118f, -40f);
            titleRect.sizeDelta = new Vector2(-220f, 28f);

            TextMeshProUGUI titleText = titleObj.GetComponent<TextMeshProUGUI>();
            titleText.fontSize = 22;
            titleText.fontStyle = FontStyles.Bold;
            titleText.color = Color.white;
            titleText.text = card.cardName.ToUpper();

            // Description Text
            GameObject descObj = new GameObject("Desc", typeof(RectTransform), typeof(TextMeshProUGUI));
            descObj.transform.SetParent(tile.transform, false);
            RectTransform descRect = descObj.GetComponent<RectTransform>();
            descRect.anchorMin = new Vector2(0f, 1f);
            descRect.anchorMax = new Vector2(1f, 1f);
            descRect.pivot = new Vector2(0f, 1f);
            descRect.anchoredPosition = new Vector2(118f, -70f);
            descRect.sizeDelta = new Vector2(-210f, 36f);

            TextMeshProUGUI descText = descObj.GetComponent<TextMeshProUGUI>();
            descText.fontSize = 16;
            descText.color = new Color(0.80f, 0.85f, 0.92f, 0.95f);
            descText.text = card.description;
            descText.enableWordWrapping = true;
            descText.overflowMode = TextOverflowModes.Ellipsis;

            // Stack Multiplier Box (Right slot: [ x1 ], [ x2 ])
            GameObject stackBox = new GameObject("StackBox", typeof(RectTransform), typeof(Image));
            stackBox.transform.SetParent(tile.transform, false);
            RectTransform sbRect = stackBox.GetComponent<RectTransform>();
            sbRect.anchorMin = new Vector2(1f, 0.5f);
            sbRect.anchorMax = new Vector2(1f, 0.5f);
            sbRect.pivot = new Vector2(1f, 0.5f);
            sbRect.anchoredPosition = new Vector2(-20f, 0f);
            sbRect.sizeDelta = new Vector2(62f, 36f);

            Image sbImg = stackBox.GetComponent<Image>();
            sbImg.color = new Color(0.08f, 0.12f, 0.18f, 0.9f);

            Outline sbOutline = stackBox.AddComponent<Outline>();
            sbOutline.effectColor = new Color(0.35f, 0.45f, 0.60f, 0.8f);
            sbOutline.effectDistance = new Vector2(1f, -1f);

            GameObject stackTextObj = new GameObject("Text", typeof(RectTransform), typeof(TextMeshProUGUI));
            stackTextObj.transform.SetParent(stackBox.transform, false);
            RectTransform stRect = stackTextObj.GetComponent<RectTransform>();
            stRect.anchorMin = Vector2.zero;
            stRect.anchorMax = Vector2.one;
            stRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI stText = stackTextObj.GetComponent<TextMeshProUGUI>();
            stText.fontSize = 18;
            stText.fontStyle = FontStyles.Bold;
            stText.alignment = TextAlignmentOptions.Center;
            stText.color = Color.white;
            stText.text = $"x{count}";
        }

        public static Color GetRarityColor(CardRarity rarity)
        {
            return rarity switch
            {
                CardRarity.Common => new Color(0.70f, 0.72f, 0.75f, 1f),
                CardRarity.Uncommon => new Color(0.25f, 0.85f, 0.35f, 1f),
                CardRarity.Rare => new Color(0.30f, 0.65f, 1.00f, 1f),
                CardRarity.Epic => new Color(0.75f, 0.35f, 0.95f, 1f),
                CardRarity.Legendary => new Color(1.00f, 0.70f, 0.15f, 1f),
                CardRarity.Mythic => new Color(0.95f, 0.20f, 0.20f, 1f),
                _ => new Color(0.70f, 0.72f, 0.75f, 1f)
            };
        }

        private Color GetCategoryColor(BuffCategory cat, BuffModalSettings s)
        {
            if (s != null)
            {
                return cat switch
                {
                    BuffCategory.Survival => s.survivalColor,
                    BuffCategory.Offense => s.offenseColor,
                    BuffCategory.Mobility => s.mobilityColor,
                    BuffCategory.Defense => s.defenseColor,
                    BuffCategory.Critical => s.criticalColor,
                    _ => Color.white
                };
            }
            return cat switch
            {
                BuffCategory.Survival => new Color(0.13f, 0.77f, 0.37f),
                BuffCategory.Offense => new Color(0.96f, 0.62f, 0.04f),
                BuffCategory.Mobility => new Color(0.02f, 0.71f, 0.83f),
                BuffCategory.Defense => new Color(0.23f, 0.51f, 0.96f),
                BuffCategory.Critical => new Color(0.66f, 0.33f, 0.97f),
                _ => Color.white
            };
        }

        private Sprite GetCategorySprite(BuffCategory cat, BuffModalSettings s)
        {
            if (s == null) return null;
            return cat switch
            {
                BuffCategory.Survival => s.survivalIcon != null ? s.survivalIcon : s.hpIcon,
                BuffCategory.Offense => s.offenseIcon != null ? s.offenseIcon : s.damageIcon,
                BuffCategory.Mobility => s.mobilityIcon != null ? s.mobilityIcon : s.speedIcon,
                BuffCategory.Defense => s.defenseIcon != null ? s.defenseIcon : s.armorIcon,
                BuffCategory.Critical => s.criticalIcon != null ? s.criticalIcon : s.critRateIcon,
                _ => null
            };
        }

        private void EnsureUIHierarchy()
        {
            Canvas canvas = GetComponentInParent<Canvas>();
            if (canvas == null) canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null) return;

            Transform canvasTr = canvas.transform;
            var s = settings != null ? settings : BuffModalSettings.Instance;

            // Cleanly remove any stale instances
            Transform oldBtn = canvasTr.Find("BuffHUDButton");
            if (oldBtn != null)
            {
                oldBtn.name = "BuffHUDButton_Old";
                oldBtn.gameObject.SetActive(false);
                Destroy(oldBtn.gameObject);
            }

            Transform oldModal = canvasTr.Find("ActiveBuffsModalOverlay");
            if (oldModal != null)
            {
                oldModal.name = "ActiveBuffsModalOverlay_Old";
                oldModal.gameObject.SetActive(false);
                Destroy(oldModal.gameObject);
            }

            // 1. Setup HUD Button (Top Left, centered horizontally at 40 inside 80px wing, centered vertically at -40)
            float btnSize = s != null ? s.hudButtonSize : 54f;
            GameObject btnObj = new GameObject("BuffHUDButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnObj.transform.SetParent(canvasTr, false);
            RectTransform rect = btnObj.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = new Vector2(40f, -40f);
            rect.sizeDelta = new Vector2(btnSize, btnSize);

            Image img = btnObj.GetComponent<Image>();
            img.color = s != null ? s.hudButtonColor : new Color(0.08f, 0.11f, 0.16f, 0.95f);

            Outline outline = btnObj.AddComponent<Outline>();
            outline.effectColor = new Color(0.24f, 0.30f, 0.40f, 0.9f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);

            if (s != null && s.buffButtonSprite != null)
            {
                img.sprite = s.buffButtonSprite;
                img.color = Color.white;
                img.preserveAspect = true;
                if (outline != null) outline.enabled = false;
            }
            else
            {
                // Procedural Playing Cards Emblem
                GameObject card1 = new GameObject("CardBack", typeof(RectTransform), typeof(Image));
                card1.transform.SetParent(btnObj.transform, false);
                RectTransform c1Rect = card1.GetComponent<RectTransform>();
                c1Rect.anchorMin = new Vector2(0.5f, 0.5f);
                c1Rect.anchorMax = new Vector2(0.5f, 0.5f);
                c1Rect.pivot = new Vector2(0.5f, 0.5f);
                c1Rect.anchoredPosition = new Vector2(-3f, 1f);
                c1Rect.sizeDelta = new Vector2(18f, 24f);
                c1Rect.localRotation = Quaternion.Euler(0f, 0f, 9f);
                Image c1Img = card1.GetComponent<Image>();
                c1Img.color = new Color(0.0f, 0.85f, 0.95f, 0.9f);
                c1Img.raycastTarget = false;

                GameObject card2 = new GameObject("CardFront", typeof(RectTransform), typeof(Image));
                card2.transform.SetParent(btnObj.transform, false);
                RectTransform c2Rect = card2.GetComponent<RectTransform>();
                c2Rect.anchorMin = new Vector2(0.5f, 0.5f);
                c2Rect.anchorMax = new Vector2(0.5f, 0.5f);
                c2Rect.pivot = new Vector2(0.5f, 0.5f);
                c2Rect.anchoredPosition = new Vector2(3f, -1f);
                c2Rect.sizeDelta = new Vector2(18f, 24f);
                c2Rect.localRotation = Quaternion.Euler(0f, 0f, -7f);
                Image c2Img = card2.GetComponent<Image>();
                c2Img.color = new Color(1.0f, 0.75f, 0.2f, 0.98f);
                c2Img.raycastTarget = false;

                GameObject inner = new GameObject("CardInner", typeof(RectTransform), typeof(Image));
                inner.transform.SetParent(card2.transform, false);
                RectTransform inRect = inner.GetComponent<RectTransform>();
                inRect.anchorMin = new Vector2(0.5f, 0.5f);
                inRect.anchorMax = new Vector2(0.5f, 0.5f);
                inRect.pivot = new Vector2(0.5f, 0.5f);
                inRect.anchoredPosition = Vector2.zero;
                inRect.sizeDelta = new Vector2(12f, 18f);
                Image inImg = inner.GetComponent<Image>();
                inImg.color = new Color(0.12f, 0.15f, 0.20f, 0.95f);
                inImg.raycastTarget = false;
            }

            // Badge Container
            GameObject bObj = new GameObject("Badge", typeof(RectTransform), typeof(Image));
            bObj.transform.SetParent(btnObj.transform, false);
            RectTransform bRect = bObj.GetComponent<RectTransform>();
            bRect.anchorMin = new Vector2(1f, 1f);
            bRect.anchorMax = new Vector2(1f, 1f);
            bRect.pivot = new Vector2(0.5f, 0.5f);
            bRect.anchoredPosition = new Vector2(-2f, -2f);
            bRect.sizeDelta = new Vector2(22f, 22f);
            Image bImg = bObj.GetComponent<Image>();
            bImg.color = new Color(1.0f, 0.72f, 0.19f, 1f);
            bImg.raycastTarget = false;
            badgeRoot = bObj;

            // Badge Text
            GameObject btObj = new GameObject("CountText", typeof(RectTransform), typeof(TextMeshProUGUI));
            btObj.transform.SetParent(bObj.transform, false);
            RectTransform btRect = btObj.GetComponent<RectTransform>();
            btRect.anchorMin = Vector2.zero;
            btRect.anchorMax = Vector2.one;
            btRect.sizeDelta = Vector2.zero;
            badgeCountText = btObj.GetComponent<TextMeshProUGUI>();
            badgeCountText.alignment = TextAlignmentOptions.Center;
            badgeCountText.fontSize = 13;
            badgeCountText.fontStyle = FontStyles.Bold;
            badgeCountText.color = Color.black;
            badgeCountText.raycastTarget = false;
            badgeCountText.text = "0";

            buffHUDButton = btnObj.GetComponent<Button>();
            buffHUDButton.onClick.AddListener(OpenModal);

            // 2. Setup Sci-Fi Modal Overlay
            modalOverlay = CreateProceduralSciFiModal(canvasTr);
        }

        private GameObject CreateProceduralSciFiModal(Transform parentCanvas)
        {
            var s = settings != null ? settings : BuffModalSettings.Instance;
            float dWidth = s != null ? s.dialogWidth : 860f;
            float dHeight = s != null ? s.dialogHeight : 1420f;
            Color frameCyan = s != null ? s.frameBorderColor : new Color(0.13f, 0.72f, 0.88f);

            // Fullscreen Backdrop (Clicking outside closes modal)
            GameObject overlay = new GameObject("ActiveBuffsModalOverlay", typeof(RectTransform), typeof(Image), typeof(Button), typeof(CanvasGroup));
            overlay.transform.SetParent(parentCanvas, false);
            RectTransform ovRect = overlay.GetComponent<RectTransform>();
            ovRect.anchorMin = Vector2.zero;
            ovRect.anchorMax = Vector2.one;
            ovRect.offsetMin = Vector2.zero;
            ovRect.offsetMax = Vector2.zero;
            Image ovImg = overlay.GetComponent<Image>();
            ovImg.color = new Color(0.02f, 0.04f, 0.07f, 0.85f);
            Button backdropBtn = overlay.GetComponent<Button>();
            backdropBtn.onClick.AddListener(CloseModal);

            // Main Sci-Fi Chassis / Tablet Panel
            GameObject dialog = new GameObject("DialogWindow", typeof(RectTransform), typeof(Image));
            dialog.transform.SetParent(overlay.transform, false);
            RectTransform diaRect = dialog.GetComponent<RectTransform>();
            diaRect.anchorMin = new Vector2(0.5f, 0.5f);
            diaRect.anchorMax = new Vector2(0.5f, 0.5f);
            diaRect.pivot = new Vector2(0.5f, 0.5f);
            diaRect.sizeDelta = new Vector2(dWidth, dHeight);

            Image diaImg = dialog.GetComponent<Image>();
            diaImg.color = s != null ? s.frameBackgroundColor : new Color(0.18f, 0.21f, 0.26f, 0.98f);

            if (s != null && s.frameBackgroundSprite != null)
            {
                GameObject borderObj = new GameObject("BorderFrame", typeof(RectTransform), typeof(Image));
                borderObj.transform.SetParent(dialog.transform, false);
                RectTransform bRect = borderObj.GetComponent<RectTransform>();
                bRect.anchorMin = Vector2.zero;
                bRect.anchorMax = Vector2.one;
                bRect.sizeDelta = Vector2.zero;
                Image bImg = borderObj.GetComponent<Image>();
                bImg.sprite = s.frameBackgroundSprite;
                bImg.type = Image.Type.Sliced;
                bImg.color = s.frameBorderColor;
                bImg.raycastTarget = false;
            }

            Outline diaOutline = dialog.AddComponent<Outline>();
            diaOutline.effectColor = frameCyan;
            diaOutline.effectDistance = new Vector2(2f, -2f);

            // Top Header: "/// ACTIVE BUFFS & STATS ///"
            GameObject titleObj = new GameObject("Title", typeof(RectTransform), typeof(TextMeshProUGUI));
            titleObj.transform.SetParent(dialog.transform, false);
            RectTransform tRect = titleObj.GetComponent<RectTransform>();
            tRect.anchorMin = new Vector2(0.5f, 1f);
            tRect.anchorMax = new Vector2(0.5f, 1f);
            tRect.pivot = new Vector2(0.5f, 1f);
            tRect.anchoredPosition = new Vector2(-20f, -24f);
            tRect.sizeDelta = new Vector2(dWidth - 140f, 44f);
            TextMeshProUGUI tText = titleObj.GetComponent<TextMeshProUGUI>();
            tText.fontSize = 32;
            tText.fontStyle = FontStyles.Bold | FontStyles.Italic;
            tText.alignment = TextAlignmentOptions.Center;
            tText.text = "<color=#22D3EE>///</color> ACTIVE BUFFS & STATS <color=#22D3EE>///</color>";

            // Close 'X' Button (Top-Right, Chamfered Tech Box)
            GameObject xBtnObj = new GameObject("CloseXButton", typeof(RectTransform), typeof(Image), typeof(Button));
            xBtnObj.transform.SetParent(dialog.transform, false);
            RectTransform xRect = xBtnObj.GetComponent<RectTransform>();
            xRect.anchorMin = new Vector2(1f, 1f);
            xRect.anchorMax = new Vector2(1f, 1f);
            xRect.pivot = new Vector2(1f, 1f);
            xRect.anchoredPosition = new Vector2(-24f, -24f);
            xRect.sizeDelta = new Vector2(46f, 46f);
            xBtnObj.GetComponent<Image>().color = new Color(0.05f, 0.15f, 0.22f, 0.95f);
            Outline xOutline = xBtnObj.AddComponent<Outline>();
            xOutline.effectColor = frameCyan;
            xOutline.effectDistance = new Vector2(1.5f, -1.5f);

            if (s != null && s.closeButtonSprite != null)
            {
                Image xImg = xBtnObj.GetComponent<Image>();
                xImg.sprite = s.closeButtonSprite;
                xImg.type = Image.Type.Simple;
                xImg.preserveAspect = true;
                xImg.color = frameCyan;
            }
            else
            {
                GameObject xTextObj = new GameObject("XText", typeof(RectTransform), typeof(TextMeshProUGUI));
                xTextObj.transform.SetParent(xBtnObj.transform, false);
                RectTransform xtRect = xTextObj.GetComponent<RectTransform>();
                xtRect.anchorMin = Vector2.zero;
                xtRect.anchorMax = Vector2.one;
                xtRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI xt = xTextObj.GetComponent<TextMeshProUGUI>();
                xt.alignment = TextAlignmentOptions.Center;
                xt.fontSize = 22;
                xt.fontStyle = FontStyles.Bold;
                xt.color = frameCyan;
                xt.text = "X";
            }
            closeButton = xBtnObj.GetComponent<Button>();
            closeButton.onClick.AddListener(CloseModal);

            // ==========================================
            // SECTION 1: "// CORE STATS"
            // ==========================================
            GameObject statSectionHeader = new GameObject("CoreStatsHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            statSectionHeader.transform.SetParent(dialog.transform, false);
            RectTransform cshRect = statSectionHeader.GetComponent<RectTransform>();
            cshRect.anchorMin = new Vector2(0f, 1f);
            cshRect.anchorMax = new Vector2(1f, 1f);
            cshRect.pivot = new Vector2(0.5f, 1f);
            cshRect.anchoredPosition = new Vector2(0f, -80f);
            cshRect.sizeDelta = new Vector2(dWidth - 60f, 30f);
            TextMeshProUGUI cshText = statSectionHeader.GetComponent<TextMeshProUGUI>();
            cshText.fontSize = 22;
            cshText.fontStyle = FontStyles.Bold;
            cshText.text = "<color=#22D3EE>// CORE STATS</color>";

            // 6 Stats Grid (2 Columns x 3 Rows)
            GameObject statsBox = new GameObject("StatsGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            statsBox.transform.SetParent(dialog.transform, false);
            RectTransform sRect = statsBox.GetComponent<RectTransform>();
            sRect.anchorMin = new Vector2(0.5f, 1f);
            sRect.anchorMax = new Vector2(0.5f, 1f);
            sRect.pivot = new Vector2(0.5f, 1f);
            sRect.anchoredPosition = new Vector2(0f, -118f);
            sRect.sizeDelta = new Vector2(dWidth - 50f, 310f);

            GridLayoutGroup grid = statsBox.GetComponent<GridLayoutGroup>();
            float colWidth = (dWidth - 70f) / 2f;
            grid.cellSize = new Vector2(colWidth, 94f);
            grid.spacing = new Vector2(12f, 10f);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = 2;

            // Stat Cards
            CreateSciFiStatCard(statsBox.transform, "MAX HP", s != null ? s.hpIcon : null, s != null ? s.hpColor : new Color(0.13f, 0.77f, 0.37f), out healthValText, out healthPipsContainer);
            CreateSciFiStatCard(statsBox.transform, "DAMAGE", s != null ? s.damageIcon : null, s != null ? s.damageColor : new Color(0.96f, 0.62f, 0.04f), out damageValText, out damagePipsContainer);
            CreateSciFiStatCard(statsBox.transform, "CRIT RATE", s != null ? s.critRateIcon : null, s != null ? s.critRateColor : new Color(0.66f, 0.33f, 0.97f), out critRateValText, out critRatePipsContainer);
            CreateSciFiStatCard(statsBox.transform, "CRIT DMG", s != null ? s.critDmgIcon : null, s != null ? s.critDmgColor : new Color(0.75f, 0.52f, 0.99f), out critDmgValText, out critDmgPipsContainer);
            CreateSciFiStatCard(statsBox.transform, "SPEED", s != null ? s.speedIcon : null, s != null ? s.speedColor : new Color(0.02f, 0.71f, 0.83f), out speedValText, out speedPipsContainer);
            CreateSciFiStatCard(statsBox.transform, "ARMOR", s != null ? s.armorIcon : null, s != null ? s.armorColor : new Color(0.23f, 0.51f, 0.96f), out armorValText, out armorPipsContainer);

            // ==========================================
            // SECTION 2: "// ACTIVE BUFFS"
            // ==========================================
            GameObject buffSectionHeader = new GameObject("ActiveBuffsHeader", typeof(RectTransform), typeof(TextMeshProUGUI));
            buffSectionHeader.transform.SetParent(dialog.transform, false);
            RectTransform bshRect = buffSectionHeader.GetComponent<RectTransform>();
            bshRect.anchorMin = new Vector2(0f, 1f);
            bshRect.anchorMax = new Vector2(1f, 1f);
            bshRect.pivot = new Vector2(0.5f, 1f);
            bshRect.anchoredPosition = new Vector2(0f, -446f);
            bshRect.sizeDelta = new Vector2(dWidth - 60f, 32f);
            TextMeshProUGUI bshText = buffSectionHeader.GetComponent<TextMeshProUGUI>();
            bshText.fontSize = 22;
            bshText.fontStyle = FontStyles.Bold;
            bshText.text = "<color=#22D3EE>// ACTIVE BUFFS</color>";

            // Subtitle on right: "X / Y ACTIVE ////"
            GameObject subObj = new GameObject("ActiveCountSubtitle", typeof(RectTransform), typeof(TextMeshProUGUI));
            subObj.transform.SetParent(buffSectionHeader.transform, false);
            RectTransform subRect = subObj.GetComponent<RectTransform>();
            subRect.anchorMin = new Vector2(1f, 0f);
            subRect.anchorMax = new Vector2(1f, 1f);
            subRect.pivot = new Vector2(1f, 0.5f);
            subRect.anchoredPosition = Vector2.zero;
            subRect.sizeDelta = new Vector2(240f, 0f);
            activeCountSubtitle = subObj.GetComponent<TextMeshProUGUI>();
            activeCountSubtitle.alignment = TextAlignmentOptions.Right;
            activeCountSubtitle.fontSize = 18;
            activeCountSubtitle.fontStyle = FontStyles.Bold;
            activeCountSubtitle.text = "<color=#64748B>0 ACTIVE</color> <color=#0E7490>////</color>";

            // Scroll View for Active Cards
            GameObject scrollObj = new GameObject("CardsScrollView", typeof(RectTransform), typeof(ScrollRect));
            scrollObj.transform.SetParent(dialog.transform, false);
            RectTransform scrRect = scrollObj.GetComponent<RectTransform>();
            scrRect.anchorMin = new Vector2(0.5f, 1f);
            scrRect.anchorMax = new Vector2(0.5f, 1f);
            scrRect.pivot = new Vector2(0.5f, 1f);
            scrRect.anchoredPosition = new Vector2(0f, -486f);
            scrRect.sizeDelta = new Vector2(dWidth - 40f, dHeight - 540f);

            GameObject viewport = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewport.transform.SetParent(scrollObj.transform, false);
            RectTransform vpRect = viewport.GetComponent<RectTransform>();
            vpRect.anchorMin = Vector2.zero;
            vpRect.anchorMax = Vector2.one;
            vpRect.sizeDelta = Vector2.zero;
            viewport.GetComponent<Image>().color = Color.white;
            viewport.GetComponent<Mask>().showMaskGraphic = false;

            GameObject content = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            content.transform.SetParent(viewport.transform, false);
            RectTransform cRect = content.GetComponent<RectTransform>();
            cRect.anchorMin = new Vector2(0f, 1f);
            cRect.anchorMax = new Vector2(1f, 1f);
            cRect.pivot = new Vector2(0.5f, 1f);
            cRect.sizeDelta = new Vector2(0f, 0f);

            VerticalLayoutGroup vlg = content.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 10f;
            vlg.childControlWidth = true;
            vlg.childControlHeight = false;
            vlg.childForceExpandWidth = true;
            vlg.childForceExpandHeight = false;

            ContentSizeFitter csf = content.GetComponent<ContentSizeFitter>();
            csf.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            ScrollRect sr = scrollObj.GetComponent<ScrollRect>();
            sr.content = cRect;
            sr.viewport = vpRect;
            sr.horizontal = false;
            sr.vertical = true;
            cardListContent = content.transform;

            // Empty State Notice
            GameObject emptyNoticeObj = new GameObject("EmptyStateNotice", typeof(RectTransform), typeof(TextMeshProUGUI));
            emptyNoticeObj.transform.SetParent(scrollObj.transform, false);
            RectTransform enRect = emptyNoticeObj.GetComponent<RectTransform>();
            enRect.anchorMin = Vector2.zero;
            enRect.anchorMax = Vector2.one;
            enRect.sizeDelta = Vector2.zero;
            TextMeshProUGUI enText = emptyNoticeObj.GetComponent<TextMeshProUGUI>();
            enText.alignment = TextAlignmentOptions.Center;
            enText.fontSize = 24;
            enText.color = new Color(0.5f, 0.6f, 0.7f, 0.85f);
            enText.text = "No active upgrade cards equipped.\nEarn cards by clearing waves & defeating bosses!";
            emptyStateNotice = emptyNoticeObj;

            // Bottom Chassis Tech Vents ("///   ///   ///")
            GameObject bottomVents = new GameObject("BottomVents", typeof(RectTransform), typeof(TextMeshProUGUI));
            bottomVents.transform.SetParent(dialog.transform, false);
            RectTransform bvRect = bottomVents.GetComponent<RectTransform>();
            bvRect.anchorMin = new Vector2(0.5f, 0f);
            bvRect.anchorMax = new Vector2(0.5f, 0f);
            bvRect.pivot = new Vector2(0.5f, 0f);
            bvRect.anchoredPosition = new Vector2(0f, 16f);
            bvRect.sizeDelta = new Vector2(400f, 24f);
            TextMeshProUGUI bvt = bottomVents.GetComponent<TextMeshProUGUI>();
            bvt.alignment = TextAlignmentOptions.Center;
            bvt.fontSize = 16;
            bvt.color = new Color(frameCyan.r, frameCyan.g, frameCyan.b, 0.6f);
            bvt.text = "///   ///   ///";

            overlay.SetActive(false);
            return overlay;
        }

        private void CreateSciFiStatCard(Transform parent, string label, Sprite icon, Color accentColor, out TextMeshProUGUI valText, out Transform pipsContainer)
        {
            GameObject card = new GameObject($"StatCard_{label}", typeof(RectTransform), typeof(Image));
            card.transform.SetParent(parent, false);

            var s = settings != null ? settings : BuffModalSettings.Instance;
            Image bg = card.GetComponent<Image>();
            bg.color = s != null ? s.cardTileBackgroundColor : new Color(0.12f, 0.15f, 0.19f, 0.95f);

            Outline cardOutline = card.AddComponent<Outline>();
            cardOutline.effectColor = new Color(accentColor.r * 0.7f, accentColor.g * 0.7f, accentColor.b * 0.7f, 0.65f);
            cardOutline.effectDistance = new Vector2(1f, -1f);

            // Left Icon Box
            GameObject iconBox = new GameObject("IconBox", typeof(RectTransform), typeof(Image));
            iconBox.transform.SetParent(card.transform, false);
            RectTransform ibRect = iconBox.GetComponent<RectTransform>();
            ibRect.anchorMin = new Vector2(0f, 0.5f);
            ibRect.anchorMax = new Vector2(0f, 0.5f);
            ibRect.pivot = new Vector2(0f, 0.5f);
            ibRect.anchoredPosition = new Vector2(10f, 0f);
            ibRect.sizeDelta = new Vector2(56f, 56f);

            Image ibImg = iconBox.GetComponent<Image>();
            ibImg.color = new Color(accentColor.r * 0.15f, accentColor.g * 0.15f, accentColor.b * 0.15f, 0.9f);
            Outline ibOutline = iconBox.AddComponent<Outline>();
            ibOutline.effectColor = accentColor;
            ibOutline.effectDistance = new Vector2(1f, -1f);

            if (icon != null)
            {
                GameObject icObj = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                icObj.transform.SetParent(iconBox.transform, false);
                RectTransform icRect = icObj.GetComponent<RectTransform>();
                icRect.anchorMin = new Vector2(0.5f, 0.5f);
                icRect.anchorMax = new Vector2(0.5f, 0.5f);
                icRect.pivot = new Vector2(0.5f, 0.5f);
                icRect.sizeDelta = new Vector2(36f, 36f);
                Image ic = icObj.GetComponent<Image>();
                ic.sprite = icon;
                ic.color = accentColor;
                ic.preserveAspect = true;
                ic.raycastTarget = false;
            }
            else
            {
                // Procedural fallback glyph
                GameObject symObj = new GameObject("Symbol", typeof(RectTransform), typeof(TextMeshProUGUI));
                symObj.transform.SetParent(iconBox.transform, false);
                RectTransform syRect = symObj.GetComponent<RectTransform>();
                syRect.anchorMin = Vector2.zero;
                syRect.anchorMax = Vector2.one;
                syRect.sizeDelta = Vector2.zero;
                TextMeshProUGUI sym = symObj.GetComponent<TextMeshProUGUI>();
                sym.alignment = TextAlignmentOptions.Center;
                sym.fontSize = 26;
                sym.fontStyle = FontStyles.Bold;
                sym.color = accentColor;
                sym.raycastTarget = false;
                string lblUpper = label.ToUpperInvariant();
                sym.text = lblUpper.Contains("CRIT RATE") ? "%" :
                           lblUpper.Contains("CRIT DMG") ? "x" :
                           lblUpper.Contains("SPEED") ? ">>" :
                           lblUpper.Contains("DAMAGE") ? "▲" :
                           lblUpper.Contains("HP") ? "+" : "#";
            }

            // Stat Label (Small, uppercase)
            GameObject lblObj = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            lblObj.transform.SetParent(card.transform, false);
            RectTransform lRect = lblObj.GetComponent<RectTransform>();
            lRect.anchorMin = new Vector2(0f, 1f);
            lRect.anchorMax = new Vector2(1f, 1f);
            lRect.pivot = new Vector2(0f, 1f);
            lRect.anchoredPosition = new Vector2(76f, -12f);
            lRect.sizeDelta = new Vector2(-80f, 20f);
            TextMeshProUGUI lText = lblObj.GetComponent<TextMeshProUGUI>();
            lText.fontSize = 14;
            lText.fontStyle = FontStyles.Bold;
            lText.color = new Color(0.6f, 0.7f, 0.8f, 0.9f);
            lText.text = label;

            // Stat Value (Big bold readout)
            GameObject valObj = new GameObject("Value", typeof(RectTransform), typeof(TextMeshProUGUI));
            valObj.transform.SetParent(card.transform, false);
            RectTransform vRect = valObj.GetComponent<RectTransform>();
            vRect.anchorMin = new Vector2(0f, 1f);
            vRect.anchorMax = new Vector2(1f, 1f);
            vRect.pivot = new Vector2(0f, 1f);
            vRect.anchoredPosition = new Vector2(76f, -34f);
            vRect.sizeDelta = new Vector2(-80f, 28f);
            valText = valObj.GetComponent<TextMeshProUGUI>();
            valText.fontSize = 20;
            valText.fontStyle = FontStyles.Bold;
            valText.color = Color.white;
            valText.text = "--";

            // Segmented Pips Gauge (Bottom row: "///////")
            GameObject pipsBar = new GameObject("PipsBar", typeof(RectTransform), typeof(HorizontalLayoutGroup));
            pipsBar.transform.SetParent(card.transform, false);
            RectTransform pbRect = pipsBar.GetComponent<RectTransform>();
            pbRect.anchorMin = new Vector2(0f, 0f);
            pbRect.anchorMax = new Vector2(1f, 0f);
            pbRect.pivot = new Vector2(0f, 0f);
            pbRect.anchoredPosition = new Vector2(76f, 12f);
            pbRect.sizeDelta = new Vector2(-90f, 10f);

            HorizontalLayoutGroup hlg = pipsBar.GetComponent<HorizontalLayoutGroup>();
            hlg.spacing = 4f;
            hlg.childControlWidth = true;
            hlg.childControlHeight = true;
            hlg.childForceExpandWidth = true;
            hlg.childForceExpandHeight = true;

            int pipCount = 8;
            for (int i = 0; i < pipCount; i++)
            {
                GameObject pip = new GameObject($"Pip_{i}", typeof(RectTransform), typeof(Image));
                pip.transform.SetParent(pipsBar.transform, false);
                Image pImg = pip.GetComponent<Image>();
                pImg.color = accentColor;
            }

            pipsContainer = pipsBar.transform;
        }
    }
}
