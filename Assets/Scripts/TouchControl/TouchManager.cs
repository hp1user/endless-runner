using UnityEngine;
using UnityEngine.InputSystem; // REQUIRED for the new system
using System;

public class TouchManager : MonoBehaviour
{
    [Header("Touch Settings")]
    public float swipeThreshold = 40f;

    private Vector2 startTouchPos;
    private bool isSwiping;

    // Events for Swiping
    public static event Action OnSwipeLeft;
    public static event Action OnSwipeRight;
    public static event Action OnSwipeUp;
    public static event Action OnSwipeDown;

    // States for your existing PlayerController / AimController Update loops
    public static bool IsShooting { get; private set; }
    public static Vector2 CurrentTouchPosition { get; private set; }

    void Update()
    {
        // 1. Abort if no pointing device is detected (Mouse, Touchscreen, Pen)
        if (Pointer.current == null) return;

        // 2. Read the unified Pointer states
        bool pressedThisFrame = Pointer.current.press.wasPressedThisFrame;
        bool isPressed = Pointer.current.press.isPressed;
        bool releasedThisFrame = Pointer.current.press.wasReleasedThisFrame;

        Vector2 position = Pointer.current.position.ReadValue();
        CurrentTouchPosition = position;

        // 3. Logic Routing
        if (pressedThisFrame)
        {
            startTouchPos = position;
            isSwiping = false;
            IsShooting = true; // Assume they are shooting until it becomes a swipe
        }
        else if (isPressed)
        {
            Vector2 currentDelta = position - startTouchPos;

            // If they moved the pointer far enough, it's a swipe, not a shot
            if (!isSwiping && currentDelta.magnitude > swipeThreshold)
            {
                isSwiping = true;
                IsShooting = false; // Cancel shooting, they are swiping
                DetectSwipeDirection(currentDelta);
            }
        }
        else if (releasedThisFrame)
        {
            IsShooting = false;
        }
        else if (!isPressed)
        {
            // Failsafe to ensure shooting turns off when nothing is touching
            IsShooting = false;
        }
    }

    private void DetectSwipeDirection(Vector2 delta)
    {
        if (Mathf.Abs(delta.x) > Mathf.Abs(delta.y))
        {
            if (delta.x > 0) OnSwipeRight?.Invoke();
            else OnSwipeLeft?.Invoke();
        }
        else
        {
            if (delta.y > 0) OnSwipeUp?.Invoke();
            else OnSwipeDown?.Invoke();
        }
    }
}