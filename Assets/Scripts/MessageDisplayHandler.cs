using UnityEngine;
using TMPro;
using System.Collections.Generic;

/// <summary>
/// Handles the visual display of messages from the MessageQueue.
/// Listens to MessageQueue events and creates instances of messages that rise up the screen.
/// Each message is a separate GameObject that rises independently.
/// </summary>
public class MessageDisplayHandler : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The prefab to instantiate for each message")]
    public GameObject messageUIPrefab;

    [Tooltip("The parent canvas/transform to spawn messages under")]
    public Transform messageParent;

    [Header("Display Settings")]
    [Tooltip("Starting Y position for the message")]
    public float messageStartY = -200f;

    [Tooltip("Rise speed for RetroArcadeText animation")]
    public float riseSpeed = 300f;

    [Tooltip("Rise speed for high-priority messages (like rabbit hits)")]
    public float fastRiseSpeed = 2000f;

    [Tooltip("Height at which message disappears")]
    public float destroyHeight = 2000f;

    // Track all active message instances
    private List<GameObject> activeMessages = new List<GameObject>();

    void Start()
    {
        // Subscribe to MessageQueue events
        if (MessageQueue.Instance != null)
        {
            MessageQueue.Instance.OnMessageDisplayStart += OnMessageStart;
            MessageQueue.Instance.OnMessageDisplayEnd += OnMessageEnd;
            Debug.Log("MessageDisplayHandler: Subscribed to MessageQueue events");
        }
        else
        {
            Debug.LogError("MessageDisplayHandler: MessageQueue.Instance is null!");
        }

        // Validate references
        if (messageUIPrefab == null)
        {
            Debug.LogError("MessageDisplayHandler: messageUIPrefab is not assigned!");
        }

        if (messageParent == null)
        {
            Debug.LogWarning("MessageDisplayHandler: messageParent is not assigned, will use this GameObject's transform");
            messageParent = transform;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events when destroyed
        if (MessageQueue.Instance != null)
        {
            MessageQueue.Instance.OnMessageDisplayStart -= OnMessageStart;
            MessageQueue.Instance.OnMessageDisplayEnd -= OnMessageEnd;
            Debug.Log("MessageDisplayHandler: Unsubscribed from MessageQueue events");
        }

        // Clean up any remaining message instances
        foreach (GameObject msg in activeMessages)
        {
            if (msg != null)
            {
                Destroy(msg);
            }
        }
        activeMessages.Clear();
    }

    /// <summary>
    /// Called when a message starts displaying - creates a new instance
    /// </summary>
    private void OnMessageStart(string message)
    {
        Debug.LogWarning($"MessageDisplayHandler: === CREATING NEW MESSAGE === '{message}'");

        if (messageUIPrefab == null)
        {
            Debug.LogError("MessageDisplayHandler: messageUIPrefab is null!");
            return;
        }

        // Instantiate a new message instance
        GameObject messageInstance = Instantiate(messageUIPrefab, messageParent);
        activeMessages.Add(messageInstance);
        Debug.Log($"MessageDisplayHandler: Instantiated new message, total active: {activeMessages.Count}");

        // Get the RectTransform and set starting position
        RectTransform rectTransform = messageInstance.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector2 anchoredPos = rectTransform.anchoredPosition;
            anchoredPos.y = messageStartY;
            rectTransform.anchoredPosition = anchoredPos;
            Debug.Log($"MessageDisplayHandler: Set anchoredPosition to y={messageStartY}");
        }

        // Set the text
        TextMeshProUGUI messageText = messageInstance.GetComponent<TextMeshProUGUI>();
        if (messageText != null)
        {
            messageText.text = message;
            Debug.Log($"MessageDisplayHandler: Set text to '{message}'");
        }
        else
        {
            Debug.LogError("MessageDisplayHandler: No TextMeshProUGUI component on instantiated message!");
        }

        // Configure RetroArcadeText component
        var retroText = messageInstance.GetComponent<RetroArcadeText>();
        if (retroText != null)
        {
            retroText.riseSpeed = riseSpeed;
            retroText.destroyHeight = destroyHeight;
            Debug.Log($"MessageDisplayHandler: RetroArcadeText configured - riseSpeed={riseSpeed}, destroyHeight={destroyHeight}");
        }
        else
        {
            Debug.LogWarning("MessageDisplayHandler: No RetroArcadeText component on instantiated message!");
        }

        // Activate the message
        messageInstance.SetActive(true);
        Debug.Log($"MessageDisplayHandler: Message instance activated");
    }

    /// <summary>
    /// Called when a message finishes displaying - this just marks it as done in the queue
    /// The actual message GameObject will destroy itself when it reaches destroyHeight
    /// </summary>
    private void OnMessageEnd(string message)
    {
        Debug.Log($"MessageDisplayHandler: Message finished in queue - '{message}'");
        // Don't destroy anything here - let RetroArcadeText handle it when it reaches destroyHeight
    }

    /// <summary>
    /// Accelerate all currently visible messages by 5x
    /// </summary>
    public void AccelerateAllMessages()
    {
        Debug.Log($"MessageDisplayHandler: Accelerating {activeMessages.Count} active messages by 5x");

        // Remove null entries (messages that have already been destroyed)
        activeMessages.RemoveAll(msg => msg == null);

        foreach (GameObject messageObj in activeMessages)
        {
            var retroText = messageObj.GetComponent<RetroArcadeText>();
            if (retroText != null)
            {
                retroText.riseSpeed = retroText.riseSpeed * 5f;
                Debug.Log($"MessageDisplayHandler: Accelerated message to {retroText.riseSpeed} px/s");
            }
        }
    }

    /// <summary>
    /// Clean up destroyed messages from the active list
    /// </summary>
    void Update()
    {
        // Clean up null references every second
        if (Time.frameCount % 60 == 0)
        {
            int beforeCount = activeMessages.Count;
            activeMessages.RemoveAll(msg => msg == null);
            int afterCount = activeMessages.Count;

            if (beforeCount != afterCount)
            {
                Debug.Log($"MessageDisplayHandler: Cleaned up {beforeCount - afterCount} destroyed messages, {afterCount} still active");
            }
        }
    }
}
