using UnityEngine;

/// <summary>
/// Defines an interaction zone - interactive objects will only respond to input within this zone.
/// Add this component to a GameObject with a BoxCollider to create an interaction area.
/// </summary>
public class InteractionZone : MonoBehaviour
{
    [Header("Visualization")]
    [Tooltip("Show the interaction zone as a visible box on screen")]
    public bool showVisibleBox = true;

    [Tooltip("Color of the visible box")]
    public Color boxColor = new Color(1, 0, 0, 0.3f); // Red semi-transparent

    private BoxCollider boxCollider;

    void Awake()
    {
        boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogWarning($"InteractionZone '{gameObject.name}': No BoxCollider found! Adding one.");
            boxCollider = gameObject.AddComponent<BoxCollider>();
        }

        // Make collider a trigger so it doesn't block physics
        boxCollider.isTrigger = true;

        if (showVisibleBox)
        {
            CreateVisibleBox();
        }

        Debug.Log($"InteractionZone '{gameObject.name}' initialized with bounds: {boxCollider.bounds}");
    }

    void CreateVisibleBox()
    {
        // Create a cube mesh to visualize the box collider
        GameObject visualBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualBox.name = "InteractionZone_Visual";
        visualBox.transform.SetParent(transform);
        visualBox.transform.localPosition = Vector3.zero;
        visualBox.transform.localRotation = Quaternion.identity;
        visualBox.transform.localScale = Vector3.one;

        // Get the box collider size and match it
        if (boxCollider != null)
        {
            visualBox.transform.localPosition = boxCollider.center;
            visualBox.transform.localScale = boxCollider.size;
        }

        // Remove the collider from the visual box (we only want the mesh)
        Destroy(visualBox.GetComponent<BoxCollider>());

        // Set up the material to be semi-transparent
        MeshRenderer renderer = visualBox.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            // Try Unlit/Color first (simpler and more reliable)
            Shader shader = Shader.Find("Unlit/Color");
            if (shader == null)
            {
                // Fallback to Sprites/Default if available
                shader = Shader.Find("Sprites/Default");
            }
            if (shader == null)
            {
                // Final fallback to Standard
                shader = Shader.Find("Standard");
            }

            Material mat = new Material(shader);
            mat.color = boxColor;

            // Try to set up transparency
            if (mat.HasProperty("_Mode"))
            {
                mat.SetFloat("_Mode", 3); // Transparent mode
            }
            if (mat.HasProperty("_SrcBlend"))
            {
                mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
                mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            }
            if (mat.HasProperty("_ZWrite"))
            {
                mat.SetInt("_ZWrite", 0);
            }

            mat.renderQueue = 3000;
            renderer.material = mat;

            Debug.Log($"InteractionZone: Created visual box with shader: {shader.name}, color: {boxColor}");
        }
    }

    /// <summary>
    /// Check if a screen position is inside this interaction zone
    /// </summary>
    public bool IsPositionInZone(Vector2 screenPosition)
    {
        if (boxCollider == null) return true; // If no collider, allow all interactions

        Ray ray = Camera.main.ScreenPointToRay(screenPosition);
        RaycastHit hit;

        // Check if the ray hits this specific collider
        if (boxCollider.Raycast(ray, out hit, 1000f))
        {
            Debug.Log($"Position {screenPosition} is INSIDE interaction zone '{gameObject.name}'");
            return true;
        }

        Debug.Log($"Position {screenPosition} is OUTSIDE interaction zone '{gameObject.name}'");
        return false;
    }

}
