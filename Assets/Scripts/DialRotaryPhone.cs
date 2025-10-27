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

    [Tooltip("Required rotation angle to register a dial (in degrees)")]
    public float requiredRotation = 60f;

    [Tooltip("Angle range for each number position (in degrees)")]
    public float numberAngleRange = 30f;  // 360° / 12 positions

    [Tooltip("Angle offset to align with your dial (adjust this to calibrate)")]
    public float angleOffset = 0f;

    // Static flag to tell other scripts that dial is being used
    public static bool IsDialActive { get; private set; } = false;

    private bool isDragging = false;
    private Vector3 originalRotation;
    private float currentRotation = 0f;
    private Vector2 lastMousePosition;
    private Camera mainCamera;
    private float totalRotationThisDrag = 0f;
    private int selectedDigit = -1;

    void Start()
    {
        // Store the original rotation
        originalRotation = transform.eulerAngles;
        mainCamera = Camera.main;

        if (mainCamera == null)
        {
            Debug.LogError("DialRotaryPhone: No main camera found!");
        }
    }

    void Update()
    {
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
        Ray ray = mainCamera.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        if (Physics.Raycast(ray, out hit))
        {
            // Check if we hit this object or any of its children
            Transform hitTransform = hit.collider.transform;
            while (hitTransform != null)
            {
                if (hitTransform == transform)
                {
                    return true;
                }
                hitTransform = hitTransform.parent;
            }
        }

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
        RaycastHit hit;

        Debug.Log($"GetDigitFromPosition: Raycasting from {screenPosition}");

        if (Physics.Raycast(ray, out hit, 1000f))
        {
            // Check if the hit object's name contains a digit
            string hitName = hit.collider.gameObject.name;
            Debug.Log($"GetDigitFromPosition: Hit object '{hitName}' at distance {hit.distance}");

            // Check the entire hierarchy for a digit in the name
            Transform current = hit.collider.transform;
            while (current != null)
            {
                string objName = current.gameObject.name;
                Debug.Log($"Checking hierarchy: '{objName}'");

                // Try to parse the digit from the object name
                for (int i = 0; i <= 9; i++)
                {
                    if (objName.Contains(i.ToString()))
                    {
                        Debug.Log($"Found digit: {i} in '{objName}'");
                        return i;
                    }
                }

                current = current.parent;
            }

            Debug.Log($"No digit found in hierarchy of '{hitName}'");
            return -1;
        }

        Debug.Log("GetDigitFromPosition: Raycast hit nothing");
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

        // Append digit to phone number display
        string oldText = phoneNumberText.text;
        phoneNumberText.text += selectedDigit.ToString();
        Debug.Log($"DialRotaryPhone: Dialed {selectedDigit} (rotation: {totalRotationThisDrag:F1}°)");
        Debug.Log($"Text changed from '{oldText}' to '{phoneNumberText.text}'");
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
            phoneNumberText.text = "";
            Debug.Log("DialRotaryPhone: Phone number cleared");
        }
    }

    // Public method to get the current phone number
    public string GetPhoneNumber()
    {
        return phoneNumberText != null ? phoneNumberText.text : "";
    }
}
