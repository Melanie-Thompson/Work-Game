using UnityEngine;
using UnityEngine.InputSystem;

public class CircularCarousel : MonoBehaviour
{
    [Header("Carousel Settings")]
    public GameObject[] carouselObjects;  // Public so other scripts can check it
    public float radius = 3f;
    public float heightOffset = 0f;
    
    [Header("Rotation Settings")]
    public float snapSpeed = 8f;
    
    [Header("Swipe Settings")]
    public float swipeThreshold = 50f;
    public float swipeTimeWindow = 0.3f;

    [Header("Interaction Settings")]
    public float centerTolerance = 30f;
    public float clickThreshold = 5f;
    public float clickCooldown = 0.1f;

    [Header("Swipe Detection Colliders")]
    [Tooltip("Colliders that act as swipe zones (e.g., UI panels with BoxColliders)")]
    public Collider[] swipeZoneColliders;

    [Header("Dial Settings")]
    public GameObject dialObject;  // Assign the dial GameObject to ignore input on it

    private float currentAngle = 0f;
    private float targetAngle = 0f;
    private int currentIndex = 0;
    private bool isDragging = false;
    private bool isSnapping = false;
    private Vector2 dragStartPosition;
    private float dragStartTime;
    private float angleStep;
    private bool carouselEnabled = true;
    private GameManager gameManager;
    private float lastMonitorCloseTime = -999f;
    
    public void SetGameManager(GameManager manager)
    {
        gameManager = manager;
        Debug.Log($"Carousel: GameManager set - {(gameManager != null ? "SUCCESS" : "NULL")}");
    }
    
    public void DisableCarousel()
    {
        carouselEnabled = false;
        Debug.Log("Carousel: Disabled");
    }
    
    public void EnableCarousel()
    {
        Debug.Log($"*** EnableCarousel called - WAS: {carouselEnabled}, NOW: true ***");
        carouselEnabled = true;
        lastMonitorCloseTime = Time.time;
        Debug.Log($"Carousel carouselEnabled is now: {carouselEnabled}, cooldown until {Time.time + clickCooldown}");
    }
    
    void Start()
    {
        if (carouselObjects == null || carouselObjects.Length == 0)
        {
            Debug.LogWarning("No carousel objects assigned!");
            return;
        }
        
        angleStep = 360f / carouselObjects.Length;
        currentAngle = 0f;
        targetAngle = 0f;
        currentIndex = 0;
        
        // Disable all monitors at start
        foreach (var obj in carouselObjects)
        {
            if (obj == null) continue;
            
            var clickable = obj.GetComponentInChildren<ClickableMonitor>();
            if (clickable != null)
            {
                clickable.enabled = false;
            }
        }
        
        ArrangeObjects();
        Debug.Log($"Carousel ready! angleStep = {angleStep}°");
    }
    
    void Update()
    {
        // Auto-find GameManager if not set
        if (gameManager == null && GameManager.Instance != null)
        {
            gameManager = GameManager.Instance;
            Debug.Log("Carousel: Auto-found GameManager.Instance");
        }
        
        if (carouselEnabled)
        {
            HandleInput();
            
            if (isSnapping && !isDragging)
            {
                currentAngle = Mathf.LerpAngle(currentAngle, targetAngle, Time.deltaTime * snapSpeed);
                
                if (Mathf.Abs(Mathf.DeltaAngle(currentAngle, targetAngle)) < 0.1f)
                {
                    currentAngle = targetAngle;
                    isSnapping = false;
                }
            }
        }
        
        ArrangeObjects();
    }
    
    bool IsPositionOnLever(Vector2 screenPosition)
    {
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

        Debug.Log($"Carousel: IsPositionOnLever - Raycast found {hits.Length} hits");

        // Check ALL objects hit by the ray
        foreach (RaycastHit hit in hits)
        {
            Debug.Log($"  -> Lever check hit: {hit.collider.gameObject.name} (enabled: {hit.collider.enabled})");

            // Skip disabled colliders
            if (!hit.collider.enabled)
            {
                Debug.Log($"  -> SKIPPING disabled collider");
                continue;
            }

            // Check if this collider has a MyLever component or is a child of one
            MyLever lever = hit.collider.GetComponent<MyLever>();
            if (lever == null)
            {
                lever = hit.collider.GetComponentInParent<MyLever>();
            }

            if (lever != null)
            {
                Debug.Log($"  -> Found lever component! Lever enabled: {lever.enabled}");
                if (lever.enabled)
                {
                    Debug.Log($"Carousel: ✓✓✓ MATCH! Click is on LEVER (hit: {hit.collider.gameObject.name}) - ignoring input");
                    return true;
                }
                else
                {
                    Debug.Log($"  -> Lever is DISABLED, not ignoring input");
                }
            }
        }

        Debug.Log("Carousel: No lever match found - carousel will process input");
        return false;
    }

    bool IsPositionOnDial(Vector2 screenPosition)
    {
        if (dialObject == null)
        {
            Debug.Log("Carousel: dialObject is NULL - not checking for dial");
            return false;
        }

        // Check if the DialRotaryPhone component is enabled - if not, ignore dial checks
        DialRotaryPhone dialComponent = dialObject.GetComponent<DialRotaryPhone>();
        if (dialComponent == null)
        {
            dialComponent = dialObject.GetComponentInChildren<DialRotaryPhone>();
        }
        if (dialComponent != null && !dialComponent.enabled)
        {
            Debug.Log($"Carousel: DialRotaryPhone component is DISABLED - not checking for dial");
            return false;
        }

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

        Debug.Log($"Carousel: IsPositionOnDial - Raycast found {hits.Length} hits. DialObject = '{dialObject.name}'");

        // Check ALL objects hit by the ray, not just the first one
        // ONLY match if we hit actual number colliders (not the shell/body)
        foreach (RaycastHit hit in hits)
        {
            Debug.Log($"  -> Hit: {hit.collider.gameObject.name} (enabled: {hit.collider.enabled}, parent chain: {GetParentChain(hit.collider.transform)})");

            // Skip disabled colliders
            if (!hit.collider.enabled)
            {
                Debug.Log($"  -> SKIPPING disabled collider: {hit.collider.gameObject.name}");
                continue;
            }

            // ONLY match if the name contains "number" (like "0 number", "1 number", etc.)
            string hitName = hit.collider.gameObject.name.ToLower();
            if (hitName.Contains("number"))
            {
                // Now check if this number belongs to our dial
                Transform hitTransform = hit.collider.transform;
                while (hitTransform != null)
                {
                    if (hitTransform.gameObject == dialObject || hitTransform == dialObject.transform)
                    {
                        Debug.Log($"Carousel: ✓✓✓ MATCH! Click is on dial NUMBER (hit: {hit.collider.gameObject.name}) - ignoring input");
                        return true;
                    }
                    hitTransform = hitTransform.parent;
                }
            }
        }

        Debug.Log("Carousel: No dial match found - carousel will process input");
        return false;
    }

    string GetParentChain(Transform t)
    {
        string chain = t.name;
        Transform parent = t.parent;
        while (parent != null)
        {
            chain += " -> " + parent.name;
            parent = parent.parent;
        }
        return chain;
    }

    bool IsPositionInSwipeZone(Vector2 screenPosition)
    {
        // If no swipe zone colliders are assigned, allow swiping anywhere (legacy behavior)
        if (swipeZoneColliders == null || swipeZoneColliders.Length == 0)
        {
            return true;
        }

        // Cast a ray from the screen position
        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // Check if we hit any of the swipe zone colliders
        if (Physics.Raycast(ray, out hit, 1000f))
        {
            foreach (Collider swipeZone in swipeZoneColliders)
            {
                if (swipeZone != null && hit.collider == swipeZone)
                {
                    Debug.Log($"Position {screenPosition} hit swipe zone: {swipeZone.gameObject.name}");
                    return true;
                }
            }
        }

        Debug.Log($"Position {screenPosition} not in any swipe zone");
        return false;
    }

    void HandleInput()
    {
        Debug.Log($"Carousel: HandleInput called - carouselEnabled={carouselEnabled}, DialActive={DialRotaryPhone.IsDialActive}, LeverActive={MyLever.IsAnyLeverActive}, LeverCooldown={MyLever.IsInCooldown()}, GMCarouselActive={gameManager?.IsCarouselActive()}, WorkComplete={gameManager?.IsWorkShiftComplete()}");

        // Check if work shift is complete
        if (gameManager != null && gameManager.IsWorkShiftComplete())
        {
            Debug.Log("Carousel: Input ignored - work shift complete");
            return;
        }

        // Check if lever is being used or in cooldown
        if (MyLever.IsAnyLeverActive || MyLever.IsInCooldown())
        {
            Debug.Log($"Carousel: Input ignored - lever active or in cooldown");
            return;
        }

        // Check if dial is being used
        if (DialRotaryPhone.IsDialActive)
        {
            Debug.Log("Carousel: Input ignored - dial is active");
            return;
        }

        // Check global state from GameManager
        if (gameManager != null && !gameManager.IsCarouselActive())
        {
            Debug.Log("Carousel: Input ignored - carousel not active in GameManager");
            return;
        }

        bool inputHandled = false;

        // Try touch input first (mobile)
        var touchscreen = Touchscreen.current;
        if (touchscreen != null)
        {
            var touch = touchscreen.primaryTouch;

            if (touch.press.wasPressedThisFrame)
            {
                Vector2 touchPosition = touch.position.ReadValue();
                Debug.Log($"Carousel: *** TOUCH PRESSED at {touchPosition} - checking for dial/lever ***");

                // FIRST: Check if touching the dial - if so, ignore carousel input completely
                if (IsPositionOnDial(touchPosition))
                {
                    Debug.Log("Carousel: Touch on dial detected - ignoring carousel input");
                    inputHandled = true; // Mark as handled so we don't continue
                    return;
                }

                // Check if touching a lever - if so, ignore carousel input completely
                if (IsPositionOnLever(touchPosition))
                {
                    Debug.Log("Carousel: Touch on lever detected - ignoring carousel input");
                    inputHandled = true;
                    return;
                }

                // SECOND: Track touch for swipe or monitor click
                // Hide bonus message on any touch
                Debug.Log($"Carousel: Touch detected, gameManager is {(gameManager == null ? "NULL" : "NOT NULL")}");
                if (gameManager != null)
                {
                    Debug.Log("Carousel: Calling gameManager.HideBonusMessage()");
                    gameManager.HideBonusMessage();
                }
                else
                {
                    Debug.LogError("Carousel: Cannot hide bonus message - gameManager is NULL!");
                }

                isDragging = true;
                isSnapping = false;
                dragStartPosition = touchPosition;
                dragStartTime = Time.time;
                inputHandled = true;

                if (IsPositionInSwipeZone(touchPosition))
                {
                    Debug.Log($">>> TOUCH DRAG STARTED in swipe zone at {dragStartPosition}, time: {dragStartTime}");
                }
                else
                {
                    Debug.Log($">>> TOUCH DRAG STARTED outside swipe zone (possible monitor click) at {dragStartPosition}");
                }
            }

            if (touch.press.wasReleasedThisFrame && isDragging)
            {
                isDragging = false;
                Vector2 releasePosition = touch.position.ReadValue();
                ProcessInputRelease(releasePosition);
                inputHandled = true;
            }
        }

        // If no touch input was handled, check mouse input (desktop or simulator)
        if (!inputHandled)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Vector2 mousePosition = mouse.position.ReadValue();
                    Debug.Log($"Carousel: *** MOUSE PRESSED at {mousePosition} - checking for dial/lever ***");

                    // FIRST: Check if clicking the dial - if so, ignore carousel input completely
                    if (IsPositionOnDial(mousePosition))
                    {
                        Debug.Log("Carousel: Click on dial detected - ignoring carousel input");
                        return;
                    }

                    // Check if clicking a lever - if so, ignore carousel input completely
                    if (IsPositionOnLever(mousePosition))
                    {
                        Debug.Log("Carousel: Click on lever detected - ignoring carousel input");
                        return;
                    }

                    // SECOND: Check if click started in a valid swipe zone OR just track it for monitor clicks
                    // Hide bonus message on any click
                    Debug.Log($"Carousel: Mouse click detected, gameManager is {(gameManager == null ? "NULL" : "NOT NULL")}");
                    if (gameManager != null)
                    {
                        Debug.Log("Carousel: Calling gameManager.HideBonusMessage()");
                        gameManager.HideBonusMessage();
                    }
                    else
                    {
                        Debug.LogError("Carousel: Cannot hide bonus message - gameManager is NULL!");
                    }

                    isDragging = true;
                    isSnapping = false;
                    dragStartPosition = mousePosition;
                    dragStartTime = Time.time;

                    if (IsPositionInSwipeZone(mousePosition))
                    {
                        Debug.Log($">>> MOUSE DRAG STARTED in swipe zone at {dragStartPosition}, time: {dragStartTime}");
                    }
                    else
                    {
                        Debug.Log($">>> MOUSE DRAG STARTED outside swipe zone (possible monitor click) at {dragStartPosition}");
                    }
                }

                if (mouse.leftButton.wasReleasedThisFrame && isDragging)
                {
                    isDragging = false;
                    Vector2 releasePosition = mouse.position.ReadValue();
                    ProcessInputRelease(releasePosition);
                }
            }
        }
    }

    void ProcessInputRelease(Vector2 releasePosition)
    {
        Vector2 dragDelta = releasePosition - dragStartPosition;
        float dragDistance = dragDelta.magnitude;
        float dragTime = Time.time - dragStartTime;

        Debug.Log($">>> DRAG RELEASED at {releasePosition}");
        Debug.Log($">>> Delta: {dragDelta}, Distance: {dragDistance}, Time: {dragTime}");
        Debug.Log($">>> Swipe threshold: {swipeThreshold}, Time window: {swipeTimeWindow}");
        Debug.Log($">>> Click threshold: {clickThreshold}");

        // Check if this was in a swipe zone
        bool wasInSwipeZone = IsPositionInSwipeZone(dragStartPosition);
        Debug.Log($">>> Was drag start in swipe zone? {wasInSwipeZone}");

        // Check if it's a swipe
        if (dragDistance > swipeThreshold && dragTime < swipeTimeWindow && wasInSwipeZone)
        {
            Debug.Log($"*** SWIPE DETECTED *** - distance: {dragDistance}, time: {dragTime}");
            // SWIPE detected
            if (dragDelta.x > 0)
            {
                currentIndex--;
                Debug.Log("Swiped RIGHT - moving to previous object");
            }
            else
            {
                currentIndex++;
                Debug.Log("Swiped LEFT - moving to next object");
            }

            if (currentIndex < 0) currentIndex = carouselObjects.Length - 1;
            if (currentIndex >= carouselObjects.Length) currentIndex = 0;

            targetAngle = currentIndex * angleStep;
            isSnapping = true;

            // Award points for rotating carousel
            if (GameManager.Instance != null)
            {
                GameManager.Instance.AddScore(50);
                Debug.Log("Carousel: Awarded 50 points for rotation!");
            }

            Debug.Log($"Snapping to object {currentIndex}, angle: {targetAngle}°");
        }
        else if (dragDistance < clickThreshold)
        {
            Debug.Log($"*** CLICK DETECTED *** - distance: {dragDistance}, lastMonitorCloseTime: {lastMonitorCloseTime}");

            // Check cooldown period (only if we've closed a monitor before)
            if (lastMonitorCloseTime > 0)
            {
                float timeSinceMonitorClose = Time.time - lastMonitorCloseTime;
                if (timeSinceMonitorClose < clickCooldown)
                {
                    Debug.Log($"*** CLICK IGNORED - Still in cooldown period ({timeSinceMonitorClose:F2}s < {clickCooldown}s) ***");
                    return;
                }
                else
                {
                    Debug.Log($"*** COOLDOWN PERIOD PASSED ({timeSinceMonitorClose:F2}s >= {clickCooldown}s) - processing click ***");
                }
            }
            else
            {
                Debug.Log($"*** NO COOLDOWN CHECK (lastMonitorCloseTime={lastMonitorCloseTime}) - processing click ***");
            }
            // CLICK detected - check if centered object was clicked
            GameObject centeredObject = GetCenteredObject();
            Debug.Log($"Carousel: currentIndex={currentIndex}, centered object={centeredObject?.name}");

            if (centeredObject != null)
            {
                // Check if we actually clicked on the centered object
                Ray ray = Camera.main.ScreenPointToRay(releasePosition);
                RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

                Debug.Log($"Carousel: RaycastAll found {hits.Length} hits");

                // Log ALL hits first for debugging
                for (int i = 0; i < hits.Length; i++)
                {
                    Debug.Log($"  Hit {i}: {hits[i].collider.gameObject.name} (enabled: {hits[i].collider.enabled})");
                }

                // Filter through all hits to find the first one that belongs to the centered object
                // (Skip InteractionZone and other interactive object hits that handle their own input)
                RaycastHit? validHit = null;
                foreach (RaycastHit hit in hits)
                {
                    Debug.Log($"Carousel: Processing hit on {hit.collider.gameObject.name} (enabled: {hit.collider.enabled})");

                    // Skip disabled colliders
                    if (!hit.collider.enabled)
                    {
                        Debug.Log($"Carousel: Skipping disabled collider: {hit.collider.gameObject.name}");
                        continue;
                    }

                    // Skip InteractionZone colliders
                    if (hit.collider.GetComponent<InteractionZone>() != null)
                    {
                        Debug.Log($"Carousel: Skipping InteractionZone hit: {hit.collider.gameObject.name}");
                        continue;
                    }

                    // Skip dial/phone colliders (they handle their own input)
                    if (hit.collider.GetComponent<DialRotaryPhone>() != null ||
                        hit.collider.GetComponentInParent<DialRotaryPhone>() != null)
                    {
                        Debug.Log($"Carousel: Skipping DialRotaryPhone hit: {hit.collider.gameObject.name}");
                        continue;
                    }

                    // Skip lever colliders (they handle their own input)
                    if (hit.collider.GetComponent<MyLever>() != null ||
                        hit.collider.GetComponentInParent<MyLever>() != null)
                    {
                        Debug.Log($"Carousel: Skipping MyLever hit: {hit.collider.gameObject.name}");
                        continue;
                    }

                    // Skip UI Canvas elements (like rolling heads)
                    if (hit.collider.GetComponentInParent<Canvas>() != null)
                    {
                        Debug.Log($"Carousel: Skipping Canvas/UI element hit: {hit.collider.gameObject.name}");
                        continue;
                    }

                    // Check if this hit belongs to the centered object
                    Transform hitTransform = hit.collider.transform;
                    bool belongsToCenteredObject = false;

                    while (hitTransform != null)
                    {
                        if (hitTransform.gameObject == centeredObject)
                        {
                            belongsToCenteredObject = true;
                            Debug.Log($"Carousel: Hit on {hit.collider.gameObject.name} BELONGS to centered object {centeredObject.name}");
                            break;
                        }
                        hitTransform = hitTransform.parent;
                    }

                    if (belongsToCenteredObject)
                    {
                        Debug.Log($"Carousel: *** ACCEPTING CLICK on centered object {centeredObject.name} ***");
                        validHit = hit;
                        break;
                    }
                    else
                    {
                        Debug.Log($"Carousel: Hit on {hit.collider.gameObject.name} does NOT belong to centered object");
                    }
                }

                // ALWAYS try to get GameManager if null
                if (gameManager == null)
                {
                    Debug.LogWarning("Carousel: GameManager was NULL, attempting multiple recovery methods...");

                    // Try Instance first
                    gameManager = GameManager.Instance;

                    // If still null, try FindFirstObjectByType
                    if (gameManager == null)
                    {
                        gameManager = FindFirstObjectByType<GameManager>();
                        Debug.Log($"Tried FindFirstObjectByType, result: {(gameManager != null ? "FOUND" : "NULL")}");
                    }

                    // If still null, try FindAnyObjectByType
                    if (gameManager == null)
                    {
                        gameManager = FindAnyObjectByType<GameManager>();
                        Debug.Log($"Tried FindAnyObjectByType, result: {(gameManager != null ? "FOUND" : "NULL")}");
                    }
                }

                if (validHit.HasValue)
                {
                    Debug.Log($"Carousel: Click accepted on {centeredObject.name} (hit: {validHit.Value.collider.gameObject.name})");

                    if (gameManager == null)
                    {
                        Debug.LogError("Carousel: GameManager is NULL even after all recovery attempts! Cannot hand control.");
                        Debug.LogError("Make sure you have a GameObject with the GameManager script in your scene!");
                    }
                    else
                    {
                        Debug.Log($"Carousel: GameManager found: {gameManager.gameObject.name}");
                        Debug.Log("Carousel: Calling GameManager.OnCarouselObjectClicked");
                        gameManager.OnCarouselObjectClicked(centeredObject);
                    }
                }
                else
                {
                    Debug.Log("Carousel: No valid hit found - checking if fallback is appropriate");

                    // FALLBACK: Only use if the centered object actually has a monitor
                    // Check if there's a ClickableMonitor component
                    ClickableMonitor monitor = centeredObject.GetComponentInChildren<ClickableMonitor>();
                    if (monitor != null)
                    {
                        Debug.Log($"Carousel: FALLBACK - Opening centered object {centeredObject.name} (has monitor)");
                        if (gameManager != null)
                        {
                            gameManager.OnCarouselObjectClicked(centeredObject);
                        }
                    }
                    else
                    {
                        Debug.Log($"Carousel: NO FALLBACK - Centered object {centeredObject.name} has no monitor, ignoring click");
                    }
                }
            }
        }
        else
        {
            Debug.Log($"*** NEITHER SWIPE NOR CLICK *** - distance: {dragDistance}, time: {dragTime}");
        }
    }

    void ArrangeObjects()
    {
        if (carouselObjects == null || carouselObjects.Length == 0) return;
        
        for (int i = 0; i < carouselObjects.Length; i++)
        {
            if (carouselObjects[i] == null) continue;
            
            float angle = currentAngle + (i * angleStep);
            float angleRad = angle * Mathf.Deg2Rad;
            
            float x = Mathf.Sin(angleRad) * radius;
            float y = Mathf.Cos(angleRad) * radius;
            
            Vector3 targetPosition = transform.position + new Vector3(x, y, 0);
            carouselObjects[i].transform.position = targetPosition;
        }
    }

    public GameObject GetCenteredObject()
    {
        if (carouselObjects == null || carouselObjects.Length == 0)
        {
            return null;
        }

        // Find the object that's actually visually closest to center (angle closest to 0)
        Camera cam = Camera.main;
        if (cam == null) return carouselObjects[currentIndex];

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);
        float minDistance = float.MaxValue;
        GameObject closestObject = carouselObjects[0];

        foreach (GameObject obj in carouselObjects)
        {
            if (obj == null) continue;

            Vector3 screenPos = cam.WorldToScreenPoint(obj.transform.position);
            float distance = Vector2.Distance(new Vector2(screenPos.x, screenPos.y), new Vector2(screenCenter.x, screenCenter.y));

            if (distance < minDistance)
            {
                minDistance = distance;
                closestObject = obj;
            }
        }

        return closestObject;
    }

    public bool IsCarouselMoving()
    {
        return isSnapping || isDragging;
    }
}