using UnityEngine;

public class PositionDebugger : MonoBehaviour
{
    private Vector3 lastPosition;
    private Vector3 lastLocalPosition;

    void Start()
    {
        lastPosition = transform.position;
        lastLocalPosition = transform.localPosition;
        Debug.Log($"[{gameObject.name}] START Position: {lastPosition}, LocalPosition: {lastLocalPosition}");
    }

    void LateUpdate()
    {
        if (transform.position != lastPosition || transform.localPosition != lastLocalPosition)
        {
            Debug.LogWarning($"[{gameObject.name}] POSITION CHANGED!");
            Debug.LogWarning($"  World: {lastPosition} → {transform.position}");
            Debug.LogWarning($"  Local: {lastLocalPosition} → {transform.localPosition}");
            Debug.LogWarning($"  STACK TRACE:");
            Debug.LogWarning(System.Environment.StackTrace);

            lastPosition = transform.position;
            lastLocalPosition = transform.localPosition;
        }
    }
}
