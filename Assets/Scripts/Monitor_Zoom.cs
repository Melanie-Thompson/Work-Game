using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;

public class ClickableMonitor : MonoBehaviour
{
    [Header("Camera Settings")]
    public Camera targetCamera;
    
    [Header("Zoom Settings")]
    public float zoomSpeed = 5f;
    public float zoomedOrthographicSize = 1f;
    [Tooltip("How much closer to move the camera when zoomed (along its current view direction)")]
    public float zoomDistance = 3f;
    
    [Header("UI Settings")]
    [Tooltip("GameObject to hide when zoomed in (e.g., your score UI)")]
    public GameObject uiToHide;
    
    [Tooltip("TextMeshPro component to show ONLY when zoomed in with typing animation")]
    public TextMeshProUGUI textToShowWhenZoomed;
    
    [Header("Text Content")]
    [Tooltip("Array of text messages to display in sequence")]
    [TextArea(3, 10)]
    public string[] textMessages;
    
    [Header("Typing Animation Settings")]
    [Tooltip("Speed of typing animation (characters per second)")]
    public float typingSpeed = 20f;
    
    [Tooltip("Threshold to consider zoom complete (closer to 0 = more precise)")]
    public float zoomThreshold = 0.1f;
    
    [Tooltip("Minimum time to wait after zoom starts before checking completion")]
    public float minZoomWaitTime = 0.5f;
    
    [Header("Click Settings")]
    [Tooltip("If true, will check for clicks even when UI is present")]
    public bool clickThroughUI = true;
    
    [Header("Swipe Settings")]
    [Tooltip("Minimum distance to count as a swipe instead of click")]
    public float swipeThreshold = 30f;
    
    [Header("Raycast Settings")]
    [Tooltip("Maximum distance for raycast")]
    public float maxRaycastDistance = 1000f;
    
    private bool isZoomed = false;
    private Camera mainCamera;
    private float originalOrthographicSize;
    private Vector3 originalCameraPosition;
    private Quaternion originalCameraRotation;
    private Vector3 zoomedCameraPosition;
    private Coroutine typingCoroutine;
    private bool hasStartedTyping = false;
    private float zoomStartTime = 0f;
    private int currentTextIndex = 0;
    private bool isTypingComplete = false;
    private GameManager gameManager;
    private InteractionZone interactionZone;
    private DialRotaryPhone dialRotaryPhone;
    private MyLever lever;

    // Input tracking
    private bool isPressingMouse = false;
    private Vector2 mousePressPosition;
    private float lastZoomOutTime = -999f;
    private float zoomOutCooldown = 1f;
    
    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
        Debug.Log($"Monitor '{gameObject.name}': GameManager set");
    }
    
    void Start()
    {
        Debug.Log("=== MONITOR START ===");
        
        mainCamera = targetCamera != null ? targetCamera : Camera.main;
        
        if (mainCamera == null)
        {
            Debug.LogError("No camera found!");
            return;
        }
        
        // Ensure we have a collider
        Collider myCollider = GetComponent<Collider>();
        if (myCollider == null)
        {
            myCollider = gameObject.AddComponent<BoxCollider>();
            Debug.Log($"Monitor '{gameObject.name}': Added BoxCollider");
        }
        else
        {
            Debug.Log($"Monitor '{gameObject.name}': Already has collider of type {myCollider.GetType().Name}, enabled: {myCollider.enabled}");
        }

        // Log collider details
        Debug.Log($"Monitor '{gameObject.name}': Collider bounds: {myCollider.bounds}, isTrigger: {myCollider.isTrigger}");
        
        // Save original camera state
        originalOrthographicSize = mainCamera.orthographicSize;
        originalCameraPosition = mainCamera.transform.position;
        originalCameraRotation = mainCamera.transform.rotation;
        
        // Calculate zoomed position: move camera forward along its view direction
        zoomedCameraPosition = originalCameraPosition + mainCamera.transform.forward * zoomDistance;
        
        // Initialize text
        if (textToShowWhenZoomed != null)
        {
            textToShowWhenZoomed.text = "";
            textToShowWhenZoomed.gameObject.SetActive(false);
            Debug.Log($"Text GameObject '{textToShowWhenZoomed.gameObject.name}' disabled at start");
        }
        
        // Validate text array
        if (textMessages == null || textMessages.Length == 0)
        {
            Debug.LogWarning("No text messages assigned! Add messages in the inspector.");
        }
        else
        {
            Debug.Log($"Monitor has {textMessages.Length} text messages");
        }
        
        Debug.Log($"Monitor '{gameObject.name}' initialized.");
        Debug.Log($"Original camera pos: {originalCameraPosition}");
        Debug.Log($"Zoomed camera pos: {zoomedCameraPosition}");

        // Find interaction zone in the scene or parent hierarchy
        interactionZone = GetComponentInParent<InteractionZone>();
        if (interactionZone == null)
        {
            interactionZone = FindFirstObjectByType<InteractionZone>();
        }

        if (interactionZone != null)
        {
            Debug.Log($"Monitor '{gameObject.name}': Found interaction zone: {interactionZone.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"Monitor '{gameObject.name}': No InteractionZone found - will accept input from anywhere");
        }

        // Find dial and lever in parent hierarchy
        dialRotaryPhone = GetComponentInParent<DialRotaryPhone>();
        if (dialRotaryPhone == null)
        {
            dialRotaryPhone = transform.parent?.GetComponentInChildren<DialRotaryPhone>();
        }
        if (dialRotaryPhone != null)
        {
            Debug.Log($"Monitor '{gameObject.name}': Found DialRotaryPhone: {dialRotaryPhone.gameObject.name}");
        }

        lever = GetComponentInParent<MyLever>();
        if (lever == null)
        {
            lever = transform.parent?.GetComponentInChildren<MyLever>();
        }
        if (lever != null)
        {
            Debug.Log($"Monitor '{gameObject.name}': Found MyLever: {lever.gameObject.name}");
        }
    }
    
    void OnEnable()
    {
        // When enabled by GameManager, automatically start zoom in
        Debug.Log("Monitor: OnEnable called - auto-starting zoom in");
        isZoomed = true;
        zoomStartTime = Time.time;
        currentTextIndex = 0;
        isTypingComplete = false;
        hasStartedTyping = false;
        UpdateUIVisibility();
        DisableOtherColliders();
    }
    
    void Update()
    {
        if (mainCamera == null || !enabled) return;
        
        // EMERGENCY ZOOM OUT - Press Z key
        var keyboard = Keyboard.current;
        if (keyboard != null && keyboard.zKey.wasPressedThisFrame)
        {
            Debug.Log("*** EMERGENCY ZOOM OUT TRIGGERED BY Z KEY ***");
            ForceZoomOut();
            return;
        }
        
        // Check global state from GameManager
        if (gameManager != null && !gameManager.IsMonitorActive())
        {
            // Monitor should not be active, ignore all input
            return;
        }
        
        // ONLY process input if we're zoomed in
        if (isZoomed)
        {
            bool touchEventHandled = false;

            // Check for touch input first (mobile)
            var touchscreen = Touchscreen.current;
            if (touchscreen != null)
            {
                var touch = touchscreen.primaryTouch;

                // Track touch press
                if (touch.press.wasPressedThisFrame)
                {
                    Vector2 touchPosition = touch.position.ReadValue();

                    isPressingMouse = true;
                    mousePressPosition = touchPosition;
                    Debug.Log($"Monitor: Touch pressed at {mousePressPosition}");
                    touchEventHandled = true;
                }

                // Track touch release
                if (touch.press.wasReleasedThisFrame && isPressingMouse)
                {
                    isPressingMouse = false;
                    Vector2 touchReleasePosition = touch.position.ReadValue();
                    Vector2 touchDelta = touchReleasePosition - mousePressPosition;
                    float dragDistance = touchDelta.magnitude;

                    Debug.Log($"Monitor: Touch released at {touchReleasePosition}, delta: {touchDelta}, distance: {dragDistance}");

                    // Check if it's a swipe (movement exceeds threshold)
                    if (dragDistance > swipeThreshold)
                    {
                        Debug.Log($"Monitor: SWIPE detected (distance {dragDistance} > threshold {swipeThreshold}) - IGNORING");
                        // Don't process swipes on the monitor - just ignore them
                        return;
                    }

                    // It's a tap (minimal movement)
                    Debug.Log($"Monitor: TAP detected (distance {dragDistance} < threshold {swipeThreshold})");
                    Debug.Log("Monitor is zoomed - processing tap directly");
                    HandleMonitorClick();
                    touchEventHandled = true;
                }
            }

            // Check mouse input (always check, in case simulator uses mouse instead of touch)
            if (!touchEventHandled)
            {
                var mouse = Mouse.current;
                if (mouse != null)
                {
                    // Track mouse press
                    if (mouse.leftButton.wasPressedThisFrame)
                    {
                        Vector2 mousePosition = mouse.position.ReadValue();

                        isPressingMouse = true;
                        mousePressPosition = mousePosition;
                        Debug.Log($"Monitor: Mouse pressed at {mousePressPosition}");
                    }

                    // Track mouse release
                    if (mouse.leftButton.wasReleasedThisFrame && isPressingMouse)
                    {
                        isPressingMouse = false;
                        Vector2 mouseReleasePosition = mouse.position.ReadValue();
                        Vector2 mouseDelta = mouseReleasePosition - mousePressPosition;
                        float dragDistance = mouseDelta.magnitude;

                        Debug.Log($"Monitor: Mouse released at {mouseReleasePosition}, delta: {mouseDelta}, distance: {dragDistance}");

                        // Check if it's a swipe (movement exceeds threshold)
                        if (dragDistance > swipeThreshold)
                        {
                            Debug.Log($"Monitor: SWIPE detected (distance {dragDistance} > threshold {swipeThreshold}) - IGNORING");
                            // Don't process swipes on the monitor - just ignore them
                            return;
                        }

                        // It's a click (minimal movement)
                        Debug.Log($"Monitor: CLICK detected (distance {dragDistance} < threshold {swipeThreshold})");

                        // When zoomed in, ANY click should work (don't require hitting the monitor)
                        // This prevents issues where raycasts miss the monitor when camera is very close
                        Debug.Log("Monitor is zoomed - processing click directly");
                        HandleMonitorClick();
                    }
                }
            }
        }
        
        // Smoothly transition orthographic size
        float targetSize = isZoomed ? zoomedOrthographicSize : originalOrthographicSize;
        mainCamera.orthographicSize = Mathf.Lerp(
            mainCamera.orthographicSize, 
            targetSize, 
            Time.deltaTime * zoomSpeed
        );
        
        // Smoothly transition camera position
        Vector3 targetPosition = isZoomed ? zoomedCameraPosition : originalCameraPosition;
        mainCamera.transform.position = Vector3.Lerp(
            mainCamera.transform.position, 
            targetPosition, 
            Time.deltaTime * zoomSpeed
        );
        
        // Lock rotation to original
        mainCamera.transform.rotation = Quaternion.Lerp(
            mainCamera.transform.rotation,
            originalCameraRotation,
            Time.deltaTime * zoomSpeed
        );
        
        // Check if zoom is complete and start typing animation
        if (isZoomed && !hasStartedTyping && textToShowWhenZoomed != null)
        {
            // Wait at least minZoomWaitTime seconds after zoom starts before checking completion
            if (Time.time - zoomStartTime > minZoomWaitTime)
            {
                float sizeDistance = Mathf.Abs(mainCamera.orthographicSize - zoomedOrthographicSize);
                float posDistance = Vector3.Distance(mainCamera.transform.position, zoomedCameraPosition);
                
                Debug.Log($"Checking zoom completion - Size dist: {sizeDistance}, Pos dist: {posDistance}");
                
                if (sizeDistance < zoomThreshold && posDistance < zoomThreshold)
                {
                    hasStartedTyping = true;
                    ShowCurrentMessage();
                    Debug.Log("Starting typing animation!");
                }
            }
        }
    }
    
    void HandleMonitorClick()
    {
        Debug.Log($"HandleMonitorClick called - isTypingComplete: {isTypingComplete}, currentTextIndex: {currentTextIndex}/{textMessages.Length}");

        // Only process clicks if typing is complete
        if (isTypingComplete)
        {
            Debug.Log($"*** Typing complete - checking message progress ***");
            Debug.Log($"*** currentTextIndex: {currentTextIndex}, textMessages.Length: {textMessages.Length}");
            Debug.Log($"*** Is last message? {currentTextIndex >= textMessages.Length - 1}");

            // Check if this was the last message
            if (currentTextIndex >= textMessages.Length - 1)
            {
                Debug.Log("*** LAST MESSAGE COMPLETE - ZOOMING OUT NOW ***");
                ForceZoomOut();
            }
            else
            {
                // Move to next message
                currentTextIndex++;
                Debug.Log($"Advancing to message {currentTextIndex + 1}/{textMessages.Length}");
                ShowCurrentMessage();
            }
        }
        else
        {
            Debug.Log("Still typing current message, ignoring click");
        }
    }

    void ForceZoomOut()
    {
        Debug.Log("*** FORCE ZOOM OUT STARTING ***");

        // Stop any coroutines
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Reset all state
        isZoomed = false;
        hasStartedTyping = false;
        currentTextIndex = 0;
        isTypingComplete = false;
        isPressingMouse = false;

        Debug.Log("*** Calling UpdateUIVisibility ***");
        UpdateUIVisibility();

        Debug.Log("Monitor: Starting zoom out and returning control to carousel");

        // Store gameManager reference
        GameManager gm = gameManager;

        // Start the delayed callback - wait for zoom animation to complete
        if (gm != null)
        {
            StartCoroutine(WaitForZoomOutThenCallback(gm));
        }
        else
        {
            Debug.LogError("GameManager is NULL! Cannot return control to carousel.");
        }
    }


    IEnumerator WaitForZoomOutThenCallback(GameManager gm)
    {
        Debug.Log("*** Waiting for zoom-out animation to complete ***");

        // Wait for the camera to zoom out (check position and size)
        while (true)
        {
            float sizeDistance = Mathf.Abs(mainCamera.orthographicSize - originalOrthographicSize);
            float posDistance = Vector3.Distance(mainCamera.transform.position, originalCameraPosition);

            // Check if we're close enough to the original position
            if (sizeDistance < zoomThreshold && posDistance < zoomThreshold)
            {
                Debug.Log("*** Zoom-out animation complete ***");
                break;
            }

            yield return null; // Wait one frame
        }

        // Short delay to prevent same-frame input conflicts
        yield return new WaitForSeconds(0.1f);

        // Disable the component to stop input processing
        enabled = false;
        Debug.Log("*** Monitor disabled ***");

        Debug.Log("Calling GameManager.OnMonitorClosed - returning control to carousel");
        gm.OnMonitorClosed();
    }

    
    void ShowCurrentMessage()
    {
        if (textMessages == null || textMessages.Length == 0 || currentTextIndex >= textMessages.Length)
        {
            Debug.LogWarning("No message to show!");
            return;
        }
        
        isTypingComplete = false;
        
        // Stop any existing typing coroutine
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }
        
        // Start typing the current message
        typingCoroutine = StartCoroutine(TypeCurrentText());
    }
    
    void UpdateUIVisibility()
    {
        if (uiToHide != null)
        {
            // Hide UI when zoomed in, show when zoomed out
            uiToHide.SetActive(!isZoomed);
            Debug.Log($"UI visibility set to: {!isZoomed}");
        }
        else
        {
            Debug.LogWarning("No UI GameObject assigned to hide/show!");
        }
        
        // Show text container when zoomed in (but text will be empty until typing starts)
        if (textToShowWhenZoomed != null)
        {
            textToShowWhenZoomed.gameObject.SetActive(isZoomed);
            if (!isZoomed)
            {
                textToShowWhenZoomed.text = ""; // Clear text when zooming out
            }
            Debug.Log($"Text visibility set to: {isZoomed}");
        }
    }
    
    IEnumerator TypeCurrentText()
    {
        string currentMessage = textMessages[currentTextIndex];
        textToShowWhenZoomed.text = "";
        
        Debug.Log($"Typing message {currentTextIndex + 1}/{textMessages.Length}: {currentMessage.Substring(0, Mathf.Min(20, currentMessage.Length))}...");
        
        foreach (char c in currentMessage)
        {
            textToShowWhenZoomed.text += c;
            yield return new WaitForSeconds(1f / typingSpeed);
        }
        
        isTypingComplete = true;
        Debug.Log($"*** MESSAGE {currentTextIndex + 1} TYPING COMPLETE ***");
        Debug.Log($"*** Is this the last message? {currentTextIndex} >= {textMessages.Length - 1} = {currentTextIndex >= textMessages.Length - 1}");
    }
    
    void DisableOtherColliders()
    {
        Debug.Log("*** Monitor: Disabling other interactive components ***");

        // Disable InteractionZone collider
        if (interactionZone != null)
        {
            BoxCollider zoneCollider = interactionZone.GetComponent<BoxCollider>();
            if (zoneCollider != null)
            {
                zoneCollider.enabled = false;
                Debug.Log($"Monitor: Disabled InteractionZone collider on {interactionZone.gameObject.name}");
            }
        }

        // Disable the dial COMPONENT AND all its colliders
        if (dialRotaryPhone != null)
        {
            dialRotaryPhone.enabled = false;
            Debug.Log($"Monitor: Disabled DialRotaryPhone component on {dialRotaryPhone.gameObject.name}");

            // Also disable all colliders on the dial
            Collider[] dialColliders = dialRotaryPhone.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in dialColliders)
            {
                col.enabled = false;
                Debug.Log($"Monitor: Disabled dial collider on {col.gameObject.name}");
            }
        }

        // Disable the lever COMPONENT (not just colliders)
        if (lever != null)
        {
            lever.enabled = false;
            Debug.Log($"Monitor: Disabled MyLever component on {lever.gameObject.name}");
        }
    }

    void EnableOtherColliders()
    {
        Debug.Log("*** Monitor: Re-enabling other interactive components ***");

        // Re-enable InteractionZone collider
        if (interactionZone != null)
        {
            BoxCollider zoneCollider = interactionZone.GetComponent<BoxCollider>();
            if (zoneCollider != null)
            {
                zoneCollider.enabled = true;
                Debug.Log($"Monitor: Re-enabled InteractionZone collider on {interactionZone.gameObject.name}");
            }
        }

        // Re-enable the dial COMPONENT AND all its colliders
        if (dialRotaryPhone != null)
        {
            dialRotaryPhone.enabled = true;
            Debug.Log($"Monitor: Re-enabled DialRotaryPhone component on {dialRotaryPhone.gameObject.name}");

            // Also re-enable all colliders on the dial
            Collider[] dialColliders = dialRotaryPhone.GetComponentsInChildren<Collider>(true);
            foreach (Collider col in dialColliders)
            {
                col.enabled = true;
                Debug.Log($"Monitor: Re-enabled dial collider on {col.gameObject.name}");
            }
        }

        // Re-enable the lever COMPONENT
        if (lever != null)
        {
            lever.enabled = true;
            Debug.Log($"Monitor: Re-enabled MyLever component on {lever.gameObject.name}");
        }
    }

    void OnDisable()
    {
        // Clean up when disabled
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Reset zoom state
        if (isZoomed)
        {
            isZoomed = false;
            UpdateUIVisibility();
        }

        // Re-enable other colliders
        EnableOtherColliders();

        Debug.Log($"Monitor '{gameObject.name}' disabled");
    }
}