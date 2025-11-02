using UnityEngine;

/// <summary>
/// Simple component to mark a GameObject as a swipe zone for the carousel.
/// Add this to a GameObject with a BoxCollider to create a swipe detection area.
/// </summary>
public class SwipeZone : MonoBehaviour
{
    [Header("Visualization")]
    [Tooltip("Show the swipe zone as a visible box on screen")]
    public bool showVisibleBox = true;

    [Tooltip("Color of the visible box")]
    public Color boxColor = new Color(0, 1, 0, 0.3f);

    void Start()
    {
        // Ensure we have a BoxCollider
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider == null)
        {
            Debug.LogWarning($"SwipeZone '{gameObject.name}': No BoxCollider found! Adding one.");
            collider = gameObject.AddComponent<BoxCollider>();
        }

        if (showVisibleBox)
        {
            CreateVisibleBox();
        }

        Debug.Log($"SwipeZone '{gameObject.name}' initialized with collider bounds: {collider.bounds}");
    }

    void CreateVisibleBox()
    {
        // Create a cube mesh to visualize the box collider
        GameObject visualBox = GameObject.CreatePrimitive(PrimitiveType.Cube);
        visualBox.name = "SwipeZone_Visual";
        visualBox.transform.SetParent(transform);
        visualBox.transform.localPosition = Vector3.zero;
        visualBox.transform.localRotation = Quaternion.identity;
        visualBox.transform.localScale = Vector3.one;

        // Get the box collider size and match it
        BoxCollider collider = GetComponent<BoxCollider>();
        if (collider != null)
        {
            visualBox.transform.localPosition = collider.center;
            visualBox.transform.localScale = collider.size;
        }

        // Remove the collider from the visual box (we only want the mesh)
        BoxCollider visualBoxCollider = visualBox.GetComponent<BoxCollider>();
        if (visualBoxCollider != null)
        {
            Destroy(visualBoxCollider);
        }

        // Set up the material to be semi-transparent
        MeshRenderer renderer = visualBox.GetComponent<MeshRenderer>();
        if (renderer != null)
        {
            Material mat = new Material(Shader.Find("Standard"));
            mat.color = boxColor;
            mat.SetFloat("_Mode", 3); // Transparent mode
            mat.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
            mat.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
            mat.SetInt("_ZWrite", 0);
            mat.DisableKeyword("_ALPHATEST_ON");
            mat.EnableKeyword("_ALPHABLEND_ON");
            mat.DisableKeyword("_ALPHAPREMULTIPLY_ON");
            mat.renderQueue = 3000;
            renderer.material = mat;
        }
    }
}
