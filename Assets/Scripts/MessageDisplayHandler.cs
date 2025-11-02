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
    public float messageStartY = -800f;

    [Tooltip("Rise speed for RetroArcadeText animation")]
    public float riseSpeed = 150f;

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
        Debug.Log($"MessageDisplayHandler: Displaying message - '{message}'");

        if (messageUIObject == null || messageText == null)
        {
            Debug.LogError("MessageDisplayHandler: Missing UI references!");
            return;
        }

        // Reset position to starting Y
        RectTransform rectTransform = messageUIObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            Vector3 currentPos = rectTransform.localPosition;
            rectTransform.localPosition = new Vector3(currentPos.x, messageStartY, currentPos.z);
            Debug.Log($"MessageDisplayHandler: Reset position to y={messageStartY}");
        }

        // Reset RetroArcadeText speed if it exists
        var retroText = messageUIObject.GetComponent<RetroArcadeText>();
        if (retroText != null)
        {
            retroText.riseSpeed = riseSpeed;
            Debug.Log($"MessageDisplayHandler: Reset RetroArcadeText speed to {riseSpeed}");
        }

        // Update text
        messageText.text = message;

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
