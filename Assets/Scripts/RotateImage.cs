using UnityEngine;

public class RotateImage : MonoBehaviour
{
    [Header("Rotation Settings")]
    [Tooltip("Rotation speed in degrees per second (positive = anticlockwise)")]
    public float rotationSpeed = 45f;

    [Tooltip("If true, rotation only happens when the image is active")]
    public bool onlyRotateWhenActive = true;

    void Update()
    {
        if (onlyRotateWhenActive && !gameObject.activeSelf)
        {
            return;
        }

        // Rotate anticlockwise around Z axis (positive rotation)
        transform.Rotate(0f, 0f, rotationSpeed * Time.deltaTime);
    }
}
