using UnityEngine;
using UnityEngine.InputSystem;

public enum ScoringDirection { Up, Down, Both }

public class MyLever : MonoBehaviour
{
    // Static flag to tell other scripts (like carousel) that a lever is being dragged
    public static bool IsAnyLeverActive { get; private set; } = false;
    private static float lastLeverReleaseTime = -999f;
    public static float LeverCooldown = 0.3f; // Cooldown after lever release

    public float rotationSpeed = 1f;
    public float springBackSpeed = 5f;
    public bool enableSpringBack = true;
    
    [Header("Collider Size")]
    public Vector3 colliderSize = new Vector3(0.5f, 3f, 0.5f);
    
    [Header("Horizontal Sliding")]
    public bool enableSliding = true;
    public float slideSpeed = 0.001f;  // Much smaller
    public float minSlidePosition = -1f;  // Much tighter range
    public float maxSlidePosition = 1f;   // Much tighter range
    public float slideSpringBackSpeed = 5f;
    public bool enableSlideSpringBack = true;
    
    [Header("Light Control")]
    public Light controlledLight;
    public float activationThreshold = 5f;

    [Header("Scoring Configuration")]
    public ScoringDirection scoringDirection = ScoringDirection.Up;

    private bool hasScored = false; // Track if we've scored in this pull

    private float targetRotation = -90f;
    public float centerRotation = -90f;
    public float minRotation = -135f;
    public float maxRotation = -45f;
    private bool dragging = false;
    private float lastMouseY;
    private float lastMouseX;
    private Camera mainCamera;
    private BoxCollider myCollider;
    
    private float targetSlidePosition = 0f;
    private float centerSlidePosition = 0f;
    private CircularCarousel carousel;
    private int originalLayer;
    private GameObject myCarouselWrapper;
    private InteractionZone interactionZone;

    void Start()
    {
        mainCamera = Camera.main;
        
        Collider oldCol = GetComponent<Collider>();
        if (oldCol != null) Destroy(oldCol);
        
        myCollider = gameObject.AddComponent<BoxCollider>();
        myCollider.center = Vector3.zero;
        myCollider.size = colliderSize;
        
        targetRotation = centerRotation;
        transform.localRotation = Quaternion.Euler(centerRotation, 0, 0);
        
        // Initialize slide position to current position
        targetSlidePosition = transform.localPosition.x;
        centerSlidePosition = targetSlidePosition;
        
        if (controlledLight != null)
        {
            controlledLight.enabled = false;
        }

        // Find the carousel
        carousel = FindFirstObjectByType<CircularCarousel>();

        // Store original layer
        originalLayer = gameObject.layer;

        // Find my carousel wrapper by looking for a parent that's in the carousel array
        if (carousel != null)
        {
            myCarouselWrapper = FindMyCarouselWrapper();
            if (myCarouselWrapper != null)
            {
                Debug.Log($"Lever '{gameObject.name}': Found carousel wrapper: {myCarouselWrapper.name}");
            }
            else
            {
                Debug.LogWarning($"Lever '{gameObject.name}': Could not find carousel wrapper in parent hierarchy!");
            }
        }

        // Find interaction zone in the scene or parent hierarchy
        interactionZone = GetComponentInParent<InteractionZone>();
        if (interactionZone == null)
        {
            interactionZone = FindFirstObjectByType<InteractionZone>();
        }

        if (interactionZone != null)
        {
            Debug.Log($"Lever '{gameObject.name}': Found interaction zone: {interactionZone.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Lever '{gameObject.name}': No InteractionZone found - will accept input from anywhere");
        }
    }
    
    void Update()
    {
        // Check if work shift is complete - if so, block ALL input
        if (GameManager.Instance != null && GameManager.Instance.IsWorkShiftComplete())
        {
            if (dragging)
            {
                Debug.LogWarning($"Lever '{gameObject.name}': BLOCKED - Work shift complete! Releasing drag.");
                dragging = false;
                IsAnyLeverActive = false;
            }
            return;
        }

        // Check if GameManager says we shouldn't be processing input
        if (GameManager.Instance != null && !GameManager.Instance.IsCarouselActive())
        {
            // Carousel is not active (e.g., monitor is zoomed in), don't process input
            if (dragging)
            {
                Debug.LogWarning($"Lever '{gameObject.name}': BLOCKED - Carousel not active but was dragging! Releasing drag.");
                dragging = false;
            }
            return;
        }

        Debug.Log($"Lever '{gameObject.name}': Update - dragging={dragging}, carousel active={GameManager.Instance?.IsCarouselActive()}");

        // Only process input if this lever's wrapper is centered
        if (carousel != null && myCarouselWrapper != null)
        {
            GameObject centeredObject = carousel.GetCenteredObject();
            bool isCentered = (centeredObject == myCarouselWrapper);

            if (!isCentered)
            {
                // Move to Ignore Raycast layer so it doesn't block clicks to other objects
                if (gameObject.layer != LayerMask.NameToLayer("Ignore Raycast"))
                {
                    gameObject.layer = LayerMask.NameToLayer("Ignore Raycast");
                }
                if (dragging)
                {
                    Debug.LogWarning($"Lever '{gameObject.name}': BLOCKED - Not centered but was dragging! Releasing drag.");
                    dragging = false;
                }
                return;
            }
            else
            {
                // Restore original layer when centered
                if (gameObject.layer != originalLayer)
                {
                    gameObject.layer = originalLayer;
                    Debug.Log($"Lever '{gameObject.name}': IS centered, restoring to original layer");
                }
            }
        }

        bool inputHandled = false;
        Vector2 inputPosition = Vector2.zero;
        bool inputPressed = false;
        bool inputHeld = false;
        bool inputReleased = false;

        // Check for touch input first (mobile)
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var touch = touchscreen.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                inputPosition = touch.position.ReadValue();
                inputPressed = true;
                inputHeld = true; // IMPORTANT: Set inputHeld on first press too!
                inputHandled = true;
            }
            else if (touch.press.isPressed)
            {
                inputPosition = touch.position.ReadValue();
                inputHeld = true;
                inputHandled = true;
            }

            if (touch.press.wasReleasedThisFrame)
            {
                inputReleased = true;
                inputHandled = true;
            }
        }

        // Fallback to mouse input if no touch was handled
        if (!inputHandled)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                // Always read mouse position when checking mouse input
                inputPosition = mouse.position.ReadValue();

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    inputPressed = true;
                    inputHeld = true; // IMPORTANT: Set inputHeld on first press too!
                }
                else if (mouse.leftButton.isPressed)
                {
                    inputHeld = true;
                }

                if (mouse.leftButton.wasReleasedThisFrame)
                {
                    inputReleased = true;
                }
            }
            else
            {
                return; // No input devices available
            }
        }

        // Handle input press
        if (inputPressed)
        {
            Debug.Log($"Lever '{gameObject.name}': Input pressed at {inputPosition}, enabled={enabled}, collider enabled={myCollider?.enabled}");

            // First check if input is within interaction zone
            if (interactionZone != null && !interactionZone.IsPositionInZone(inputPosition))
            {
                Debug.Log($"Lever '{gameObject.name}': Input rejected - outside interaction zone");
                return;
            }

            // Convert lever bounds to screen space for more reliable detection
            Bounds bounds = myCollider.bounds;
            Vector3[] corners = new Vector3[8];
            corners[0] = bounds.min;
            corners[1] = new Vector3(bounds.min.x, bounds.min.y, bounds.max.z);
            corners[2] = new Vector3(bounds.min.x, bounds.max.y, bounds.min.z);
            corners[3] = new Vector3(bounds.max.x, bounds.min.y, bounds.min.z);
            corners[4] = new Vector3(bounds.min.x, bounds.max.y, bounds.max.z);
            corners[5] = new Vector3(bounds.max.x, bounds.min.y, bounds.max.z);
            corners[6] = new Vector3(bounds.max.x, bounds.max.y, bounds.min.z);
            corners[7] = bounds.max;

            // Find screen space bounding box
            Vector2 min = new Vector2(float.MaxValue, float.MaxValue);
            Vector2 max = new Vector2(float.MinValue, float.MinValue);

            foreach (Vector3 corner in corners)
            {
                Vector3 screenPoint = mainCamera.WorldToScreenPoint(corner);
                if (screenPoint.z > 0) // Only if in front of camera
                {
                    min.x = Mathf.Min(min.x, screenPoint.x);
                    min.y = Mathf.Min(min.y, screenPoint.y);
                    max.x = Mathf.Max(max.x, screenPoint.x);
                    max.y = Mathf.Max(max.y, screenPoint.y);
                }
            }

            // Add some padding for easier clicking
            float padding = 20f;
            min.x -= padding;
            min.y -= padding;
            max.x += padding;
            max.y += padding;

            // Check if input is within screen space bounds
            bool hitThisLever = inputPosition.x >= min.x && inputPosition.x <= max.x &&
                               inputPosition.y >= min.y && inputPosition.y <= max.y;

            Debug.Log($"Lever '{gameObject.name}': Screen bounds check - input:{inputPosition}, min:{min}, max:{max}, hit:{hitThisLever}");

            if (hitThisLever)
            {
                Debug.Log($"Lever '{gameObject.name}': ✓ Started dragging - SCREEN SPACE HIT!");
                dragging = true;
                IsAnyLeverActive = true; // Set static flag so carousel knows
                lastMouseY = inputPosition.y;
                lastMouseX = inputPosition.x;
                targetRotation = transform.localRotation.eulerAngles.x;
                if (targetRotation > 180) targetRotation -= 360;

                // Capture current slide position when starting drag
                targetSlidePosition = transform.localPosition.x;
            }
            else
            {
                Debug.Log($"Lever '{gameObject.name}': ✗ MISSED - input outside screen bounds");
            }
        }

        // Handle input hold (dragging)
        if (dragging && inputHeld)
        {
            float currentMouseY = inputPosition.y;
            float currentMouseX = inputPosition.x;

            // Vertical rotation (existing)
            float deltaY = currentMouseY - lastMouseY;
            Debug.Log($"Lever '{gameObject.name}': DRAGGING - deltaY={deltaY}, currentY={currentMouseY}, lastY={lastMouseY}, rotSpeed={rotationSpeed}");

            targetRotation += deltaY * rotationSpeed;
            targetRotation = Mathf.Clamp(targetRotation, minRotation, maxRotation);
            transform.localRotation = Quaternion.Euler(targetRotation, 0, 0);

            Debug.Log($"Lever '{gameObject.name}': New rotation={targetRotation}, transform={transform.localRotation.eulerAngles.x}");

            // Horizontal sliding with proper clamping
            if (enableSliding)
            {
                float deltaX = currentMouseX - lastMouseX;
                targetSlidePosition += deltaX * slideSpeed;

                // Clamp to min/max relative to center
                float absoluteMin = centerSlidePosition + minSlidePosition;
                float absoluteMax = centerSlidePosition + maxSlidePosition;
                targetSlidePosition = Mathf.Clamp(targetSlidePosition, absoluteMin, absoluteMax);

                Vector3 newPos = transform.localPosition;
                newPos.x = targetSlidePosition;
                transform.localPosition = newPos;
            }

            lastMouseY = currentMouseY;
            lastMouseX = currentMouseX;
        }
        else if (dragging && !inputHeld)
        {
            Debug.LogWarning($"Lever '{gameObject.name}': dragging=TRUE but inputHeld=FALSE!");
        }
        else if (!dragging && inputHeld)
        {
            Debug.Log($"Lever '{gameObject.name}': dragging=FALSE but inputHeld=TRUE");
        }

        // Handle input release
        if (inputReleased)
        {
            if (dragging)
            {
                Debug.Log($"Lever '{gameObject.name}': Released - setting cooldown");
                IsAnyLeverActive = false;
                lastLeverReleaseTime = Time.time;
            }
            dragging = false;
        }

        // Spring back rotation to center
        if (!dragging && enableSpringBack && Mathf.Abs(targetRotation - centerRotation) > 0.5f)
        {
            targetRotation = Mathf.Lerp(targetRotation, centerRotation, Time.deltaTime * springBackSpeed);
            transform.localRotation = Quaternion.Euler(targetRotation, 0, 0);
        }

        // Spring back slide position to center
        if (!dragging && enableSlideSpringBack && Mathf.Abs(targetSlidePosition - centerSlidePosition) > 0.01f)
        {
            targetSlidePosition = Mathf.Lerp(targetSlidePosition, centerSlidePosition, Time.deltaTime * slideSpringBackSpeed);
            Vector3 newPos = transform.localPosition;
            newPos.x = targetSlidePosition;
            transform.localPosition = newPos;
        }

        UpdateLight();
    }

    void UpdateLight()
    {
        // Early return if no light is assigned
        if (controlledLight == null) return;

        bool shouldLightBeOn = false;
        bool shouldScore = false;

        // Check scoring based on configured direction
        if (scoringDirection == ScoringDirection.Up || scoringDirection == ScoringDirection.Both)
        {
            // Check if lever is pushed forward (up) - close to maxRotation (-45)
            if (targetRotation >= (maxRotation - activationThreshold))
            {
                shouldLightBeOn = true;
                if (!hasScored)
                {
                    shouldScore = true;
                }
            }
        }

        if (scoringDirection == ScoringDirection.Down || scoringDirection == ScoringDirection.Both)
        {
            // Check if lever is pulled backward (down) - close to minRotation (-135)
            if (targetRotation <= (minRotation + activationThreshold))
            {
                shouldLightBeOn = true;
                if (!hasScored)
                {
                    shouldScore = true;
                }
            }
        }

        // Update light state
        if (shouldLightBeOn && !controlledLight.enabled)
        {
            controlledLight.enabled = true;
        }
        else if (!shouldLightBeOn && controlledLight.enabled)
        {
            controlledLight.enabled = false;
            hasScored = false; // Reset scoring flag when light turns off
        }

        // Award score if appropriate
        if (shouldScore)
        {
            UpdateScore(100);
            hasScored = true;
            Debug.Log($"Lever '{gameObject.name}': Scored 100 points!");
        }
    }
    
    void UpdateScore(int points)
    {
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(points);
        }
    }

    GameObject FindMyCarouselWrapper()
    {
        // Walk up the parent hierarchy and check if any parent is in the carousel's object array
        Transform current = transform.parent;
        while (current != null)
        {
            // Check if this parent is in the carousel's carouselObjects array
            if (carousel.carouselObjects != null)
            {
                foreach (GameObject carouselObj in carousel.carouselObjects)
                {
                    if (carouselObj == current.gameObject)
                    {
                        return current.gameObject;
                    }
                }
            }
            current = current.parent;
        }
        return null;
    }

    public static bool IsInCooldown()
    {
        return (Time.time - lastLeverReleaseTime) < LeverCooldown;
    }

    void OnDrawGizmos()
    {
        // Draw lever collider
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}