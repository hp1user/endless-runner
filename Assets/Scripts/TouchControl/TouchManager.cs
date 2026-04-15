using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class TouchManager : MonoBehaviour
{
    [Header("Touch Settings")]
    [Tooltip("How far across the screen (0.0 to 1.0) the player must swipe. 0.15 = 15% of the screen.")]
    [Range(0.05f, 0.5f)]
    public float swipeScreenPercentage = 0.15f;
    public float holdTimeToOpenWheel = 0.5f;

    [Header("Layer & UX Safeties")]
    public LayerMask playerLayer;
    public LayerMask enemyLayer;
    public bool requireTouchOnBottomHalf = true;

    private Vector2 startTouchPos;
    private bool isSwiping;
    private bool isHoldingOnPlayer;
    private float touchStartTime;
    private float actualSwipeThreshold;

    // Events
    public static event Action OnSwipeLeft;
    public static event Action OnSwipeRight;
    public static event Action OnSwipeUp; // Reload Event

    public static bool IsShooting { get; private set; }
    public static Vector2 CurrentTouchPosition { get; private set; }
    public static float TouchHoldTime { get; private set; }

    void Start()
    {
        actualSwipeThreshold = Screen.width * swipeScreenPercentage;
    }

    void Update()
    {
        if (Pointer.current == null) return;

        bool pressedThisFrame = Pointer.current.press.wasPressedThisFrame;
        bool isPressed = Pointer.current.press.isPressed;
        bool releasedThisFrame = Pointer.current.press.wasReleasedThisFrame;

        Vector2 position = Pointer.current.position.ReadValue();
        CurrentTouchPosition = position;

        // Check if the wheel is currently open
        bool isWheelOpen = WeaponWheelManager.Instance != null && WeaponWheelManager.Instance.wheelUI.activeSelf;

        // --- 1. TOUCH BEGAN ---
        if (pressedThisFrame)
        {
            startTouchPos = position;
            isSwiping = false;
            touchStartTime = Time.unscaledTime;
            TouchHoldTime = 0f;

            if (!isWheelOpen && DidTouchPlayer(position))
            {
                isHoldingOnPlayer = true;
                IsShooting = false;
            }
            else
            {
                isHoldingOnPlayer = false;
                IsShooting = true;
            }
        }
        // --- 2. TOUCH HELD / MOVED ---
        else if (isPressed)
        {
            TouchHoldTime = Time.unscaledTime - touchStartTime;
            Vector2 currentDelta = position - startTouchPos;

            if (isWheelOpen)
            {
                // DO NOTHING. The WeaponWheelManager is reading the finger to select a weapon.
                // We block swiping and shooting here.
            }
            else
            {
                // FIX: Check for swipe FIRST! A swipe cancels a "Hold" intent.
                if (!isSwiping && currentDelta.magnitude > actualSwipeThreshold)
                {
                    isSwiping = true;
                    isHoldingOnPlayer = false; // Cancel the wheel timer
                    DetectSwipeDirection(currentDelta);
                }
                // If they haven't swiped, and are holding the player, check the timer
                else if (isHoldingOnPlayer && !isSwiping)
                {
                    if (Time.unscaledTime - touchStartTime >= holdTimeToOpenWheel)
                    {
                        WeaponWheelManager.Instance.OpenWheel();
                        isSwiping = true; // Lock out swiping just in case
                    }
                }
            }
        }
        // --- 3. TOUCH ENDED ---
        else if (releasedThisFrame)
        {
            IsShooting = false;
            isHoldingOnPlayer = false;

            if (isWheelOpen)
            {
                WeaponWheelManager.Instance.CloseWheel();
            }
        }
        else if (!isPressed)
        {
            IsShooting = false;
        }
    }

    private bool DidTouchPlayer(Vector2 screenPos)
    {
        if (Camera.main == null) return false;

        if (requireTouchOnBottomHalf && screenPos.y > Screen.height / 2f) return false;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f);

        bool hitPlayer = false;
        bool hitEnemy = false;

        foreach (RaycastHit hit in hits)
        {
            if ((enemyLayer.value & (1 << hit.collider.gameObject.layer)) > 0) hitEnemy = true;
            else if ((playerLayer.value & (1 << hit.collider.gameObject.layer)) > 0) hitPlayer = true;
        }

        if (hitEnemy) return false;
        return hitPlayer;
    }

    private void DetectSwipeDirection(Vector2 delta)
    {
        // Horizontal Swipe
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            if (delta.x > 0) OnSwipeRight?.Invoke();
            else OnSwipeLeft?.Invoke();
        }
        // Vertical Swipe
        else
        {
            // Only trigger if swiping UP (delta.y > 0). Ignores swiping down entirely.
            if (delta.y > 0) OnSwipeUp?.Invoke();
        }
    }
}