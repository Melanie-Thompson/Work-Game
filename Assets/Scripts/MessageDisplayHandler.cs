using UnityEngine;
using TMPro;

/// <summary>
/// Handles the visual display of messages from the MessageQueue.
/// Listens to MessageQueue events and controls the UI elements.
/// </summary>
public class MessageDisplayHandler : MonoBehaviour
{
    [Header("UI References")]
    [Tooltip("The GameObject containing the message UI (will be shown/hidden)")]
    public GameObject messageUIObject;

    [Tooltip("The TextMeshPro component to display the message text")]
    public TextMeshProUGUI messageText;

    [Header("Display Settings")]
    [Tooltip("Starting Y position for the message")]
    public float messageStartY = -200f;

    [Tooltip("Rise speed for RetroArcadeText animation")]
    public float riseSpeed = 300f;

    [Tooltip("Rise speed for high-priority messages (like rabbit hits)")]
    public float fastRiseSpeed = 2000f;

    [Tooltip("Height at which message disappears")]
    public float destroyHeight = 2000f;

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

        // Hide message UI initially
        if (messageUIObject != null)
        {
            messageUIObject.SetActive(false);
            Debug.Log("MessageDisplayHandler: Message UI hidden at startup");
        }
        else
        {
            Debug.LogError("MessageDisplayHandler: messageUIObject is not assigned!");
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
    }

    /// <summary>
    /// Called when a message starts displaying
    /// </summary>
    private void OnMessageStart(string message)
    {
        Debug.LogWarning($"MessageDisplayHandler: === DISPLAYING MESSAGE === '{message}'");

        if (messageUIObject == null || messageText == null)
        {
            Debug.LogError("MessageDisplayHandler: Missing UI references!");
            return;
        }

        // Reset position to starting Y
        RectTransform rectTransform = messageUIObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector2 anchoredPos = rectTransform.anchoredPosition;
            anchoredPos.y = messageStartY;
            rectTransform.anchoredPosition = anchoredPos;
            Debug.Log($"MessageDisplayHandler: Reset anchoredPosition to y={messageStartY}");
        }

        // Don't override RetroArcadeText settings - let Inspector values be used
        var retroText = messageUIObject.GetComponent<RetroArcadeText>();
        if (retroText != null)
        {
            // Reset rise speed to default value (in case it was accelerated)
            retroText.riseSpeed = riseSpeed;
            Debug.Log($"MessageDisplayHandler: RetroArcadeText - riseSpeed reset to {retroText.riseSpeed}, destroyHeight={retroText.destroyHeight}");
        }
        else
        {
            Debug.LogWarning("MessageDisplayHandler: No RetroArcadeText component found on messageUIObject!");
        }

        // Update text - THIS IS CRITICAL
        Debug.LogWarning($"MessageDisplayHandler: BEFORE setting text, messageText.text = '{messageText.text}'");
        messageText.text = message;
        Debug.LogWarning($"MessageDisplayHandler: AFTER setting text, messageText.text = '{messageText.text}'");

        // Show the UI
        messageUIObject.SetActive(true);
        Debug.Log($"MessageDisplayHandler: Message UI activated");
    }

    /// <summary>
    /// Called when a message finishes displaying
    /// </summary>
    private void OnMessageEnd(string message)
    {
        Debug.Log($"MessageDisplayHandler: Hiding message - '{message}'");

        if (messageUIObject != null)
        {
            messageUIObject.SetActive(false);
            Debug.Log("MessageDisplayHandler: Message UI hidden");
        }
    }

    /// <summary>
    /// Manually hide the message (useful for external systems)
    /// </summary>
    public void HideMessage()
    {
        if (messageUIObject != null && messageUIObject.activeSelf)
        {
            messageUIObject.SetActive(false);
            Debug.Log("MessageDisplayHandler: Message UI manually hidden");
        }
    }
}
