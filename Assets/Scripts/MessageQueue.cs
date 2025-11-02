using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// Message queue system that separates emission and display.
/// Emitters add messages to the queue, and a separate process displays them.
/// </summary>
public class MessageQueue : MonoBehaviour
{
    public static MessageQueue Instance { get; private set; }

    [System.Serializable]
    public class QueuedMessage
    {
        public string text;
        public float displayDuration;
        public int priority; // Lower number = higher priority
        public System.Action onComplete; // Optional callback when message finishes displaying

        public QueuedMessage(string text, float displayDuration = 3f, int priority = 10, System.Action onComplete = null)
        {
            this.text = text;
            this.displayDuration = displayDuration;
            this.priority = priority;
            this.onComplete = onComplete;
        }
    }

    // The message queue (FIFO)
    private Queue<QueuedMessage> messageQueue = new Queue<QueuedMessage>();

    // Current display state
    private QueuedMessage currentMessage = null;
    private float currentMessageTimer = 0f;
    private bool isDisplayingMessage = false;

    // Event delegates for external systems to hook into
    public delegate void MessageDisplayHandler(string message);
    public event MessageDisplayHandler OnMessageDisplayStart;
    public event MessageDisplayHandler OnMessageDisplayEnd;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("MessageQueue: Instance created");
        }
        else
        {
            Debug.LogWarning("MessageQueue: Duplicate instance destroyed");
            Destroy(gameObject);
        }
    }

    void Update()
    {
        ProcessQueue();
    }

    /// <summary>
    /// Emit a message to the queue (PRODUCER)
    /// </summary>
    public void EmitMessage(string message, float duration = 3f, int priority = 10, System.Action onComplete = null)
    {
        QueuedMessage queuedMsg = new QueuedMessage(message, duration, priority, onComplete);
        messageQueue.Enqueue(queuedMsg);
        Debug.Log($"MessageQueue: Message emitted - '{message}' (priority: {priority}, duration: {duration}s) - Queue size: {messageQueue.Count}");
    }

    /// <summary>
    /// Process the queue and display messages (CONSUMER)
    /// </summary>
    private void ProcessQueue()
    {
        // If we're displaying a message, update its timer
        if (isDisplayingMessage && currentMessage != null)
        {
            currentMessageTimer += Time.deltaTime;

            // Check if it's time to finish displaying this message
            if (currentMessageTimer >= currentMessage.displayDuration)
            {
                Debug.Log($"MessageQueue: Message display complete - '{currentMessage.text}'");
                FinishCurrentMessage();
            }
        }
        // If not displaying and queue has messages, display the next one
        else if (!isDisplayingMessage && messageQueue.Count > 0)
        {
            currentMessage = messageQueue.Dequeue();
            StartDisplayingMessage(currentMessage);
        }
    }

    /// <summary>
    /// Start displaying a message
    /// </summary>
    private void StartDisplayingMessage(QueuedMessage message)
    {
        isDisplayingMessage = true;
        currentMessageTimer = 0f;

        Debug.Log($"MessageQueue: Starting display - '{message.text}' for {message.displayDuration}s ({messageQueue.Count} remaining in queue)");

        // Fire event for external systems to react
        OnMessageDisplayStart?.Invoke(message.text);
    }

    /// <summary>
    /// Finish displaying the current message
    /// </summary>
    private void FinishCurrentMessage()
    {
        if (currentMessage != null)
        {
            string messageText = currentMessage.text;

            // Fire completion callback if exists
            currentMessage.onComplete?.Invoke();

            // Fire event for external systems to react
            OnMessageDisplayEnd?.Invoke(messageText);
        }

        // Reset state
        currentMessage = null;
        isDisplayingMessage = false;
        currentMessageTimer = 0f;
    }

    /// <summary>
    /// Skip the current message and move to the next
    /// </summary>
    public void SkipCurrentMessage()
    {
        if (isDisplayingMessage)
        {
            Debug.Log($"MessageQueue: Skipping current message - '{currentMessage?.text}'");
            FinishCurrentMessage();
        }
    }

    /// <summary>
    /// Clear all messages from the queue
    /// </summary>
    public void ClearQueue()
    {
        int count = messageQueue.Count;
        messageQueue.Clear();
        Debug.Log($"MessageQueue: Cleared {count} messages from queue");
    }

    /// <summary>
    /// Get the number of messages waiting in the queue
    /// </summary>
    public int GetQueueSize()
    {
        return messageQueue.Count;
    }

    /// <summary>
    /// Check if a message is currently being displayed
    /// </summary>
    public bool IsDisplaying()
    {
        return isDisplayingMessage;
    }

    /// <summary>
    /// Get the current message being displayed (null if none)
    /// </summary>
    public string GetCurrentMessage()
    {
        return currentMessage?.text;
    }
}
