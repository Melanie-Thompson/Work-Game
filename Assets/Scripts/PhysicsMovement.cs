using UnityEngine;
using UnityEngine.InputSystem;

public class PhysicsMovement : MonoBehaviour
{
    public Light greenLight;
    public float goalZPosition = 10f;
    public float goalTolerance = 0.5f;
    public float zOffset = 10f;
    public float wallBuffer = 0.08f;
    public float radiusMultiplier = 0.8f;
    public float maxMovePerFrame = 0.5f; // Limit movement per frame to reduce popping
    public float dualAxisThreshold = 0.15f; // Allow both axes to move if total movement is below this
    
    private float lockedY;
    private Quaternion lockedRotation;
    private bool isDragging = false;
    private Vector3 offset;
    private float colliderRadius;
    private int lockedAxis = -1; // -1 = none, 0 = X, 1 = Z
    private Vector3 lastDragPosition;

    void Start()
    {
        lockedY = transform.position.y;
        lockedRotation = transform.rotation;
        
        MeshRenderer meshRenderer = GetComponent<MeshRenderer>();
        if (meshRenderer != null)
        {
            colliderRadius = meshRenderer.bounds.extents.x * radiusMultiplier;
        }
        else
        {
            colliderRadius = 0.3f;
        }
        
        if (greenLight != null)
        {
            greenLight.enabled = false;
        }
    }

    void Update()
    {
        // Mouse
        if (Mouse.current != null)
        {
            if (Mouse.current.leftButton.wasPressedThisFrame)
            {
                Ray ray = Camera.main.ScreenPointToRay(Mouse.current.position.ReadValue());
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit) && hit.transform == transform)
                {
                    isDragging = true;
                    lockedAxis = -1; // Reset axis lock on new drag
                    lastDragPosition = transform.position;
                    Vector3 mouseWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(Mouse.current.position.ReadValue().x, Mouse.current.position.ReadValue().y, zOffset));
                    offset = transform.position - mouseWorldPos;
                }
            }
            
            if (isDragging && Mouse.current.leftButton.isPressed)
            {
                Vector2 mousePos = Mouse.current.position.ReadValue();
                Vector3 targetPos = Camera.main.ScreenToWorldPoint(new Vector3(mousePos.x, mousePos.y, zOffset)) + offset;
                targetPos.y = lockedY;
                
                MoveTowards(targetPos);
            }
            
            if (Mouse.current.leftButton.wasReleasedThisFrame)
            {
                isDragging = false;
                lockedAxis = -1; // Clear axis lock
            }
        }

        // Touch
        if (Touchscreen.current != null && Touchscreen.current.primaryTouch.press.isPressed)
        {
            var touch = Touchscreen.current.primaryTouch;
            var phase = touch.phase.ReadValue();
            
            if (phase == UnityEngine.InputSystem.TouchPhase.Began)
            {
                Vector2 touchPos = touch.position.ReadValue();
                Ray ray = Camera.main.ScreenPointToRay(touchPos);
                RaycastHit hit;
                if (Physics.Raycast(ray, out hit) && hit.transform == transform)
                {
                    isDragging = true;
                    lockedAxis = -1; // Reset axis lock on new drag
                    lastDragPosition = transform.position;
                    Vector3 touchWorldPos = Camera.main.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, zOffset));
                    offset = transform.position - touchWorldPos;
                }
            }
            else if (isDragging && phase == UnityEngine.InputSystem.TouchPhase.Moved)
            {
                Vector2 touchPos = touch.position.ReadValue();
                Vector3 targetPos = Camera.main.ScreenToWorldPoint(new Vector3(touchPos.x, touchPos.y, zOffset)) + offset;
                targetPos.y = lockedY;
                
                MoveTowards(targetPos);
            }
            else if (phase == UnityEngine.InputSystem.TouchPhase.Ended)
            {
                isDragging = false;
                lockedAxis = -1; // Clear axis lock
            }
        }
        
        transform.rotation = lockedRotation;
        
        if (greenLight != null)
        {
            greenLight.enabled = Mathf.Abs(transform.position.z - goalZPosition) <= goalTolerance;
        }
    }

    void MoveTowards(Vector3 targetPos)
    {
        Vector3 startPos = transform.position;
        Vector3 movement = targetPos - startPos;

        // Clamp movement to prevent huge jumps
        if (movement.magnitude > maxMovePerFrame)
        {
            movement = movement.normalized * maxMovePerFrame;
            targetPos = startPos + movement;
        }

        if (movement.magnitude < 0.001f) return;

        Vector3 finalPos = startPos;

        // Track the movement delta from last frame
        Vector3 dragDelta = startPos - lastDragPosition;

        // Allow dual-axis movement for small distances (corner navigation)
        // Use single-axis movement for larger distances
        bool allowDualAxis = movement.magnitude <= dualAxisThreshold;

        // Once an axis is locked, keep it locked unless movement is very small
        if (lockedAxis == -1 && !allowDualAxis)
        {
            // Determine which axis to lock to
            lockedAxis = Mathf.Abs(movement.x) >= Mathf.Abs(movement.z) ? 0 : 1;
        }
        else if (allowDualAxis)
        {
            // Clear lock when movement is small enough
            lockedAxis = -1;
        }

        if (allowDualAxis)
        {
            // Small movement - allow both axes (original corner-navigation logic)
            // Try X movement
            if (Mathf.Abs(movement.x) > 0.001f)
            {
                Vector3 xDir = new Vector3(Mathf.Sign(movement.x), 0, 0);
                float xDist = Mathf.Abs(movement.x);

                RaycastHit xHit;
                if (Physics.SphereCast(startPos, colliderRadius, xDir, out xHit, xDist))
                {
                    if (xHit.collider.gameObject != gameObject && xHit.collider.gameObject.name != "Plane")
                    {
                        float stopDist = Mathf.Max(0, xHit.distance - wallBuffer);
                        finalPos.x = startPos.x + xDir.x * stopDist;
                    }
                    else
                    {
                        finalPos.x = targetPos.x;
                    }
                }
                else
                {
                    finalPos.x = targetPos.x;
                }
            }

            // Try Z movement from updated position
            if (Mathf.Abs(movement.z) > 0.001f)
            {
                Vector3 zDir = new Vector3(0, 0, Mathf.Sign(movement.z));
                float zDist = Mathf.Abs(movement.z);

                RaycastHit zHit;
                if (Physics.SphereCast(finalPos, colliderRadius, zDir, out zHit, zDist))
                {
                    if (zHit.collider.gameObject != gameObject && zHit.collider.gameObject.name != "Plane")
                    {
                        float stopDist = Mathf.Max(0, zHit.distance - wallBuffer);
                        finalPos.z = finalPos.z + zDir.z * stopDist;
                    }
                    else
                    {
                        finalPos.z = targetPos.z;
                    }
                }
                else
                {
                    finalPos.z = targetPos.z;
                }
            }
        }
        else
        {
            // Larger movement - restrict to single axis
            // Use locked axis if set, otherwise determine from movement
            bool moveInX = (lockedAxis == 0);

            if (moveInX && Mathf.Abs(movement.x) > 0.001f)
            {
                // Move only in X axis
                Vector3 xDir = new Vector3(Mathf.Sign(movement.x), 0, 0);
                float xDist = Mathf.Abs(movement.x);

                RaycastHit xHit;
                if (Physics.SphereCast(startPos, colliderRadius, xDir, out xHit, xDist))
                {
                    if (xHit.collider.gameObject != gameObject && xHit.collider.gameObject.name != "Plane")
                    {
                        float stopDist = Mathf.Max(0, xHit.distance - wallBuffer);
                        finalPos.x = startPos.x + xDir.x * stopDist;
                    }
                    else
                    {
                        finalPos.x = targetPos.x;
                    }
                }
                else
                {
                    finalPos.x = targetPos.x;
                }
            }
            else if (!moveInX && Mathf.Abs(movement.z) > 0.001f)
            {
                // Move only in Z axis
                Vector3 zDir = new Vector3(0, 0, Mathf.Sign(movement.z));
                float zDist = Mathf.Abs(movement.z);

                RaycastHit zHit;
                if (Physics.SphereCast(startPos, colliderRadius, zDir, out zHit, zDist))
                {
                    if (zHit.collider.gameObject != gameObject && zHit.collider.gameObject.name != "Plane")
                    {
                        float stopDist = Mathf.Max(0, zHit.distance - wallBuffer);
                        finalPos.z = startPos.z + zDir.z * stopDist;
                    }
                    else
                    {
                        finalPos.z = targetPos.z;
                    }
                }
                else
                {
                    finalPos.z = targetPos.z;
                }
            }
        }

        lastDragPosition = finalPos;
        transform.position = finalPos;
    }
}