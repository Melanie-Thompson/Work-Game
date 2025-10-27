using UnityEngine;

public class CarouselItem : MonoBehaviour
{
    [SerializeField]
    private string itemId;

    public string ItemId
    {
        get
        {
            // Auto-generate ID if not set
            if (string.IsNullOrEmpty(itemId))
            {
                itemId = System.Guid.NewGuid().ToString();
                Debug.Log($"CarouselItem '{gameObject.name}': Generated new ID: {itemId}");
            }
            return itemId;
        }
        set { itemId = value; }
    }

    void OnValidate()
    {
        // Generate ID in editor if empty
        if (string.IsNullOrEmpty(itemId))
        {
            itemId = System.Guid.NewGuid().ToString();
        }
    }
}
