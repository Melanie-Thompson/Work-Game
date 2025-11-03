using UnityEngine;

public class RabbitPositioner : MonoBehaviour
{
    [Header("Positioning")]
    public float spacing = 2f; // Distance between the two rabbits
    public Transform rabbit1; // Assign RabbitParent
    public Transform rabbit2; // Assign RabbitParent (1)

    void Start()
    {
        PositionRabbits();
    }

    void PositionRabbits()
    {
        if (rabbit1 == null || rabbit2 == null)
        {
            Debug.LogError("RabbitPositioner: Both rabbits must be assigned!");
            return;
        }

        // Position rabbits centered on x-axis relative to this parent
        // Rabbit 1 on the left, Rabbit 2 on the right
        rabbit1.localPosition = new Vector3(-spacing / 2f, rabbit1.localPosition.y, rabbit1.localPosition.z);
        rabbit2.localPosition = new Vector3(spacing / 2f, rabbit2.localPosition.y, rabbit2.localPosition.z);

        Debug.Log($"Positioned rabbits at {rabbit1.localPosition} and {rabbit2.localPosition}");
    }
}
