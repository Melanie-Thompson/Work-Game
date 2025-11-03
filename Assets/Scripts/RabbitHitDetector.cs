using UnityEngine;

public class RabbitHitDetector : MonoBehaviour
{
    [Header("Hit Response")]
    public bool isLeftRabbit = true; // True for left rabbit, false for right
    public float fallDuration = 1.5f;
    public float fallRotation = 90f; // How far forward to rotate
    public float fallDistance = 10f; // How far down to fall
    public float jumpHeight = 1f; // Initial jump up before falling
    public float forwardDistance = 8f; // How far forward (Z) they fly - negative Z toward camera
    public float respawnDelay = 2f; // Time after falling before respawning

    private bool isHit = false;
    private float hitTime = 0f;
    private Vector3 startPosition;
    private Quaternion startRotation;
    private RabbitBouncer bouncer;
    private Vector3 originalLocalPosition;
    private Quaternion originalLocalRotation;
    private CircularCarousel carousel;
    private GameObject myCarouselWrapper;

    void Start()
    {
        bouncer = GetComponent<RabbitBouncer>();
        originalLocalPosition = transform.localPosition;
        originalLocalRotation = transform.localRotation;

        // Find the carousel
        carousel = FindFirstObjectByType<CircularCarousel>();

        // Find my carousel wrapper by looking for a parent that's in the carousel array
        if (carousel != null)
        {
            myCarouselWrapper = FindMyCarouselWrapper();
            if (myCarouselWrapper != null)
            {
                Debug.Log($"RabbitHitDetector '{gameObject.name}': Found carousel wrapper: {myCarouselWrapper.name}");
            }
            else
            {
                Debug.LogWarning($"RabbitHitDetector '{gameObject.name}': Could not find carousel wrapper in parent hierarchy!");
            }
        }

        // Log collider info
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Debug.Log($"{gameObject.name} has collider: {col.GetType().Name}, isTrigger: {col.isTrigger}");
        }
        else
        {
            Debug.LogError($"{gameObject.name} has NO COLLIDER!");
        }
    }

    void OnDrawGizmos()
    {
        // Visualize the collider in scene view
        Collider col = GetComponent<Collider>();
        if (col != null)
        {
            Gizmos.color = isHit ? Color.red : Color.green;
            Gizmos.DrawWireCube(transform.position, col.bounds.size);
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        Debug.Log($"COLLISION: {gameObject.name} collided with {collision.gameObject.name}");

        // Check if hit by hammer
        if (collision.gameObject.name.Contains("Hammer") && !isHit)
        {
            Debug.Log($"*** HAMMER HIT! {gameObject.name} hit by {collision.gameObject.name}! ***");
            StartFall();
        }
    }

    void OnTriggerEnter(Collider other)
    {
        // IMPORTANT: Only allow hits when this rabbit's carousel item is centered
        if (carousel != null && myCarouselWrapper != null)
        {
            GameObject centeredObject = carousel.GetCenteredObject();
            bool isCentered = (centeredObject == myCarouselWrapper);

            if (!isCentered)
            {
                Debug.Log($"RabbitHitDetector '{gameObject.name}': Ignoring hit - not centered (centered item: {centeredObject?.name})");
                return;
            }
        }

        // Check if hit by hammer (if using triggers)
        bool containsHammer = other.gameObject.name.Contains("Hammer");

        if (containsHammer && !isHit)
        {
            // Check if it's the correct side hammer
            bool isLeftHammer = other.gameObject.name.Contains("L") || other.gameObject.name.Contains("Left");
            bool isRightHammer = other.gameObject.name.Contains("R") || other.gameObject.name.Contains("Right");

            // Only allow hit if hammer matches rabbit side
            if ((isLeftRabbit && isLeftHammer) || (!isLeftRabbit && isRightHammer))
            {
                Debug.LogWarning($"*** HAMMER HIT! {gameObject.name} hit by {other.gameObject.name}! ***");
                StartFall();
            }
        }
    }

    void StartFall()
    {
        Debug.LogWarning($"=== STARTFALL CALLED ON {gameObject.name} === STACK TRACE:");
        Debug.LogWarning(System.Environment.StackTrace);

        isHit = true;
        hitTime = 0f;
        startPosition = transform.localPosition;
        startRotation = transform.localRotation;

        // Disable bouncer
        if (bouncer != null)
        {
            bouncer.enabled = false;
        }

        // Award points and show bonus message
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(500);

            // Show message with shorter duration so it rises faster and disappears quickly
            GameManager.Instance.ShowBonusMessage("RABBIT DOWN! +500 POINTS", duration: 1.5f, priority: 1);

            Debug.Log("RabbitHitDetector: Awarded 500 points for hitting rabbit!");
        }
    }

    void Update()
    {
        if (isHit)
        {
            hitTime += Time.deltaTime;

            if (hitTime < fallDuration)
            {
                float t = hitTime / fallDuration;

                // Arc trajectory - jump up then fall down
                float yOffset;
                if (t < 0.3f)
                {
                    // Jump up phase
                    float jumpT = t / 0.3f;
                    yOffset = Mathf.Lerp(0, jumpHeight, jumpT);
                }
                else
                {
                    // Fall down phase with acceleration
                    float fallT = (t - 0.3f) / 0.7f;
                    yOffset = jumpHeight - (fallDistance * fallT * fallT);
                }

                Vector3 targetPos = startPosition;
                targetPos.y += yOffset;
                targetPos.z -= forwardDistance * t; // Fly forward

                transform.localPosition = targetPos;

                // Rotate forward continuously
                Quaternion targetRot = startRotation * Quaternion.Euler(fallRotation * t * 2f, 0, 0);
                transform.localRotation = targetRot;
            }
            else if (hitTime >= fallDuration + respawnDelay)
            {
                // Respawn
                Respawn();
            }
        }
    }

    void Respawn()
    {
        Debug.Log($"{gameObject.name} respawning!");
        isHit = false;
        hitTime = 0f;
        transform.localPosition = originalLocalPosition;
        transform.localRotation = originalLocalRotation;

        // Re-enable bouncer
        if (bouncer != null)
        {
            bouncer.enabled = true;
        }
    }

    GameObject FindMyCarouselWrapper()
    {
        // Walk up the parent hierarchy and check if any parent is in the carousel's object array
        Transform current = transform.parent;
        while (current != null)
        {
            // Check if this parent is in the carousel's carouselObjects array
            if (carousel.carouselObjects != null)
            {
                foreach (GameObject carouselObj in carousel.carouselObjects)
                {
                    if (carouselObj == current.gameObject)
                    {
                        return current.gameObject;
                    }
                }
            }
            current = current.parent;
        }
        return null;
    }
}
