using UnityEngine;
using UnityEngine.InputSystem;

public class SpeechBubbleClickHandler : MonoBehaviour
{
    private CorporateHeadSpawner spawner;
    private CorporateHeadSpawner.PhoneNumberMapping mapping;
    private Camera mainCamera;
    private BoxCollider boxCollider;
    private int updateCount = 0;

    void Awake()
    {
        Debug.Log($"SpeechBubbleClickHandler: Awake called on {gameObject.name}");
    }

    void OnEnable()
    {
        Debug.Log($"SpeechBubbleClickHandler: OnEnable called on {gameObject.name}");
    }

    void OnDisable()
    {
        Debug.Log($"SpeechBubbleClickHandler: OnDisable called on {gameObject.name}");
    }

    public void Initialize(CorporateHeadSpawner spawner, CorporateHeadSpawner.PhoneNumberMapping mapping)
    {
        this.spawner = spawner;
        this.mapping = mapping;
        mainCamera = Camera.main;

        Debug.Log($"=== SpeechBubbleClickHandler: Initialize called ===");
        Debug.Log($"  GameObject: {gameObject.name}");
        Debug.Log($"  GameObject active: {gameObject.activeSelf}");
        Debug.Log($"  GameObject activeInHierarchy: {gameObject.activeInHierarchy}");
        Debug.Log($"  Component enabled: {enabled}");
        Debug.Log($"  Phone number: {mapping.phoneNumber}");

        // Ensure there's a BoxCollider
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            boxCollider = gameObject.AddComponent<BoxCollider>();
            Debug.Log("SpeechBubbleClickHandler: Added BoxCollider to speech bubble");
        }

        Debug.Log($"SpeechBubbleClickHandler: BoxCollider - enabled: {boxCollider.enabled}, size: {boxCollider.size}, center: {boxCollider.center}");
        Debug.Log($"SpeechBubbleClickHandler: GameObject layer: {gameObject.layer} ({LayerMask.LayerToName(gameObject.layer)})");
        Debug.Log($"SpeechBubbleClickHandler: GameObject position: {transform.position}");
        Debug.Log($"SpeechBubbleClickHandler: Setup complete - Update will start logging clicks");
    }

    void Update()
    {
        // Log every 60 frames to confirm Update is running
        updateCount++;
        if (updateCount % 60 == 0)
        {
            Debug.Log($"SpeechBubbleClickHandler: Update running on {gameObject.name}, frame {updateCount}");
        }

        // Manual click detection - check every frame
        var mouse = Mouse.current;

        // Debug: Check if mouse exists
        if (mouse == null)
        {
            if (updateCount % 300 == 0) // Log less frequently
            {
                Debug.LogError("SpeechBubbleClickHandler: Mouse.current is NULL!");
            }
            return;
        }

        if (mouse.leftButton.wasPressedThisFrame)
        {
            Vector2 mousePosition = mouse.position.ReadValue();

            Debug.Log($"=== SpeechBubbleClickHandler: MOUSE CLICK DETECTED ===");
            Debug.Log($"  Position: {mousePosition}");
            Debug.Log($"  GameObject: {gameObject.name}");
            Debug.Log($"  GameObject active: {gameObject.activeSelf}");
            Debug.Log($"  GameObject activeInHierarchy: {gameObject.activeInHierarchy}");
            Debug.Log($"  Component enabled: {enabled}");

            if (mainCamera == null)
            {
                Debug.LogError("SpeechBubbleClickHandler: mainCamera is NULL!");
                return;
            }

            Ray ray = mainCamera.ScreenPointToRay(mousePosition);
            Debug.Log($"  Ray origin: {ray.origin}, direction: {ray.direction}");

            // Raycast against ALL objects to see what we're hitting
            RaycastHit[] hits = Physics.RaycastAll(ray, 1000f);
            Debug.Log($"  Found {hits.Length} raycast hits");

            bool hitThisBubble = false;
            foreach (RaycastHit hit in hits)
            {
                Debug.Log($"    - Hit: {hit.collider.gameObject.name} at distance {hit.distance}");

                if (hit.collider == boxCollider)
                {
                    Debug.Log("*** SPEECH BUBBLE CLICKED! ***");
                    hitThisBubble = true;

                    if (spawner != null && mapping != null)
                    {
                        spawner.OnSpeechBubbleClicked(mapping);
                    }
                    else
                    {
                        Debug.LogError("SpeechBubbleClickHandler: spawner or mapping is null!");
                    }
                    break;
                }
            }

            if (!hitThisBubble && hits.Length == 0)
            {
                Debug.Log("  No objects hit by raycast");
            }
            else if (!hitThisBubble)
            {
                Debug.Log($"  Speech bubble NOT hit (boxCollider: {boxCollider?.name ?? "NULL"})");
            }
        }
    }

    void OnMouseDown()
    {
        Debug.Log("*** SPEECH BUBBLE OnMouseDown called! ***");
        if (spawner != null && mapping != null)
        {
            spawner.OnSpeechBubbleClicked(mapping);
        }
    }
}
