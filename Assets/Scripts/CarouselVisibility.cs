using UnityEngine;

public class CarouselVisibility : MonoBehaviour
{
    [Header("Visibility Settings")]
    public float visibilityThreshold = 0.3f; // How close to center to be visible (0-1)

    private CircularCarousel carousel;
    private Camera mainCamera;

    void Start()
    {
        carousel = GetComponent<CircularCarousel>();
        mainCamera = Camera.main;
    }

    void Update()
    {
        if (carousel == null || carousel.carouselObjects == null) return;

        Vector3 screenCenter = new Vector3(Screen.width / 2f, Screen.height / 2f, 0f);

        foreach (GameObject obj in carousel.carouselObjects)
        {
            if (obj == null) continue;

            // Get object's screen position
            Vector3 screenPos = mainCamera.WorldToScreenPoint(obj.transform.position);

            // Calculate distance from screen center (normalized)
            float distanceFromCenter = Vector2.Distance(
                new Vector2(screenPos.x, screenPos.y),
                new Vector2(screenCenter.x, screenCenter.y)
            );

            float normalizedDistance = distanceFromCenter / (Screen.width / 2f);

            // Show if close to center, hide if far
            bool shouldBeVisible = normalizedDistance < visibilityThreshold;

            // Disable renderers instead of SetActive to keep scripts running
            Renderer[] renderers = obj.GetComponentsInChildren<Renderer>();
            foreach (Renderer renderer in renderers)
            {
                renderer.enabled = shouldBeVisible;
            }

            // Also disable Canvas if it exists (for UI elements)
            Canvas[] canvases = obj.GetComponentsInChildren<Canvas>();
            foreach (Canvas canvas in canvases)
            {
                canvas.enabled = shouldBeVisible;
            }
        }
    }
}
