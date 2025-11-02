# Message Queue System

A decoupled message emission and display system for Unity.

## Architecture

The message queue system consists of three main components:

1. **MessageQueue.cs** - The queue manager (PRODUCER/CONSUMER)
2. **MessageDisplayHandler.cs** - The visual display handler (SUBSCRIBER)
3. **Game systems** - Emit messages (EMITTERS)

## How It Works

```
[Emitter] --EmitMessage()--> [MessageQueue] --OnMessageDisplayStart--> [MessageDisplayHandler]
                                   |                                            |
                                   |                                            v
                                   |                                    [UI Updates]
                                   |                                            |
                                   |<------OnMessageDisplayEnd-------------------+
                                   v
                            [Process Next Message]
```

### 1. Message Emission (Producer)

Any script can emit messages to the queue:

```csharp
// Simple message
MessageQueue.Instance.EmitMessage("HELLO WORLD!");

// Message with custom duration
MessageQueue.Instance.EmitMessage("IMPORTANT MESSAGE", duration: 5f);

// Message with priority (lower = higher priority)
MessageQueue.Instance.EmitMessage("HIGH PRIORITY", duration: 3f, priority: 1);

// Message with completion callback
MessageQueue.Instance.EmitMessage("TASK COMPLETE", duration: 3f, priority: 10, () => {
    Debug.Log("Message finished displaying!");
});
```

### 2. Message Queue Processing (Consumer)

The `MessageQueue` runs in `Update()` and automatically:
- Dequeues messages when ready
- Tracks display timer
- Fires events when messages start/end
- Manages the queue state

### 3. Message Display (Subscriber)

The `MessageDisplayHandler` subscribes to `MessageQueue` events:
- `OnMessageDisplayStart` - Shows the UI with the message text
- `OnMessageDisplayEnd` - Hides the UI

## Setup in Unity

### 1. Create MessageQueue GameObject

1. Create an empty GameObject named "MessageQueue"
2. Add the `MessageQueue.cs` component
3. This GameObject should persist throughout the game

### 2. Create MessageDisplayHandler GameObject

1. Create an empty GameObject named "MessageDisplayHandler"
2. Add the `MessageDisplayHandler.cs` component
3. Assign references in Inspector:
   - **Message UI Object**: The GameObject containing your message UI (will be shown/hidden)
   - **Message Text**: The TextMeshProUGUI component to display text
4. Configure settings:
   - **Message Start Y**: Starting Y position for the message (-1400 by default)

### 3. Update GameManager References

The GameManager has been updated to use the new system:

```csharp
// Old way (still works as fallback)
GameManager.Instance.ShowBonusMessage("MESSAGE");

// New way (automatically uses MessageQueue if available)
GameManager.Instance.ShowBonusMessage("MESSAGE", duration: 3f, priority: 10);
```

## Features

### Queue Management

```csharp
// Get queue size
int size = MessageQueue.Instance.GetQueueSize();

// Check if displaying
bool isDisplaying = MessageQueue.Instance.IsDisplaying();

// Get current message
string current = MessageQueue.Instance.GetCurrentMessage();

// Skip current message
MessageQueue.Instance.SkipCurrentMessage();

// Clear entire queue
MessageQueue.Instance.ClearQueue();
```

### Priority System

Messages with lower priority numbers are displayed first:
- Priority 1 = Highest priority (critical messages)
- Priority 10 = Normal priority (default)
- Priority 100 = Low priority (background notifications)

**Note:** Current implementation uses FIFO queue. To support priority-based ordering, replace `Queue<QueuedMessage>` with a priority queue implementation.

## Example Usage

### Lever Scoring

```csharp
// In Lever_Drag_Slide.cs
MessageQueue.Instance.EmitMessage("LEVER PULLED! +100 POINTS", duration: 2f);
```

### Phone System

```csharp
// In GameManager.cs
MessageQueue.Instance.EmitMessage($"CALLING {phoneNumber}...", duration: 1f);
// After 1 second...
MessageQueue.Instance.EmitMessage("CALL CONNECTED! +500 POINTS", duration: 3f);
```

### Work Time Bar

```csharp
// In WorkTimeBar.cs
MessageQueue.Instance.EmitMessage("WORK SHIFT COMPLETE! +5000 BONUS!", duration: 4f, priority: 1);
```

## Migration from Old System

The old queue-based system in GameManager is kept as a fallback. To fully migrate:

1. Add `MessageQueue` GameObject to scene
2. Add `MessageDisplayHandler` GameObject to scene
3. Configure display handler references
4. Test that messages appear correctly
5. (Optional) Remove legacy queue code from GameManager once confirmed working

## Benefits

1. **Separation of Concerns**: Emission and display are completely decoupled
2. **Reusable**: Any script can emit messages without knowing about display
3. **Event-Driven**: Display handler reacts to events, no tight coupling
4. **Extensible**: Easy to add multiple display handlers (e.g., console, UI, audio)
5. **Testable**: Each component can be tested independently

## Future Enhancements

- [ ] Priority queue implementation for true priority ordering
- [ ] Message categories/channels (e.g., "score", "story", "error")
- [ ] Multiple display handlers (e.g., different UI zones)
- [ ] Message history/logging
- [ ] Sound effects on message display
- [ ] Message interpolation/queuing strategies (replace, append, interrupt)
