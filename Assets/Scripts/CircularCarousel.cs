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
    public float clickCooldown = 0.5f;

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
    
    bool IsPositionOnDial(Vector2 screenPosition)
    {
        if (dialObject == null) return false;

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Check if we hit the dial object or any of its children
            Transform hitTransform = hit.collider.transform;
            while (hitTransform != null)
            {
                if (hitTransform.gameObject == dialObject)
                {
                    Debug.Log($"Carousel: Click is on dial object - ignoring input");
                    return true;
                }
                hitTransform = hitTransform.parent;
            }
        }

        return false;
    }

    void HandleInput()
    {
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

                // Check if touching the dial - if so, ignore this input
                if (IsPositionOnDial(touchPosition))
                {
                    return;
                }

                isDragging = true;
                isSnapping = false;
                dragStartPosition = touchPosition;
                dragStartTime = Time.time;
                Debug.Log($">>> TOUCH DRAG STARTED at {dragStartPosition}, time: {dragStartTime}");
                inputHandled = true;
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

                    // Check if clicking the dial - if so, ignore this input
                    if (IsPositionOnDial(mousePosition))
                    {
                        return;
                    }

                    isDragging = true;
                    isSnapping = false;
                    dragStartPosition = mousePosition;
                    dragStartTime = Time.time;
                    Debug.Log($">>> MOUSE DRAG STARTED at {dragStartPosition}, time: {dragStartTime}");
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

        // Check if it's a swipe
        if (dragDistance > swipeThreshold && dragTime < swipeTimeWindow)
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

            Debug.Log($"Snapping to object {currentIndex}, angle: {targetAngle}°");
        }
        else if (dragDistance < clickThreshold)
        {
            // Check cooldown period
            float timeSinceMonitorClose = Time.time - lastMonitorCloseTime;
            if (timeSinceMonitorClose < clickCooldown)
            {
                Debug.Log($"*** CLICK IGNORED - Still in cooldown period ({timeSinceMonitorClose:F2}s < {clickCooldown}s) ***");
                return;
            }

            Debug.Log($"*** CLICK DETECTED *** - distance: {dragDistance}");
            // CLICK detected - check if centered object was clicked
            GameObject centeredObject = GetCenteredObject();
            Debug.Log($"Carousel: currentIndex={currentIndex}, centered object={centeredObject?.name}");

            if (centeredObject != null)
            {
                // Check if we actually clicked on the centered object
                Ray ray = Camera.main.ScreenPointToRay(releasePosition);
                RaycastHit hit;

                if (Physics.Raycast(ray, out hit, 1000f))
                {
                    Debug.Log($"Carousel: Raycast hit {hit.collider.gameObject.name}");

                    // Check if hit object belongs to the centered wrapper by walking up hierarchy
                    Transform hitTransform = hit.collider.transform;
                    bool belongsToCenteredObject = false;

                    while (hitTransform != null)
                    {
                        if (hitTransform.gameObject == centeredObject)
                        {
                            belongsToCenteredObject = true;
                            break;
                        }
                        hitTransform = hitTransform.parent;
                    }

                    if (belongsToCenteredObject)
                    {
                        Debug.Log($"Carousel: Click accepted on {centeredObject.name}");

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
                        Debug.Log($"Carousel: Click rejected - {hit.collider.gameObject.name} does not belong to centered wrapper {centeredObject.name}");
                    }
                }
                else
                {
                    Debug.Log("Carousel: Raycast hit nothing");
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
}