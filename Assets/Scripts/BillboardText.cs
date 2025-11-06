using UnityEngine;

public class BillboardText : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = Camera.main;
        if (mainCamera == null)
        {
            Debug.LogError("BillboardText: No main camera found!");
        }
    }

    void LateUpdate()
    {
        if (mainCamera != null)
        {
            // Make the text face the camera
            transform.rotation = mainCamera.transform.rotation;
        }
    }
}
