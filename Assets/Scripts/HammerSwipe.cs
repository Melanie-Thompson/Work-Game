using UnityEngine;
using UnityEngine.InputSystem;

public class HammerSwipe : MonoBehaviour
{
    [Header("Animation Settings")]
    public float startAngle = -90f;     // Starting rotation angle (hammer head up, vertical)
    public float endAngle = 0f;         // Ending rotation angle (hammer horizontal, head down on rabbit)
    public float swipeDownTime = 0.3f;  // How fast it swipes down
    public float holdTime = 0.2f;       // How long to hold at bottom
    public float swipeUpTime = 0.5f;    // How fast it returns up

    [Header("Swipe Detection")]
    public bool isLeftHammer = true;    // True for left hammer, false for right
    public float swipeThreshold = 50f; // Minimum distance for swipe
    public float swipeTimeWindow = 0.5f; // Max time for swipe

    private Vector3 originalPosition;
    private Quaternion originalRotation;
    private float timer = 0f;
    private enum State { WaitingAtTop, SwipingDown, Holding, SwipingUp }
    private State currentState = State.WaitingAtTop;

    private Vector2 swipeStartPosition;
    private float swipeStartTime;
    private bool isTracking = false;

    void Start()
    {
        // Save the original position and rotation from the scene
        originalPosition = transform.position;
        originalRotation = transform.rotation;
        transform.rotation = originalRotation * Quaternion.Euler(startAngle, 0, 0);
    }

    void Update()
    {
        timer += Time.deltaTime;

        HandleInput();

        switch (currentState)
        {
            case State.WaitingAtTop:
                // Stay at top until triggered by swipe
                break;

            case State.SwipingDown:
                float downProgress = timer / swipeDownTime;
                if (downProgress >= 1f)
                {
                    downProgress = 1f;
                    timer = 0f;
                    currentState = State.Holding;
                }

                // Ease-in for dramatic effect
                float easeDown = downProgress * downProgress;
                float currentAngle = Mathf.Lerp(startAngle, endAngle, easeDown);
                transform.rotation = originalRotation * Quaternion.Euler(currentAngle, 0, 0);
                break;

            case State.Holding:
                if (timer >= holdTime)
                {
                    timer = 0f;
                    currentState = State.SwipingUp;
                }
                break;

            case State.SwipingUp:
                float upProgress = timer / swipeUpTime;
                if (upProgress >= 1f)
                {
                    upProgress = 1f;
                    timer = 0f;
                    currentState = State.WaitingAtTop;
                }

                // Ease-out for smooth return
                float easeUp = 1f - (1f - upProgress) * (1f - upProgress);
                float currentAngleUp = Mathf.Lerp(endAngle, startAngle, easeUp);
                transform.rotation = originalRotation * Quaternion.Euler(currentAngleUp, 0, 0);
                break;
        }
    }

    void HandleInput()
    {
        var touchscreen = Touchscreen.current;
        var mouse = Mouse.current;

        // Handle touch input
        if (touchscreen != null)
        {
            var touch = touchscreen.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                Vector2 touchPos = touch.position.ReadValue();
                if (IsOnCorrectSide(touchPos))
                {
                    swipeStartPosition = touchPos;
                    swipeStartTime = Time.time;
                    isTracking = true;
                }
            }

            if (touch.press.wasReleasedThisFrame && isTracking)
            {
                Vector2 touchEnd = touch.position.ReadValue();
                CheckSwipe(touchEnd);
                isTracking = false;
            }
        }
        // Handle mouse input
        else if (mouse != null)
        {
            if (mouse.leftButton.wasPressedThisFrame)
            {
                Vector2 mousePos = mouse.position.ReadValue();
                if (IsOnCorrectSide(mousePos))
                {
                    swipeStartPosition = mousePos;
                    swipeStartTime = Time.time;
                    isTracking = true;
                }
            }

            if (mouse.leftButton.wasReleasedThisFrame && isTracking)
            {
                Vector2 mouseEnd = mouse.position.ReadValue();
                CheckSwipe(mouseEnd);
                isTracking = false;
            }
        }
    }

    bool IsOnCorrectSide(Vector2 screenPos)
    {
        float screenMidpoint = Screen.width / 2f;
        if (isLeftHammer)
        {
            return screenPos.x < screenMidpoint;
        }
        else
        {
            return screenPos.x > screenMidpoint;
        }
    }

    void CheckSwipe(Vector2 endPosition)
    {
        float swipeTime = Time.time - swipeStartTime;
        Debug.Log($"[{gameObject.name}] CheckSwipe: time={swipeTime}, timeWindow={swipeTimeWindow}");
        if (swipeTime > swipeTimeWindow)
        {
            Debug.Log($"[{gameObject.name}] Swipe too slow, ignoring");
            return;
        }

        Vector2 swipeDelta = endPosition - swipeStartPosition;
        float swipeDistance = swipeDelta.magnitude;

        Debug.Log($"[{gameObject.name}] Swipe: distance={swipeDistance}, threshold={swipeThreshold}, deltaY={swipeDelta.y}, state={currentState}");

        // Check if swipe is downward and fast enough
        if (swipeDistance > swipeThreshold && swipeDelta.y < 0 && currentState == State.WaitingAtTop)
        {
            Debug.LogWarning($"[{gameObject.name}] *** HAMMER TRIGGERED! ***");
            // Trigger hammer strike
            timer = 0f;
            currentState = State.SwipingDown;
        }
        else
        {
            Debug.Log($"[{gameObject.name}] Swipe rejected - distance enough: {swipeDistance > swipeThreshold}, downward: {swipeDelta.y < 0}, ready: {currentState == State.WaitingAtTop}");
        }
    }

    // Call this to trigger a hammer strike
    public void TriggerStrike()
    {
        if (currentState == State.WaitingAtTop)
        {
            timer = 0f;
            currentState = State.SwipingDown;
        }
    }
}
