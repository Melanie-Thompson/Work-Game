using UnityEngine;
using UnityEngine.InputSystem;
using TMPro;

public class DialRotaryPhone : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Speed at which the dial returns to original position")]
    public float returnSpeed = 5f;

    [Tooltip("Maximum rotation angle in degrees (clockwise)")]
    public float maxRotationAngle = 360f;

    [Header("Drag Settings")]
    [Tooltip("Distance from center affects rotation sensitivity")]
    public float rotationSensitivity = 1f;

    [Header("Phone Number Display")]
    [Tooltip("TextMeshPro component to display the phone number")]
    public TMP_Text phoneNumberText;

    [Tooltip("GameObject containing the phone call icon (will be shown when number is ready to dial)")]
    public GameObject phoneCallIcon;

    [Tooltip("GameObject containing the phone hang-up icon (will be shown when call is in progress)")]
    public GameObject phoneHangUpIcon;

    [Tooltip("Maximum number of digits (not including the # prefix)")]
    public int maxDigits = 3;

    [Tooltip("Required rotation angle to register a dial (in degrees)")]
    public float requiredRotation = 60f;

    [Tooltip("Angle range for each number position (in degrees)")]
    public float numberAngleRange = 30f;  // 360° / 12 positions

    [Tooltip("Angle offset to align with your dial (adjust this to calibrate)")]
    public float angleOffset = 0f;

    // Static flag to tell other scripts that dial is being used
    public static bool IsDialActive { get; private set; } = false;

    // Static flag to track if a call is currently in progress
    public static bool IsCallInProgress { get; private set; } = false;

    // Static flag to track if dialogue is currently being shown (prevents hang-up icon from showing)
    public static bool IsDialogueActive { get; set; } = false;

    private bool isDragging = false;
    private Vector3 originalRotation;
    private float currentRotation = 0f;
    private Vector2 lastMousePosition;
    private Camera mainCamera;
    private float totalRotationThisDrag = 0f;
    private int selectedDigit = -1;
    private CircularCarousel carousel;
    private GameObject myCarouselWrapper;
    private InteractionZone interactionZone;
    private bool lastCenteredState = false;
    private float lastVisibilityChangeTime = 0f;
    private const float VISIBILITY_CHANGE_COOLDOWN = 0.2f; // Prevent rapid toggling

    void Start()
    {
        // Store the original rotation
        originalRotation = transform.eulerAngles;
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("DialRotaryPhone: No main camera found!");
        }

        // Find the carousel
        carousel = FindFirstObjectByType<CircularCarousel>();

        // Find my carousel wrapper by looking for a parent that's in the carousel array
        if (carousel != null)
        {
            myCarouselWrapper = FindMyCarouselWrapper();
            if (myCarouselWrapper != null)
            {
                Debug.Log($"DialRotaryPhone: Found carousel wrapper: {myCarouselWrapper.name}");
            }
            else
            {
                Debug.LogWarning($"DialRotaryPhone: Could not find carousel wrapper in parent hierarchy!");
            }
        }

        // Initialize phone number with # prefix
        if (phoneNumberText != null)
        {
            phoneNumberText.text = "#";
            Debug.Log("DialRotaryPhone: Initialized phone number with # prefix");
        }

        // Initially hide phone number text if not centered
        UpdatePhoneNumberVisibility();

        // Find interaction zone in the scene or parent hierarchy
        interactionZone = GetComponentInParent<InteractionZone>();
        if (interactionZone == null)
        {
            interactionZone = FindFirstObjectByType<InteractionZone>();
        }

        if (interactionZone != null)
        {
            Debug.Log($"DialRotaryPhone '{gameObject.name}': Found interaction zone: {interactionZone.gameObject.name}");
        }
        else
        {
            Debug.LogWarning($"DialRotaryPhone '{gameObject.name}': No InteractionZone found - will accept input from anywhere");
        }
    }

    void Update()
    {
        // Check if dialogue is active - block dial input (but allow phone icon clicks)
        if (IsDialogueActive)
        {
            if (isDragging)
            {
                isDragging = false;
                IsDialActive = false;
            }
            // Don't return - allow phone icon visibility update
        }

        // Check if work shift is complete - if so, block ALL input
        if (GameManager.Instance != null && GameManager.Instance.IsWorkShiftComplete())
        {
            if (isDragging)
            {
                isDragging = false;
                IsDialActive = false;
            }
            return;
        }

        // Check if GameManager says we shouldn't be processing input
        if (GameManager.Instance != null && !GameManager.Instance.IsCarouselActive())
        {
            // Carousel is not active (e.g., monitor is zoomed in), don't process input
            return;
        }

        // Update phone number text visibility based on carousel position
        UpdatePhoneNumberVisibility();

        // Update phone icon visibility based on carousel position
        UpdatePhoneIconVisibility();

        HandleInput();

        // If not dragging, return to original position
        if (!isDragging && currentRotation != 0f)
        {
            currentRotation = Mathf.Lerp(currentRotation, 0f, Time.deltaTime * returnSpeed);

            // Snap to zero when very close
            if (Mathf.Abs(currentRotation) < 0.1f)
            {
                currentRotation = 0f;
                totalRotationThisDrag = 0f; // Reset for next dial
                selectedDigit = -1;
            }

            ApplyRotation();
        }
    }

    void HandleInput()
    {
        // Block dial input if dialogue is active
        if (IsDialogueActive)
        {
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

                if (IsTouchingDial(touchPosition))
                {
                    isDragging = true;
                    IsDialActive = true;
                    lastMousePosition = touchPosition;
                    totalRotationThisDrag = 0f;
                    selectedDigit = GetDigitFromPosition(touchPosition);
                    inputHandled = true;
                    Debug.Log($"DialRotaryPhone: Touch drag started on digit {selectedDigit}");
                }
            }

            if (touch.press.isPressed && isDragging)
            {
                Vector2 currentPosition = touch.position.ReadValue();
                ProcessDrag(currentPosition);
                inputHandled = true;
            }

            if (touch.press.wasReleasedThisFrame && isDragging)
            {
                isDragging = false;
                IsDialActive = false;
                inputHandled = true;
                SendDigitToDisplay();
                Debug.Log("DialRotaryPhone: Touch drag released");
            }
        }

        // If no touch input, check mouse input (desktop)
        if (!inputHandled)
        {
            var mouse = Mouse.current;
            if (mouse != null)
            {
                if (mouse.leftButton.wasPressedThisFrame)
                {
                    Vector2 mousePosition = mouse.position.ReadValue();
                    Debug.Log($"*** MOUSE CLICKED at {mousePosition} ***");

                    if (IsTouchingDial(mousePosition))
                    {
                        Debug.Log($"*** TOUCHING DIAL - calling GetDigitFromPosition ***");

                        isDragging = true;
                        IsDialActive = true;
                        lastMousePosition = mousePosition;
                        totalRotationThisDrag = 0f;
                        selectedDigit = GetDigitFromPosition(mousePosition);
                        Debug.Log($"DialRotaryPhone: Mouse drag started on digit {selectedDigit}");
                    }
                }

                if (mouse.leftButton.isPressed && isDragging)
                {
                    Vector2 currentPosition = mouse.position.ReadValue();
                    ProcessDrag(currentPosition);
                }

                if (mouse.leftButton.wasReleasedThisFrame && isDragging)
                {
                    isDragging = false;
                    IsDialActive = false;
                    SendDigitToDisplay();
                    Debug.Log("DialRotaryPhone: Mouse drag released");
                }
            }
        }
    }

    bool IsTouchingDial(Vector2 screenPosition)
    {
        // First check if input is within interaction zone
        if (interactionZone != null && !interactionZone.IsPositionInZone(screenPosition))
        {
            Debug.Log($"DialRotaryPhone: Input rejected - outside interaction zone");
            return false;
        }

        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

        Debug.Log($"IsTouchingDial: Found {hits.Length} raycast hits");

        // Check ALL objects hit by the ray - looking specifically for number colliders
        foreach (RaycastHit hit in hits)
        {
            // Skip InteractionZone colliders
            if (hit.collider.GetComponent<InteractionZone>() != null)
            {
                Debug.Log($"  -> Skipping InteractionZone: {hit.collider.gameObject.name}");
                continue;
            }

            Debug.Log($"  -> Hit: {hit.collider.gameObject.name}");

            // Check if the hit object's name contains "number" (like "0 number", "1 number", etc)
            string hitName = hit.collider.gameObject.name.ToLower();
            if (hitName.Contains("number"))
            {
                Debug.Log($"*** DIAL MATCH FOUND! Clicked on number: {hit.collider.gameObject.name} ***");
                return true;
            }

            // Also check if we hit a child of this transform that's a number
            Transform hitTransform = hit.collider.transform;
            while (hitTransform != null && hitTransform != transform.parent)
            {
                string objName = hitTransform.gameObject.name.ToLower();
                if (objName.Contains("number") && hitTransform.IsChildOf(transform))
                {
                    Debug.Log($"*** DIAL MATCH FOUND! Clicked on dial child: {hitTransform.gameObject.name} ***");
                    return true;
                }
                hitTransform = hitTransform.parent;
            }
        }

        Debug.Log("IsTouchingDial: No dial match found");
        return false;
    }

    void ProcessDrag(Vector2 currentPosition)
    {
        // Get the center of the dial in screen space
        Vector3 screenCenter = mainCamera.WorldToScreenPoint(transform.position);
        Vector2 dialCenter = new Vector2(screenCenter.x, screenCenter.y);

        // Calculate angles from center to previous and current positions
        Vector2 lastDir = lastMousePosition - dialCenter;
        Vector2 currentDir = currentPosition - dialCenter;

        // Calculate the angle difference (negated to match swipe direction)
        float angle = -Vector2.SignedAngle(lastDir, currentDir);

        // Only allow counter-clockwise rotation (positive angle in Unity's coordinate system)
        if (angle > 0)
        {
            currentRotation += angle * rotationSensitivity;

            // Clamp rotation to max angle
            currentRotation = Mathf.Clamp(currentRotation, 0f, maxRotationAngle);

            // Track total rotation during this drag
            totalRotationThisDrag += angle * rotationSensitivity;

            ApplyRotation();
        }

        lastMousePosition = currentPosition;
    }

    int GetDigitFromPosition(Vector2 screenPosition)
    {
        // Raycast to find which number collider was hit
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);

        Debug.Log($"GetDigitFromPosition: Raycasting from {screenPosition}, found {hits.Length} hits");

        // Check ALL hits to find a number collider
        foreach (RaycastHit hit in hits)
        {
            // Skip InteractionZone colliders
            if (hit.collider.GetComponent<InteractionZone>() != null)
            {
                Debug.Log($"GetDigitFromPosition: Skipping InteractionZone: {hit.collider.gameObject.name}");
                continue;
            }

            Debug.Log($"GetDigitFromPosition: Checking hit '{hit.collider.gameObject.name}'");

            // Check the entire hierarchy for a digit in the name
            Transform current = hit.collider.transform;
            while (current != null)
            {
                string objName = current.gameObject.name;
                Debug.Log($"  Checking hierarchy: '{objName}'");

                // Try to parse the digit from the object name
                for (int i = 0; i <= 9; i++)
                {
                    if (objName.Contains(i.ToString()))
                    {
                        Debug.Log($"*** FOUND DIGIT: {i} in '{objName}' ***");
                        return i;
                    }
                }

                current = current.parent;
            }
        }

        Debug.Log("GetDigitFromPosition: No digit found in any hits");
        return -1;
    }

    void SendDigitToDisplay()
    {
        Debug.Log($"SendDigitToDisplay called - selectedDigit: {selectedDigit}, totalRotation: {totalRotationThisDrag:F1}°");

        if (phoneNumberText == null)
        {
            Debug.LogError("DialRotaryPhone: No phone number text assigned!");
            return;
        }

        if (selectedDigit < 0)
        {
            Debug.Log("DialRotaryPhone: No digit was selected (empty position)");
            return;
        }

        // Only send digit if rotation exceeded required threshold
        if (totalRotationThisDrag < requiredRotation)
        {
            Debug.Log($"DialRotaryPhone: Rotation too small ({totalRotationThisDrag:F1}°), digit not registered (required: {requiredRotation}°)");
            return;
        }

        // Check if we've already reached max digits (excluding the # prefix)
        int currentDigitCount = phoneNumberText.text.Length - 1; // Subtract 1 for the #
        if (currentDigitCount >= maxDigits)
        {
            Debug.Log($"DialRotaryPhone: Max digits ({maxDigits}) already reached, ignoring input");
            return;
        }

        // Append digit to phone number display
        string oldText = phoneNumberText.text;
        phoneNumberText.text += selectedDigit.ToString();
        Debug.Log($"DialRotaryPhone: Dialed {selectedDigit} (rotation: {totalRotationThisDrag:F1}°)");
        Debug.Log($"Text changed from '{oldText}' to '{phoneNumberText.text}'");

        // Award points for dialing a digit
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(20);
            Debug.Log("DialRotaryPhone: Awarded 20 points for dialing a digit!");
        }
    }

    void ApplyRotation()
    {
        // Apply rotation around the Z-axis (assuming 2D-style rotation)
        transform.eulerAngles = originalRotation + new Vector3(0, 0, currentRotation);
    }

    // Public method to get current rotation value (useful for other scripts)
    public float GetNormalizedRotation()
    {
        // Returns 0 to 1, where 0 is original position and 1 is max rotation
        return Mathf.Abs(currentRotation) / maxRotationAngle;
    }

    // Public method to clear the phone number display
    public void ClearPhoneNumber()
    {
        if (phoneNumberText != null)
        {
            phoneNumberText.text = "#"; // Reset to # prefix
            Debug.Log("DialRotaryPhone: Phone number cleared and reset to #");
        }
    }

    // Public method to get the current phone number
    public string GetPhoneNumber()
    {
        return phoneNumberText != null ? phoneNumberText.text : "";
    }

    // Public method to start a call (called by PhoneIconClick)
    public static void StartCall()
    {
        IsCallInProgress = true;
        Debug.Log("DialRotaryPhone: Call started");
    }

    // Public method to end a call (called by PhoneIconClick when hanging up)
    public static void EndCall()
    {
        IsCallInProgress = false;
        IsDialogueActive = false; // Reset dialogue flag when call ends
        Debug.Log("DialRotaryPhone: Call ended");
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

    void UpdatePhoneNumberVisibility()
    {
        if (phoneNumberText == null)
        {
            Debug.LogWarning($"DialRotaryPhone [{gameObject.name}]: phoneNumberText is NULL!");
            return;
        }

        // Only show phone number text when this dial's wrapper is centered
        if (carousel != null && myCarouselWrapper != null)
        {
            GameObject centeredObject = carousel.GetCenteredObject();
            bool isCentered = (centeredObject == myCarouselWrapper);

            Debug.Log($"DialRotaryPhone [{gameObject.name}]: phoneNumberText='{phoneNumberText.gameObject.name}', currentlyActive={phoneNumberText.gameObject.activeSelf}, shouldBeCentered={isCentered}, CenteredObj={centeredObject?.name}, MyWrapper={myCarouselWrapper?.name}");

            // Always update visibility based on centered state
            phoneNumberText.gameObject.SetActive(isCentered);
            lastCenteredState = isCentered;

            Debug.Log($"DialRotaryPhone [{gameObject.name}]: Set phoneNumberText.gameObject.SetActive({isCentered}) - now active={phoneNumberText.gameObject.activeSelf}");
        }
        else
        {
            Debug.LogWarning($"DialRotaryPhone [{gameObject.name}]: carousel={carousel?.name ?? "NULL"}, myCarouselWrapper={myCarouselWrapper?.name ?? "NULL"} - hiding by default");
            // If we can't find the carousel, hide it by default
            phoneNumberText.gameObject.SetActive(false);
            lastCenteredState = false;
        }
    }

    void UpdatePhoneIconVisibility()
    {
        // Only show phone icons when this dial's wrapper is centered AND we have max digits
        if (carousel != null && myCarouselWrapper != null)
        {
            // Don't update visibility while carousel is moving - prevents flickering
            if (carousel.IsCarouselMoving())
            {
                return;
            }

            GameObject centeredObject = carousel.GetCenteredObject();
            bool isCentered = (centeredObject == myCarouselWrapper);

            // Check if we have the full phone number (# + maxDigits)
            int currentDigitCount = phoneNumberText != null ? phoneNumberText.text.Length - 1 : 0;
            bool hasFullNumber = currentDigitCount >= maxDigits;

            // Show icons only when centered AND has full phone number
            bool shouldShowIcons = isCentered && hasFullNumber;

            // Show the appropriate icon based on call state
            if (shouldShowIcons)
            {
                if (IsCallInProgress)
                {
                    // Only show hang-up icon if dialogue is NOT active
                    if (phoneCallIcon != null) phoneCallIcon.SetActive(false);
                    if (phoneHangUpIcon != null) phoneHangUpIcon.SetActive(!IsDialogueActive);
                }
                else
                {
                    // Show call icon when ready to dial
                    if (phoneCallIcon != null) phoneCallIcon.SetActive(true);
                    if (phoneHangUpIcon != null) phoneHangUpIcon.SetActive(false);
                }
            }
            else
            {
                // Hide both icons
                if (phoneCallIcon != null) phoneCallIcon.SetActive(false);
                if (phoneHangUpIcon != null) phoneHangUpIcon.SetActive(false);
            }
        }
        else
        {
            // If we can't find the carousel, hide both icons by default
            if (phoneCallIcon != null) phoneCallIcon.SetActive(false);
            if (phoneHangUpIcon != null) phoneHangUpIcon.SetActive(false);
        }
    }
}
