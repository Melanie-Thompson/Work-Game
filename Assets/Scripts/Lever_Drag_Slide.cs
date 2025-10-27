using UnityEngine;
using UnityEngine.InputSystem;

public class MyLever : MonoBehaviour
{
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
    }
    
    void Update()
    {
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
                inputHandled = true;
            }

            if (touch.press.isPressed)
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
                inputPosition = mouse.position.ReadValue();

                if (mouse.leftButton.wasPressedThisFrame)
                {
                    inputPressed = true;
                }

                if (mouse.leftButton.isPressed)
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
            Ray ray = mainCamera.ScreenPointToRay(inputPosition);

            if (myCollider != null && myCollider.bounds.IntersectRay(ray))
            {
                dragging = true;
                lastMouseY = inputPosition.y;
                lastMouseX = inputPosition.x;
                targetRotation = transform.localRotation.eulerAngles.x;
                if (targetRotation > 180) targetRotation -= 360;

                // Capture current slide position when starting drag
                targetSlidePosition = transform.localPosition.x;
            }
        }

        // Handle input hold (dragging)
        if (dragging && inputHeld)
        {
            float currentMouseY = inputPosition.y;
            float currentMouseX = inputPosition.x;

            // Vertical rotation (existing)
            float deltaY = currentMouseY - lastMouseY;
            targetRotation += deltaY * rotationSpeed;
            targetRotation = Mathf.Clamp(targetRotation, minRotation, maxRotation);
            transform.localRotation = Quaternion.Euler(targetRotation, 0, 0);

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

        // Handle input release
        if (inputReleased)
        {
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

        // Only turn light ON when lever is very close to maxRotation (-45)
        if (targetRotation >= (maxRotation - activationThreshold))
        {
            // Lever is pushed forward enough
            if (!controlledLight.enabled)
            {
                controlledLight.enabled = true;
                UpdateScore(100);  // ADD THIS LINE - adds 100 points when light turns on
            }
        }
        else
        {
            // Lever not pushed forward enough
            if (controlledLight.enabled)
            {
                controlledLight.enabled = false;
            }
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

    void OnDrawGizmos()
    {
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = Color.green;
            Gizmos.DrawWireCube(col.bounds.center, col.bounds.size);
        }
    }
}