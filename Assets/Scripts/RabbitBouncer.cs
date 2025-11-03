using UnityEngine;

public class RabbitBouncer : MonoBehaviour
{
    [Header("Bounce Settings")]
    public float bounceHeight = 0.3f;
    public float bounceSpeed = 2f;
    public float rotationAmount = 5f;

    [Header("Back and Forward Movement")]
    public float moveDistance = 3f; // How far forward they move (Z axis)
    public float cycleTime = 3f; // Total time for one full cycle
    public float forwardTime = 0.5f; // Time to move forward (fast)
    public float scaleFactor = 0.35f; // How much smaller they get when far away
    public bool alternatePhase = false; // Check this on one rabbit to make them alternate

    private Vector3 startPosition;
    private Vector3 startScale;
    private float time = 0f;
    private float bounceRandomOffset;
    private float rotationRandomOffset;
    private float bounceSpeedRandom;
    private float rotationSpeedRandom;
    private float cycleProgress = 0f;

    void Start()
    {
        startPosition = transform.localPosition;
        startScale = transform.localScale;

        // Random offsets for variety
        bounceRandomOffset = Random.Range(0f, Mathf.PI * 2f);
        rotationRandomOffset = Random.Range(0f, Mathf.PI * 2f);
        bounceSpeedRandom = Random.Range(0.8f, 1.2f);
        rotationSpeedRandom = Random.Range(0.7f, 1.3f);

        // If alternatePhase is true, start half a cycle offset
        if (alternatePhase)
        {
            cycleProgress = cycleTime * 0.5f;
        }
    }

    void Update()
    {
        time += Time.deltaTime * bounceSpeed;
        cycleProgress += Time.deltaTime;

        // Reset cycle
        if (cycleProgress >= cycleTime)
        {
            cycleProgress -= cycleTime;
        }

        // Bounce up and down with random variation
        Vector3 pos = startPosition;
        pos.y += Mathf.Abs(Mathf.Sin(time * bounceSpeedRandom + bounceRandomOffset)) * bounceHeight;

        // Move back and forward with acceleration
        float zOffset;
        if (cycleProgress < forwardTime)
        {
            // Fast forward with acceleration (ease-in-out)
            float t = cycleProgress / forwardTime;
            float eased = t * t * (3f - 2f * t); // Smoothstep
            zOffset = Mathf.Lerp(moveDistance, -moveDistance, eased);
        }
        else
        {
            // Slow uniform speed back
            float backTime = cycleTime - forwardTime;
            float t = (cycleProgress - forwardTime) / backTime;
            zOffset = Mathf.Lerp(-moveDistance, moveDistance, t);
        }

        pos.z += zOffset;

        transform.localPosition = pos;

        // Scale based on Z position for fake perspective
        // When zOffset is negative (far away), scale is smaller
        float scaleAmount = 1f - (zOffset / moveDistance) * scaleFactor;
        transform.localScale = startScale * scaleAmount;

        // Rotation with random variation
        Vector3 rot = transform.localEulerAngles;
        rot.z = Mathf.Sin(time * 2f * rotationSpeedRandom + rotationRandomOffset) * rotationAmount;
        transform.localEulerAngles = rot;
    }
}
