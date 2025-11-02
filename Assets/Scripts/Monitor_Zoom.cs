using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;
using System.Collections;
using System.Collections.Generic;

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

    [Header("Image Layout Settings")]
    [Tooltip("Horizontal spacing between images")]
    public float imageSpacing = 100f;
    [Tooltip("Y position for images (relative to parent anchor - negative moves down, positive moves up)")]
    public float imageYPosition = -300f;

    private GameManager.MonitorMessage[] monitorMessages;
    private GameObject[] currentImages;
    private List<GameObject> instantiatedImages = new List<GameObject>();
    
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

        // Hide any images that might be in monitor messages at startup
        if (gameManager != null)
        {
            var messages = gameManager.GetCurrentMonitorMessages();
            if (messages != null)
            {
                foreach (var msg in messages)
                {
                    if (msg.imagesToShow != null)
                    {
                        foreach (var image in msg.imagesToShow)
                        {
                            if (image != null)
                            {
                                image.SetActive(false);
                                Debug.Log($"Monitor Start: Hiding image {image.name}");
                            }
                        }
                    }
                }
            }
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
        Debug.Log("=== Monitor: OnEnable called - auto-starting zoom in ===");
        Debug.Log($"Monitor enabled state: {enabled}, gameObject active: {gameObject.activeInHierarchy}");

        // Clean up any previous state
        CleanupAllImages();

        // Stop any running coroutines from previous session
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
            typingCoroutine = null;
        }

        // Get monitor messages from GameManager's current story point
        if (gameManager != null)
        {
            monitorMessages = gameManager.GetCurrentMonitorMessages();
            if (monitorMessages != null && monitorMessages.Length > 0)
            {
                // Convert MonitorMessage array to string array for backward compatibility
                textMessages = new string[monitorMessages.Length];
                for (int i = 0; i < monitorMessages.Length; i++)
                {
                    textMessages[i] = monitorMessages[i].text;

                    // Hide all images at start
                    if (monitorMessages[i].imagesToShow != null)
                    {
                        foreach (var image in monitorMessages[i].imagesToShow)
                        {
                            if (image != null)
                            {
                                image.SetActive(false);
                                Debug.Log($"Monitor: Hiding image at start: {image.name}");
                            }
                        }
                    }
                }
                Debug.Log($"Monitor: Using {textMessages.Length} text messages from story point {gameManager.GetCurrentStoryPoint()}");
            }
            else
            {
                Debug.Log("Monitor: Story point has no monitor messages or is empty");
            }
        }
        else
        {
            Debug.LogWarning("Monitor: gameManager is NULL in OnEnable!");
        }

        // Reset all state for fresh zoom in
        isZoomed = true;
        zoomStartTime = Time.time;
        currentTextIndex = 0;
        isTypingComplete = false;
        hasStartedTyping = false;
        currentImages = null;
        isPressingMouse = false;

        Debug.Log("Monitor: Calling UpdateUIVisibility and DisableOtherColliders");
        UpdateUIVisibility();
        DisableOtherColliders();
        Debug.Log("=== Monitor: OnEnable complete ===");
    }
    
    void Update()
    {
        if (mainCamera == null || !enabled) return;
        
        // EMERGENCY ZOOM OUT - Press Z or SPACE key
        var keyboard = Keyboard.current;
        if (keyboard != null && (keyboard.zKey.wasPressedThisFrame || keyboard.spaceKey.wasPressedThisFrame))
        {
            Debug.Log("*** EMERGENCY ZOOM OUT TRIGGERED BY KEYBOARD ***");
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

        // Hide/accelerate bonus message on monitor click
        if (gameManager != null)
        {
            gameManager.HideBonusMessage();
        }

        // If still typing, skip to the end of the current message
        if (!isTypingComplete)
        {
            Debug.Log("Still typing - skipping to end of animation");

            // Stop the typing coroutine
            if (typingCoroutine != null)
            {
                StopCoroutine(typingCoroutine);
                typingCoroutine = null;
            }

            // Show the complete message immediately
            if (textMessages != null && currentTextIndex < textMessages.Length)
            {
                textToShowWhenZoomed.text = textMessages[currentTextIndex];
                isTypingComplete = true;
                Debug.Log($"*** MESSAGE {currentTextIndex + 1} COMPLETED IMMEDIATELY ***");

                // Show the images now that text is complete
                ShowImagesForCurrentMessage();
            }
        }
        // If typing is complete, move to next message or zoom out
        else
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
    }

    void CleanupAllImages()
    {
        Debug.Log("*** CLEANUP ALL IMAGES CALLED ***");

        // Destroy all instantiated images immediately
        Debug.Log($"Monitor: Destroying {instantiatedImages.Count} instantiated images");
        foreach (var img in instantiatedImages)
        {
            if (img != null)
            {
                DestroyImmediate(img);
            }
        }
        instantiatedImages.Clear();

        // Hide all original template images from monitor messages
        if (monitorMessages != null)
        {
            foreach (var message in monitorMessages)
            {
                if (message.imagesToShow != null)
                {
                    foreach (var image in message.imagesToShow)
                    {
                        if (image != null)
                        {
                            image.SetActive(false);
                        }
                    }
                }
            }
        }

        // Also check if gameManager has current monitor messages and hide those too
        if (gameManager != null)
        {
            var currentMessages = gameManager.GetCurrentMonitorMessages();
            if (currentMessages != null && currentMessages != monitorMessages)
            {
                foreach (var message in currentMessages)
                {
                    if (message.imagesToShow != null)
                    {
                        foreach (var image in message.imagesToShow)
                        {
                            if (image != null)
                            {
                                image.SetActive(false);
                            }
                        }
                    }
                }
            }
        }

        currentImages = null;
        Debug.Log("*** ALL IMAGES CLEANED UP ***");
    }

    void ForceZoomOut()
    {
        Debug.Log("*** FORCE ZOOM OUT STARTING ***");

        // IMMEDIATELY hide and destroy all images FIRST
        CleanupAllImages();

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

        // Hide and destroy previous instantiated images
        foreach (var img in instantiatedImages)
        {
            if (img != null)
            {
                Destroy(img);
            }
        }
        instantiatedImages.Clear();

        // DON'T show images yet - they will be shown after typing completes
        currentImages = null;

        isTypingComplete = false;

        // Stop any existing typing coroutine
        if (typingCoroutine != null)
        {
            StopCoroutine(typingCoroutine);
        }

        // Start typing the current message
        typingCoroutine = StartCoroutine(TypeCurrentText());
    }

    void ShowImagesForCurrentMessage()
    {
        // Show images for current message if it has any
        if (monitorMessages != null && currentTextIndex < monitorMessages.Length)
        {
            GameObject[] imagesToShow = monitorMessages[currentTextIndex].imagesToShow;
            if (imagesToShow != null && imagesToShow.Length > 0)
            {
                // Calculate total width needed for all images
                float totalWidth = (imagesToShow.Length - 1) * imageSpacing;

                // Calculate starting X position to center the row
                float startX = -totalWidth / 2f;

                Debug.Log($"Monitor: Positioning {imagesToShow.Length} images with spacing {imageSpacing}, totalWidth={totalWidth}, startX={startX}");

                // Create instances and position each image
                for (int i = 0; i < imagesToShow.Length; i++)
                {
                    if (imagesToShow[i] != null)
                    {
                        // Calculate X position for this image
                        float xPos = startX + (i * imageSpacing);

                        // Instantiate a copy of the image
                        GameObject imageInstance = Instantiate(imagesToShow[i], imagesToShow[i].transform.parent);
                        instantiatedImages.Add(imageInstance);

                        // Remove any colliders from the instantiated image (they can block input)
                        Collider[] imageColliders = imageInstance.GetComponentsInChildren<Collider>(true);
                        foreach (var col in imageColliders)
                        {
                            Destroy(col);
                            Debug.Log($"Monitor: Removed collider from instantiated image: {col.gameObject.name}");
                        }

                        // Set position (assumes images are RectTransforms in UI)
                        RectTransform rectTransform = imageInstance.GetComponent<RectTransform>();
                        if (rectTransform != null)
                        {
                            // Position relative to parent, using configured Y position
                            rectTransform.anchoredPosition = new Vector2(xPos, imageYPosition);

                            // Make sure the image doesn't block raycasts
                            UnityEngine.UI.Image uiImage = imageInstance.GetComponent<UnityEngine.UI.Image>();
                            if (uiImage != null)
                            {
                                uiImage.raycastTarget = false;
                                Debug.Log($"Monitor: Disabled raycastTarget for image: {imageInstance.name}");
                            }

                            Debug.Log($"Monitor: Instantiated and positioned image {i} '{imageInstance.name}' at anchoredPosition x={xPos}, y={imageYPosition}");
                        }
                        else
                        {
                            Debug.LogWarning($"Monitor: Image '{imageInstance.name}' has no RectTransform - skipping position");
                        }

                        imageInstance.SetActive(true);
                        Debug.Log($"Monitor: Showing image for message {currentTextIndex + 1}: {imageInstance.name}");
                    }
                }

                currentImages = imagesToShow;
            }
            else
            {
                currentImages = null;
                Debug.Log($"Monitor: No images for message {currentTextIndex + 1}");
            }
        }
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

        // NOW show the images after typing is complete
        ShowImagesForCurrentMessage();
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
        else
        {
            Debug.Log("Monitor: NO DIAL TO RE-ENABLE (dialRotaryPhone is NULL)");
        }

        // Re-enable the lever COMPONENT
        if (lever != null)
        {
            Debug.Log($"Monitor: Lever found - was enabled: {lever.enabled}, now enabling...");
            lever.enabled = true;
            Debug.Log($"Monitor: Re-enabled MyLever component on {lever.gameObject.name}, now enabled: {lever.enabled}");
        }
        else
        {
            Debug.Log("Monitor: NO LEVER TO RE-ENABLE (lever is NULL)");
        }
    }

    void OnDisable()
    {
        Debug.Log($"*** OnDisable called - cleaning up ALL images ***");

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

        // Clean up all images using the centralized cleanup function
        CleanupAllImages();

        // Re-enable other colliders
        EnableOtherColliders();

        Debug.Log($"Monitor '{gameObject.name}' disabled - cleanup complete");
    }
}