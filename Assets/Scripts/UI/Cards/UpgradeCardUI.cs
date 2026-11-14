using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.EventSystems;
using TMPro;

public class UpgradeCardUI : MonoBehaviour, IBeginDragHandler, IDragHandler, IEndDragHandler, IPointerClickHandler
{
    [Header("Hierarchy References")]
    public Image backgroundImage;
    public TextMeshProUGUI titleText;
    public Image mainIconImage;
    public TextMeshProUGUI descriptionText;

    [Tooltip("Drag the text object that displays the number (e.g. '+20') here")]
    public TextMeshProUGUI valueText;

    [Header("Swipe to Select / Confirm Settings")]
    [Tooltip("Horizontal distance in local canvas units required to confirm selection on release.")]
    public float swipeThreshold = 130f;
    [Tooltip("Minimum flick velocity in pixels/second to trigger confirmation on quick swipes.")]
    public float minFlickVelocity = 600f;
    [Tooltip("Maximum tilt rotation angle in degrees when swiping left or right.")]
    public float maxTiltAngle = 12f;
    [Tooltip("Duration in seconds of the fly-off animation upon confirmation.")]
    public float flyOffDuration = 0.18f;
    [Tooltip("Duration in seconds of the snap-back animation when swipe is cancelled.")]
    public float returnDuration = 0.15f;

    // Static lock to prevent multiple cards confirming simultaneously
    public static bool isAnyCardConfirmed = false;

    public static void ResetSwipeState()
    {
        isAnyCardConfirmed = false;
    }

    private UpgradeCard myCardData;
    private Button myButton;
    private RectTransform rectTransform;
    private CanvasGroup canvasGroup;
    private RectTransform parentRect;

    private Vector2 startLocalPointerPos;
    private Vector2 startScreenPos;
    private Vector3 restLocalPos;
    private float currentDeltaX;
    private bool isDragging = false;
    private float dragStartTime;
    private bool isSelected = false;
    private Coroutine activeCoroutine;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();
        canvasGroup = GetComponent<CanvasGroup>();
        if (canvasGroup == null)
        {
            canvasGroup = gameObject.AddComponent<CanvasGroup>();
        }

        myButton = GetComponent<Button>();
        if (myButton != null)
        {
            myButton.onClick.AddListener(OnCardClicked);
        }
    }

    private void OnEnable()
    {
        if (rectTransform == null) rectTransform = GetComponent<RectTransform>();
        if (canvasGroup == null) canvasGroup = GetComponent<CanvasGroup>();

        if (canvasGroup != null) canvasGroup.alpha = 1f;
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = Vector3.one;

        isDragging = false;
        currentDeltaX = 0f;
        restLocalPos = new Vector3(0f, rectTransform.localPosition.y, 0f);
    }

    public void Initialize(UpgradeCard cardData)
    {
        myCardData = cardData;

        SetSelected(false);

        // Set the standard text and icon
        if (titleText != null) titleText.text = cardData.cardName;
        if (descriptionText != null) descriptionText.text = cardData.description;
        if (mainIconImage != null) mainIconImage.sprite = cardData.cardIcon;

        // Format the Stat Value Text
        if (valueText != null)
        {
            valueText.text = "";
            if (cardData.effects != null && cardData.effects.Count > 0)
            {
                foreach (var effect in cardData.effects)
                {
                    string suffix = "";
                    if (effect.upgradeType == UpgradeType.DamageBoost) suffix = "%";
                    else if (effect.upgradeType == UpgradeType.MaxHealth) suffix = " HP";
                    else if (effect.upgradeType == UpgradeType.SpeedBoost) suffix = " SPD";

                    valueText.text += "+" + effect.upgradeValue.ToString() + suffix + "\n";
                }
                valueText.text = valueText.text.TrimEnd('\n');
            }
        }

        restLocalPos = new Vector3(0f, rectTransform.localPosition.y, 0f);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        // If the pointer dragged significantly, do not treat as a tap
        if (Mathf.Abs(currentDeltaX) > 15f || isAnyCardConfirmed) return;

        OnCardClicked();
    }

    private void OnCardClicked()
    {
        if (UpgradeManager.Instance != null && myCardData != null && !isAnyCardConfirmed)
        {
            UpgradeManager.Instance.PreviewUpgrade(this, myCardData);

            // Playful nudge animation to hint that the card can be swiped
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(NudgeHint());
        }
    }

    public void OnBeginDrag(PointerEventData eventData)
    {
        if (isAnyCardConfirmed) return;

        TouchManager.IsInputBlocked = true;
        isDragging = true;
        if (activeCoroutine != null) StopCoroutine(activeCoroutine);

        dragStartTime = Time.realtimeSinceStartup;
        startScreenPos = eventData.position;

        parentRect = transform.parent as RectTransform;
        if (parentRect != null)
        {
            RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out startLocalPointerPos);
        }

        restLocalPos = new Vector3(0f, rectTransform.localPosition.y, 0f);

        // Preview upgrade immediately when drag begins
        if (UpgradeManager.Instance != null && myCardData != null)
        {
            UpgradeManager.Instance.PreviewUpgrade(this, myCardData);
        }
    }

    public void OnDrag(PointerEventData eventData)
    {
        if (!isDragging || isAnyCardConfirmed || parentRect == null) return;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(parentRect, eventData.position, eventData.pressEventCamera, out Vector2 currentLocalPointerPos);
        currentDeltaX = currentLocalPointerPos.x - startLocalPointerPos.x;

        // Update horizontal position (leaving vertical position anchored to the layout)
        rectTransform.localPosition = new Vector3(restLocalPos.x + currentDeltaX, rectTransform.localPosition.y, 0f);

        // Tilt rotation based on drag offset (negative delta gives natural swipe tilt)
        float tilt = Mathf.Clamp(-currentDeltaX * 0.035f, -maxTiltAngle, maxTiltAngle);
        rectTransform.localRotation = Quaternion.Euler(0f, 0f, tilt);

        // Visual feedback: subtly scale card up when approaching or exceeding threshold
        float progress = Mathf.Clamp01(Mathf.Abs(currentDeltaX) / swipeThreshold);
        float targetScale = Mathf.Lerp(1.05f, 1.10f, progress);
        rectTransform.localScale = new Vector3(targetScale, targetScale, 1f);
    }

    public void OnEndDrag(PointerEventData eventData)
    {
        if (!isDragging || isAnyCardConfirmed) return;
        isDragging = false;

        float duration = Mathf.Max(0.001f, Time.realtimeSinceStartup - dragStartTime);
        float flickVelocity = (eventData.position.x - startScreenPos.x) / duration;

        // Swipe confirmation condition: dragged past threshold OR fast flick in either direction
        bool isSwipeConfirmed = Mathf.Abs(currentDeltaX) >= swipeThreshold ||
                                (Mathf.Abs(flickVelocity) >= minFlickVelocity && Mathf.Abs(currentDeltaX) > 40f);

        if (isSwipeConfirmed)
        {
            isAnyCardConfirmed = true;
            float direction = currentDeltaX >= 0f ? 1f : -1f;

            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(FlyOffAndConfirm(direction));
        }
        else
        {
            if (activeCoroutine != null) StopCoroutine(activeCoroutine);
            activeCoroutine = StartCoroutine(ReturnToRest());
        }
    }

    private IEnumerator FlyOffAndConfirm(float direction)
    {
        float elapsed = 0f;
        Vector3 startPos = rectTransform.localPosition;
        Quaternion startRot = rectTransform.localRotation;
        float targetX = startPos.x + direction * 2200f;
        Quaternion targetRot = Quaternion.Euler(0f, 0f, direction * -25f);

        while (elapsed < flyOffDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / flyOffDuration);
            float ease = t * t; // Smooth acceleration off screen

            rectTransform.localPosition = new Vector3(Mathf.Lerp(startPos.x, targetX, ease), rectTransform.localPosition.y, 0f);
            rectTransform.localRotation = Quaternion.Lerp(startRot, targetRot, ease);

            if (canvasGroup != null)
            {
                canvasGroup.alpha = Mathf.Lerp(1f, 0f, ease);
            }

            yield return null;
        }

        // Apply and confirm upgrade
        if (UpgradeManager.Instance != null && myCardData != null)
        {
            UpgradeManager.Instance.ConfirmUpgrade(myCardData);
        }

        activeCoroutine = null;
    }

    private IEnumerator ReturnToRest()
    {
        float elapsed = 0f;
        Vector3 startPos = rectTransform.localPosition;
        Quaternion startRot = rectTransform.localRotation;
        Vector3 startScale = rectTransform.localScale;
        Vector3 targetScale = isSelected ? new Vector3(1.05f, 1.05f, 1.05f) : Vector3.one;

        while (elapsed < returnDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / returnDuration);
            float ease = 1f - Mathf.Pow(1f - t, 3f); // Ease out cubic spring

            rectTransform.localPosition = new Vector3(Mathf.Lerp(startPos.x, restLocalPos.x, ease), rectTransform.localPosition.y, 0f);
            rectTransform.localRotation = Quaternion.Lerp(startRot, Quaternion.identity, ease);
            rectTransform.localScale = Vector3.Lerp(startScale, targetScale, ease);

            yield return null;
        }

        rectTransform.localPosition = new Vector3(restLocalPos.x, rectTransform.localPosition.y, 0f);
        rectTransform.localRotation = Quaternion.identity;
        rectTransform.localScale = targetScale;
        currentDeltaX = 0f;
        activeCoroutine = null;
    }

    private IEnumerator NudgeHint()
    {
        float elapsed = 0f;
        float duration = 0.22f;
        float amplitude = 20f;

        while (elapsed < duration)
        {
            if (isDragging || isAnyCardConfirmed) yield break;

            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / duration;
            // Quick sinusoidal nudge: right then left then settle back
            float offset = Mathf.Sin(t * Mathf.PI * 2f) * amplitude * (1f - t);

            rectTransform.localPosition = new Vector3(restLocalPos.x + offset, rectTransform.localPosition.y, 0f);
            rectTransform.localRotation = Quaternion.Euler(0f, 0f, -offset * 0.2f);

            yield return null;
        }

        rectTransform.localPosition = new Vector3(restLocalPos.x, rectTransform.localPosition.y, 0f);
        rectTransform.localRotation = Quaternion.identity;
        activeCoroutine = null;
    }

    public void SetSelected(bool selected)
    {
        isSelected = selected;
        UpdateBackgroundVisuals(selected);

        if (!isDragging && activeCoroutine == null)
        {
            transform.localScale = selected ? new Vector3(1.05f, 1.05f, 1.05f) : Vector3.one;
        }
    }

    private void UpdateBackgroundVisuals(bool selected)
    {
        if (myCardData == null || backgroundImage == null) return;

        if (selected)
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
