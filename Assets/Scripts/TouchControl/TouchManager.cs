using UnityEngine;
using UnityEngine.InputSystem;
using System;

public class TouchManager : MonoBehaviour
{
    [Header("Touch Settings")]
    [Tooltip("How far across the screen (0.0 to 1.0) the player must swipe. 0.15 = 15% of the screen.")]
    [Range(0.05f, 0.5f)]
    public float swipeScreenPercentage = 0.15f;
    public float holdTimeToOpenWheel = 0.35f;

    [Header("Layer & UX Safeties")]
    public LayerMask playerLayer;
    public LayerMask enemyLayer;
    public bool requireTouchOnBottomHalf = false;

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
    private static float shootBufferTimer = 0f;
    public static Vector2 CurrentTouchPosition { get; private set; }
    public static float TouchHoldTime { get; private set; }

    void Start()
    {
        actualSwipeThreshold = Screen.width * swipeScreenPercentage;
    }

    void Update()
    {
#if ENABLE_INPUT_SYSTEM
        if (Keyboard.current != null && Keyboard.current.tabKey.wasPressedThisFrame)
        {
            if (WeaponWheelToolkitManager.Instance != null)
            {
                if (WeaponWheelToolkitManager.Instance.IsOpen)
                    WeaponWheelToolkitManager.Instance.CloseWheel();
                else
                    WeaponWheelToolkitManager.Instance.OpenWheel();
            }
        }
#endif

        if (Pointer.current == null) return;

        bool pressedThisFrame = Pointer.current.press.wasPressedThisFrame;
        bool isPressed = Pointer.current.press.isPressed;
        bool releasedThisFrame = Pointer.current.press.wasReleasedThisFrame;

        Vector2 position = Pointer.current.position.ReadValue();
        CurrentTouchPosition = position;

        float swipeThreshold = Mathf.Clamp(Screen.width * swipeScreenPercentage, 35f, 160f);

        // Check if the wheel is currently open
        bool isWheelOpen = WeaponWheelToolkitManager.Instance != null && WeaponWheelToolkitManager.Instance.IsOpen;
        bool shouldShoot = false;

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
            }
            else
            {
                isHoldingOnPlayer = false;
                // Instant firing responsiveness on tap/click!
                shouldShoot = true;
            }
        }
        // --- 2. TOUCH HELD / MOVED ---
        else if (isPressed)
        {
            TouchHoldTime = Time.unscaledTime - touchStartTime;
            Vector2 currentDelta = position - startTouchPos;

            if (isWheelOpen)
            {
                // Wheel is open; do nothing
            }
            else
            {
                // Grace window: if touch began near player and settled within 0.25s, acquire player hold
                if (!isSwiping && !isHoldingOnPlayer && TouchHoldTime < 0.25f && currentDelta.magnitude < 35f)
                {
                    if (DidTouchPlayer(position))
                    {
                        isHoldingOnPlayer = true;
                    }
                }

                // Check if the gesture moved far enough to be a swipe (even if the touch started over the player model)
                if (!isSwiping && TouchHoldTime <= 0.45f && currentDelta.magnitude > swipeThreshold)
                {
                    isSwiping = true;
                    isHoldingOnPlayer = false; // Cancel holding on player since player swiped
                    DetectSwipeDirection(currentDelta);
                }
                else if (isHoldingOnPlayer)
                {
                    if (TouchHoldTime >= holdTimeToOpenWheel)
                    {
                        if (WeaponWheelToolkitManager.Instance != null)
                        {
                            WeaponWheelToolkitManager.Instance.OpenWheel();
                        }
                        isSwiping = true;
                        isHoldingOnPlayer = false;
                    }
                }

                // If not swiping and not holding to open wheel, continuous shoot/aim
                if (!isSwiping && !isHoldingOnPlayer)
                {
                    shouldShoot = true;
                }
            }
        }
        // --- 3. TOUCH ENDED ---
        else if (releasedThisFrame)
        {
            isHoldingOnPlayer = false;

            if (isWheelOpen)
            {
                WeaponWheelToolkitManager.Instance.CloseWheel();
            }
        }

        IsShooting = shouldShoot && !isSwiping;
    }

    private bool DidTouchPlayer(Vector2 screenPos)
    {
        if (Camera.main == null) return false;

        if (requireTouchOnBottomHalf && screenPos.y > Screen.height * 0.75f) return false;

        Ray ray = Camera.main.ScreenPointToRay(screenPos);
        RaycastHit[] hits = Physics.RaycastAll(ray, 100f, ~0, QueryTriggerInteraction.Collide);

        bool hitPlayer = false;
        bool hitEnemy = false;

        foreach (RaycastHit hit in hits)
        {
            if ((enemyLayer.value & (1 << hit.collider.gameObject.layer)) > 0)
            {
                hitEnemy = true;
            }
            else if (IsPlayerCollider(hit.collider))
            {
                hitPlayer = true;
            }
        }

        // Secondary check: SphereCast with generous radius to forgive small finger touch offsets
        if (!hitPlayer && !hitEnemy)
        {
            RaycastHit[] sphereHits = Physics.SphereCastAll(ray, 0.45f, 100f, ~0, QueryTriggerInteraction.Collide);
            foreach (RaycastHit sHit in sphereHits)
            {
                if ((enemyLayer.value & (1 << sHit.collider.gameObject.layer)) > 0)
                {
                    hitEnemy = true;
                }
                else if (IsPlayerCollider(sHit.collider))
                {
                    hitPlayer = true;
                }
            }
        }

        // Tertiary check: Screen-space bounding proximity to Player
        if (!hitPlayer && !hitEnemy && Player.Control.PlayerController.Instance != null && Camera.main != null)
        {
            Vector3 playerCenter = Player.Control.PlayerController.Instance.transform.position + Vector3.up * 0.9f;
            Vector3 screenPoint = Camera.main.WorldToScreenPoint(playerCenter);
            if (screenPoint.z > 0)
            {
                Vector3 headPoint = Camera.main.WorldToScreenPoint(Player.Control.PlayerController.Instance.transform.position + Vector3.up * 1.8f);
                float playerPixelHeight = Mathf.Abs(headPoint.y - screenPoint.y) * 2f;
                float touchRadius = Mathf.Max(playerPixelHeight * 0.65f, 85f);

                if (Vector2.Distance(screenPos, (Vector2)screenPoint) <= touchRadius)
                {
                    hitPlayer = true;
                }
            }
        }

        if (hitEnemy) return false;
        return hitPlayer;
    }

    private bool IsPlayerCollider(Collider col)
    {
        if (col == null) return false;
        if ((playerLayer.value & (1 << col.gameObject.layer)) > 0) return true;
        if (col.CompareTag("Player")) return true;
        if (col.GetComponentInParent<Player.Control.PlayerController>() != null) return true;
        return false;
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