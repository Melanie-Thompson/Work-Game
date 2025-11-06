using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.EventSystems;

public class PhoneIconClick : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [Tooltip("The DialRotaryPhone script to get the phone number from")]
    public DialRotaryPhone dialPhone;

    [Header("Wobble Settings")]
    [Tooltip("Maximum rotation angle when wobbling")]
    public float wobbleAngle = 20f;

    [Tooltip("How long the wobble/ring lasts in seconds before resetting")]
    public float wobbleDuration = 1f;

    [Tooltip("How fast the wobble oscillates - high value for jittery phone effect")]
    public float wobbleSpeed = 40f;

    [Tooltip("Axis to rotate around - Z-axis for side-to-side swing like a hanging phone")]
    public Vector3 wobbleAxis = new Vector3(0, 0, 1);

    private Camera mainCamera;
    private bool isWobbling = false;
    private float wobbleTimer = 0f;
    private Quaternion originalRotation;
    private Collider phoneIconCollider;
    private CircularCarousel carousel;
    private GameObject myCarouselWrapper;

    void Start()
    {
        mainCamera = Camera.main;
        originalRotation = transform.localRotation;

        // Find the carousel
        carousel = FindFirstObjectByType<CircularCarousel>();

        // Find my carousel wrapper
        if (carousel != null)
        {
            myCarouselWrapper = FindMyCarouselWrapper();
            if (myCarouselWrapper != null)
            {
                Debug.Log($"PhoneIconClick '{gameObject.name}': Found carousel wrapper: {myCarouselWrapper.name}");
            }
        }

        // Get or add collider for clicking (UI element, so collider not needed)
        phoneIconCollider = GetComponent<Collider>();
        if (phoneIconCollider != null)
        {
            // Remove any 3D collider - we're using UI event system
            Destroy(phoneIconCollider);
            phoneIconCollider = null;
            Debug.Log("PhoneIconClick: Removed 3D collider - using UI event system instead");
        }

        // Try to find DialRotaryPhone if not assigned
        if (dialPhone == null)
        {
            dialPhone = FindFirstObjectByType<DialRotaryPhone>();
            if (dialPhone != null)
            {
                Debug.Log($"PhoneIconClick: Found DialRotaryPhone: {dialPhone.gameObject.name}");
            }
            else
            {
                Debug.LogError("PhoneIconClick: Could not find DialRotaryPhone in scene!");
            }
        }
    }

    void Update()
    {
        // Handle wobble animation
        if (isWobbling)
        {
            wobbleTimer += Time.deltaTime;

            if (wobbleTimer < wobbleDuration)
            {
                // Calculate wobble using damped sine wave (like a pendulum)
                float normalizedTime = wobbleTimer / wobbleDuration;
                float dampingFactor = 1f - normalizedTime; // Reduces over time
                float wobble = Mathf.Sin(wobbleTimer * wobbleSpeed) * wobbleAngle * dampingFactor;

                // Apply rotation around the specified axis
                transform.localRotation = originalRotation * Quaternion.AngleAxis(wobble, wobbleAxis);
            }
            else
            {
                // Wobble finished
                isWobbling = false;
                transform.localRotation = originalRotation;
                Debug.Log("PhoneIconClick: Wobble finished");
            }
        }
    }

    // Unity UI Event System callback - called when this UI element is clicked
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("PhoneIconClick: UI element clicked via Event System!");
        OnPhoneIconClicked();
    }


    void OnPhoneIconClicked()
    {
        // ONLY allow clicking when this phone's carousel item is centered
        if (carousel != null && myCarouselWrapper != null)
        {
            GameObject centeredObject = carousel.GetCenteredObject();
            if (centeredObject != myCarouselWrapper)
            {
                Debug.Log($"PhoneIconClick: Ignoring click - not centered (centered item: {centeredObject?.name})");
                return;
            }
        }

        Debug.Log("=== PhoneIconClick: Phone icon clicked! ===");

        // Check if we're hanging up or making a call
        if (DialRotaryPhone.IsCallInProgress)
        {
            // Hang up the call
            Debug.Log("PhoneIconClick: Hanging up call");
            OnHangUpClicked();
        }
        else
        {
            // Make a new call
            Debug.Log("PhoneIconClick: Making call");
            OnCallClicked();
        }
    }

    void OnCallClicked()
    {
        Debug.Log($"PhoneIconClick: Starting wobble - angle:{wobbleAngle}, speed:{wobbleSpeed}, axis:{wobbleAxis}");
        Debug.Log($"PhoneIconClick: Original rotation: {transform.localRotation.eulerAngles}");

        // Start wobble
        isWobbling = true;
        wobbleTimer = 0f;
        originalRotation = transform.localRotation;

        // Get the phone number
        string phoneNumber = "";
        if (dialPhone != null)
        {
            phoneNumber = dialPhone.GetPhoneNumber();
            Debug.Log($"PhoneIconClick: Phone number is '{phoneNumber}'");
        }
        else
        {
            Debug.LogError("PhoneIconClick: No DialRotaryPhone reference!");
        }

        // IMMEDIATELY hide this phone call icon (it's the one that was just clicked)
        gameObject.SetActive(false);
        Debug.Log("PhoneIconClick: Hidden phone call icon immediately");

        // Start the call
        DialRotaryPhone.StartCall();

        // Fire event to GameManager
        if (GameManager.Instance != null)
        {
            GameManager.Instance.OnPhoneNumberCalled(phoneNumber);
        }
        else
        {
            Debug.LogError("PhoneIconClick: GameManager.Instance is null!");
        }
    }

    void OnHangUpClicked()
    {
        Debug.Log("PhoneIconClick: Hang up clicked!");

        // Start wobble for hang-up
        isWobbling = true;
        wobbleTimer = 0f;
        originalRotation = transform.localRotation;

        // End the call
        DialRotaryPhone.EndCall();

        // Get the phone number before clearing it (so we know which Corporate Head to hide)
        string phoneNumber = "";
        if (dialPhone != null)
        {
            phoneNumber = dialPhone.GetPhoneNumber();
            dialPhone.ClearPhoneNumber();
            Debug.Log($"PhoneIconClick: Phone number '{phoneNumber}' cleared after hang-up");
        }

        // Hide the Corporate Head associated with this phone number
        if (GameManager.Instance != null)
        {
            if (GameManager.Instance.corporateHeadSpawner != null)
            {
                GameManager.Instance.corporateHeadSpawner.HideCorporateHead(phoneNumber);
                Debug.Log($"PhoneIconClick: Hiding Corporate Head for {phoneNumber}");
            }

            // Show a message
            GameManager.Instance.ShowBonusMessage("HUNG UP!", duration: 2f, priority: 5);
        }
    }

    private System.Collections.IEnumerator ResetPhoneNumberAfterRing()
    {
        // Wait for the wobble/ring to finish
        yield return new WaitForSeconds(wobbleDuration);

        // Clear the phone number
        if (dialPhone != null)
        {
            dialPhone.ClearPhoneNumber();
            Debug.Log("PhoneIconClick: Phone number cleared after ring");
        }
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the clickable area
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireCube(transform.position, transform.lossyScale);
    }

    GameObject FindMyCarouselWrapper()
    {
        Transform current = transform.parent;
        while (current != null)
        {
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
}
