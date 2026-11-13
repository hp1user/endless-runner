using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Responsive layout controller for Upgrade Cards (uGUI / Legacy UI).
/// Keeps the VerticalLayoutGroup enabled at all times (cards stacked vertically in both
/// portrait and landscape), and dynamically calculates scale, spacing, and the Confirm
/// button position so cards never overflow or overlap on horizontal (landscape) screens.
/// </summary>
[ExecuteAlways]
[DisallowMultipleComponent]
public class UpgradeCardResponsiveLayout : MonoBehaviour
{
    [Header("UI Element References")]
    [Tooltip("The container holding the instantiated upgrade cards.")]
    public RectTransform cardContainer;

    [Tooltip("The confirm selection button.")]
    public RectTransform confirmButton;

    [Header("Portrait Layout Settings")]
    [Tooltip("Spacing between cards in portrait.")]
    public float portraitSpacing = 36f;
    [Tooltip("Max card scale in portrait.")]
    public float maxPortraitScale = 1.0f;
    [Tooltip("Min card scale in portrait.")]
    public float minPortraitScale = 0.60f;
    [Tooltip("Vertical center offset of card container in portrait.")]
    public float portraitContainerOffsetY = 60f;
    [Tooltip("Vertical gap between cards bottom and confirm button in portrait.")]
    public float portraitConfirmGap = 35f;

    [Header("Landscape Layout Settings")]
    [Tooltip("Spacing between cards in landscape (keeps VerticalLayoutGroup on with compact spacing).")]
    public float landscapeSpacing = 18f;
    [Tooltip("Max card scale in landscape.")]
    public float maxLandscapeScale = 0.62f;
    [Tooltip("Min card scale in landscape.")]
    public float minLandscapeScale = 0.38f;
    [Tooltip("Vertical center offset of card container in landscape.")]
    public float landscapeContainerOffsetY = 30f;
    [Tooltip("Vertical gap between cards bottom and confirm button in landscape.")]
    public float landscapeConfirmGap = 16f;

    [Header("Sorting Order Settings")]
    [Tooltip("Ensures CardsPanel renders in front of HUD and other UI panels.")]
    public int sortingOrder = 500;

    public const float BASE_CARD_WIDTH = 980f;
    public const float BASE_CARD_HEIGHT = 460f;

    private RectTransform panelRect;
    private Canvas parentCanvas;
    private Canvas subCanvas;
    private VerticalLayoutGroup vLayout;
    private ContentSizeFitter sizeFitter;

    private float lastWidth = -1f;
    private float lastHeight = -1f;
    private int lastChildCount = -1;
    private bool isInitialized = false;

    private void Awake()
    {
        InitializeReferences();
    }

    private void OnEnable()
    {
        InitializeReferences();
        EnsureSubCanvas();
        UpdateLayout();
        StartCoroutine(DeferredLayoutUpdate());
    }

    private void OnRectTransformDimensionsChange()
    {
        UpdateLayout();
    }

    private void Update()
    {
        if (panelRect == null) panelRect = GetComponent<RectTransform>();
        if (panelRect == null || cardContainer == null) return;

        float w = panelRect.rect.width;
        float h = panelRect.rect.height;
        int count = GetActiveCardCount();

        if (Mathf.Abs(w - lastWidth) > 0.5f || Mathf.Abs(h - lastHeight) > 0.5f || count != lastChildCount)
        {
            lastWidth = w;
            lastHeight = h;
            lastChildCount = count;
            UpdateLayout();
        }
    }

    public void InitializeReferences()
    {
        if (panelRect == null) panelRect = GetComponent<RectTransform>();
        if (parentCanvas == null) parentCanvas = GetComponentInParent<Canvas>();

        EnsureSubCanvas();

        if (cardContainer == null)
        {
            Transform t = transform.Find("CardContainer");
            if (t != null) cardContainer = t as RectTransform;
        }

        if (confirmButton == null)
        {
            Transform t = transform.Find("Confirm");
            if (t != null) confirmButton = t as RectTransform;
        }

        EnsureLayoutComponents();
        isInitialized = true;
    }

    private void EnsureSubCanvas()
    {
        subCanvas = GetComponent<Canvas>();
        if (subCanvas == null) subCanvas = gameObject.AddComponent<Canvas>();
        subCanvas.overrideSorting = true;
        subCanvas.sortingOrder = sortingOrder;

        GraphicRaycaster raycaster = GetComponent<GraphicRaycaster>();
        if (raycaster == null) raycaster = gameObject.AddComponent<GraphicRaycaster>();

        transform.SetAsLastSibling();
    }

    private void EnsureLayoutComponents()
    {
        if (cardContainer == null) return;

        vLayout = cardContainer.GetComponent<VerticalLayoutGroup>();
        if (vLayout == null) vLayout = cardContainer.gameObject.AddComponent<VerticalLayoutGroup>();
        vLayout.enabled = true; // Never turn off VerticalLayoutGroup

        // If a HorizontalLayoutGroup exists, disable it so it never conflicts
        HorizontalLayoutGroup hLayout = cardContainer.GetComponent<HorizontalLayoutGroup>();
        if (hLayout != null && hLayout.enabled)
        {
            hLayout.enabled = false;
        }

        sizeFitter = cardContainer.GetComponent<ContentSizeFitter>();
    }

    public int GetActiveCardCount()
    {
        if (cardContainer == null) return 0;
        int count = 0;
        for (int i = 0; i < cardContainer.childCount; i++)
        {
            Transform child = cardContainer.GetChild(i);
            if (child != null && child.gameObject.activeSelf) count++;
        }
        return count;
    }

    /// <summary>
    /// Recalculates card vertical spacing, scale, and confirm button position
    /// based on the current screen/canvas orientation.
    /// VerticalLayoutGroup remains active in both portrait and landscape.
    /// </summary>
    public void UpdateLayout()
    {
        if (!isInitialized) InitializeReferences();
        if (cardContainer == null || panelRect == null) return;

        Rect canvasRect = (parentCanvas != null) ? parentCanvas.GetComponent<RectTransform>().rect : panelRect.rect;
        float canvasWidth = canvasRect.width > 0 ? canvasRect.width : panelRect.rect.width;
        float canvasHeight = canvasRect.height > 0 ? canvasRect.height : panelRect.rect.height;

        if (canvasWidth <= 0 || canvasHeight <= 0) return;

        bool isLandscape = canvasWidth > canvasHeight;
        int cardCount = GetActiveCardCount();
        if (cardCount <= 0) cardCount = 3;

        // Ensure VerticalLayoutGroup is always enabled
        if (vLayout != null)
        {
            vLayout.enabled = true;
            vLayout.spacing = isLandscape ? landscapeSpacing : portraitSpacing;
            vLayout.childAlignment = TextAnchor.MiddleCenter;
            vLayout.childControlWidth = false;
            vLayout.childControlHeight = false;
            vLayout.childForceExpandWidth = false;
            vLayout.childForceExpandHeight = false;
            vLayout.padding = new RectOffset(0, 0, 0, 0);
        }

        if (sizeFitter != null)
        {
            sizeFitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;
            sizeFitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
        }

        float currentSpacing = isLandscape ? landscapeSpacing : portraitSpacing;
        float totalUnscaledHeight = cardCount * BASE_CARD_HEIGHT + (cardCount - 1) * currentSpacing;
        float totalUnscaledWidth = BASE_CARD_WIDTH;

        cardContainer.sizeDelta = new Vector2(totalUnscaledWidth, totalUnscaledHeight);
        cardContainer.anchorMin = new Vector2(0.5f, 0.5f);
        cardContainer.anchorMax = new Vector2(0.5f, 0.5f);
        cardContainer.pivot = new Vector2(0.5f, 0.5f);

        Vector2 confirmSize = isLandscape ? new Vector2(460f, 75f) : new Vector2(520f, 90f);
        float confirmGap = isLandscape ? landscapeConfirmGap : portraitConfirmGap;
        float offsetY = isLandscape ? landscapeContainerOffsetY : portraitContainerOffsetY;

        // Calculate scale to fit available screen height (reserving space for Confirm button + padding)
        float availableHeight = canvasHeight - confirmSize.y - confirmGap - (isLandscape ? 60f : 100f);
        float availableWidth = canvasWidth - (isLandscape ? 120f : 40f);

        float scaleY = availableHeight / totalUnscaledHeight;
        float scaleX = availableWidth / totalUnscaledWidth;

        float minScale = isLandscape ? minLandscapeScale : minPortraitScale;
        float maxScale = isLandscape ? maxLandscapeScale : maxPortraitScale;
        float scale = Mathf.Clamp(Mathf.Min(scaleX, scaleY), minScale, maxScale);

        cardContainer.localScale = new Vector3(scale, scale, 1f);
        cardContainer.anchoredPosition = new Vector2(0f, offsetY * scale);

        // Position Confirm button right below the cards with clean spacing and zero overlap
        if (confirmButton != null)
        {
            confirmButton.anchorMin = new Vector2(0.5f, 0.5f);
            confirmButton.anchorMax = new Vector2(0.5f, 0.5f);
            confirmButton.pivot = new Vector2(0.5f, 0.5f);
            confirmButton.sizeDelta = confirmSize;
            confirmButton.localScale = Vector3.one;

            float cardsBottomY = cardContainer.anchoredPosition.y - (totalUnscaledHeight * scale * 0.5f);
            confirmButton.anchoredPosition = new Vector2(0f, cardsBottomY - confirmGap - (confirmSize.y * 0.5f));
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(cardContainer);
    }

    private IEnumerator DeferredLayoutUpdate()
    {
        yield return null;
        UpdateLayout();
    }
}
