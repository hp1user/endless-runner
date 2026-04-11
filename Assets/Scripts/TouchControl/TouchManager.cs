using UnityEngine;
using System;

public class TouchManager : MonoBehaviour
{
    [Header("Touch Settings")]
    public float swipeThreshold = 40f;

    private Vector2 startTouchPos;
    private bool isSwiping;
    private bool isSimulatingTouch; // Tracks if the mouse is being held down

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
        // 1. MOBILE DEVICE INPUT
        if (Input.touchCount > 0)
        {
            Touch touch = Input.GetTouch(0);

            switch (touch.phase)
            {
                case TouchPhase.Began:
                    HandleTouchBegan(touch.position);
                    break;
                case TouchPhase.Moved:
                case TouchPhase.Stationary:
                    HandleTouchMoved(touch.position);
                    break;
                case TouchPhase.Ended:
                case TouchPhase.Canceled:
                    HandleTouchEnded();
                    break;
            }
        }
        // 2. UNITY EDITOR / MOUSE FALLBACK
        else
        {
            if (Input.GetMouseButtonDown(0))
            {
                isSimulatingTouch = true;
                HandleTouchBegan(Input.mousePosition);
            }
            else if (Input.GetMouseButton(0) && isSimulatingTouch)
            {
                HandleTouchMoved(Input.mousePosition);
            }
            else if (Input.GetMouseButtonUp(0) && isSimulatingTouch)
            {
                isSimulatingTouch = false;
                HandleTouchEnded();
            }
            else if (!isSimulatingTouch)
            {
                // Ensure shooting is false if nothing is being pressed
                IsShooting = false;
            }
        }
    }

    // --- Core Logic Extracted for Reusability ---

    private void HandleTouchBegan(Vector2 position)
    {
        CurrentTouchPosition = position;
        startTouchPos = position;
        isSwiping = false;
        IsShooting = true; // Assume they are shooting until it becomes a swipe
    }

    private void HandleTouchMoved(Vector2 position)
    {
        CurrentTouchPosition = position;
        Vector2 currentDelta = position - startTouchPos;

        // If they moved the mouse/finger far enough, it's a swipe, not a shot
        if (!isSwiping && currentDelta.magnitude > swipeThreshold)
        {
            isSwiping = true;
            IsShooting = false;
            DetectSwipeDirection(currentDelta);
        }
    }

    private void HandleTouchEnded()
    {
        IsShooting = false;
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