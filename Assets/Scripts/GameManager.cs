using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public CircularCarousel carousel;
    public WorkTimeBar workTimeBar; // Reference to WorkTimeBar
    public TextMeshProUGUI scoreText;
    public GameObject bonusMessageObject; // Reference to BonusMessage GameObject
    public GameObject emailCanvas; // Reference to Email Canvas
    public TextMeshProUGUI emailCountText; // Reference to Email Count UI text
    public TextMeshProUGUI priorityEmailCountText; // Reference to Priority Email Count UI text

    [Header("Bonus Message Settings")]
    public float bonusMessageStartY = -200f; // Starting Y position for bonus message
    public float bonusMessageDestroyHeight = 2000f; // Height at which message disappears
    public float bonusMessageRiseSpeed = 300f; // Speed at which message rises

    [System.Serializable]
    public class MonitorMessage
    {
        [TextArea(3, 10)]
        public string text;
        public GameObject[] imagesToShow;
    }

    [System.Serializable]
    public class StoryPoint
    {
        public string bonusMessage;
        public bool showEmailCanvas;
        public bool addEmail;
        public Email emailToAdd;
        public MonitorMessage[] monitorMessages;
    }

    [System.Serializable]
    public class Email
    {
        public string emailText;
        public bool isPriority;
    }

    [Header("Story System")]
    public StoryPoint[] story;
    private int storyValue = 0;

    [Header("Email System")]
    public Email[] emails;
    public int emailCount { get { return emails != null ? emails.Length : 0; } }
    public int priorityEmailCount
    {
        get
        {
            if (emails == null) return 0;
            int count = 0;
            foreach (var email in emails)
            {
                if (email != null && email.isPriority)
                    count++;
            }
            return count;
        }
    }

    private ClickableMonitor currentActiveMonitor;
    private int score = 0;

    // Global state flags
    private bool isCarouselActive = true;
    private bool isMonitorActive = false;
    private bool hasExitedMonitorOnce = false;
    private bool isWorkShiftComplete = false;
    
    void Awake()
    {
        Debug.Log("=== GAMEMANAGER AWAKE ===");
        if (Instance == null)
        {
            Instance = this;
            Debug.Log("GameManager: Instance created");
        }
        else
        {
            Debug.Log("GameManager: Duplicate instance destroyed");
            Destroy(gameObject);
            return;
        }

        // Hide all monitor images immediately in Awake (before Start)
        if (story != null)
        {
            foreach (var storyPoint in story)
            {
                if (storyPoint != null && storyPoint.monitorMessages != null)
                {
                    foreach (var message in storyPoint.monitorMessages)
                    {
                        if (message.imagesToShow != null)
                        {
                            foreach (var image in message.imagesToShow)
                            {
                                if (image != null)
                                {
                                    image.SetActive(false);
                                    Debug.Log($"GameManager Awake: Hiding monitor image: {image.name}");
                                }
                            }
                        }
                    }
                }
            }
        }
    }
    
    void Start()
    {
        Debug.Log("=== GAMEMANAGER START ===");

        // Ensure MessageQueue exists in scene
        if (MessageQueue.Instance == null)
        {
            Debug.LogWarning("GameManager: MessageQueue.Instance is NULL! Creating MessageQueue GameObject...");
            GameObject mqObj = new GameObject("MessageQueue");
            mqObj.AddComponent<MessageQueue>();
            Debug.Log("GameManager: MessageQueue GameObject created");
        }
        else
        {
            Debug.Log("GameManager: MessageQueue.Instance found - using queue system");
        }

        // Ensure MessageDisplayHandler exists and is configured
        MessageDisplayHandler handler = FindFirstObjectByType<MessageDisplayHandler>();
        if (handler == null && bonusMessageObject != null)
        {
            Debug.LogWarning("GameManager: No MessageDisplayHandler found! Creating on separate GameObject...");
            GameObject handlerObj = new GameObject("MessageDisplayHandler");
            handler = handlerObj.AddComponent<MessageDisplayHandler>();
            handler.messageUIObject = bonusMessageObject;
            handler.messageText = bonusMessageObject.GetComponent<TextMeshProUGUI>();
            handler.messageStartY = bonusMessageStartY;
            handler.riseSpeed = bonusMessageRiseSpeed;
            handler.destroyHeight = bonusMessageDestroyHeight;
            Debug.Log($"GameManager: MessageDisplayHandler created - messageUIObject={bonusMessageObject.name}, startY={bonusMessageStartY}, riseSpeed={bonusMessageRiseSpeed}, destroyHeight={bonusMessageDestroyHeight}");
        }
        else if (handler != null)
        {
            Debug.Log($"GameManager: MessageDisplayHandler found on {handler.gameObject.name}");
        }

        // Hide bonus message at startup
        if (bonusMessageObject != null)
        {
            Debug.Log($"GameManager: BonusMessage active state BEFORE: {bonusMessageObject.activeSelf}");

            // Set up the RetroArcadeText component for later use
            var bonusText = bonusMessageObject.GetComponent<TextMeshProUGUI>();
            var retroText = bonusMessageObject.GetComponent<RetroArcadeText>();
            if (retroText == null)
            {
                Debug.Log("GameManager: RetroArcadeText not found, adding it now");
                retroText = bonusMessageObject.AddComponent<RetroArcadeText>();
                Debug.Log($"GameManager: RetroArcadeText component added: {retroText != null}");

                // Configure it ONLY when first added
                retroText.textComponent = bonusText;
                retroText.enableSineWaveMovement = true;
                retroText.sineWaveAmplitude = 10f;
                retroText.sineWaveSpeed = 2f;
                retroText.letterDelay = 0.3f;
                retroText.destroyWhenOffScreen = false; // Don't destroy - just deactivate
                retroText.destroyHeight = 2000f;
                retroText.riseSpeed = 150f;
                retroText.enableShimmer = true;
                retroText.shimmerSpeed = 1f;
                retroText.shimmerColorA = Color.red;
                retroText.shimmerColorB = Color.blue;
                retroText.flickerIntensity = 0f;
                retroText.enableScanLines = false;
                Debug.Log($"GameManager: Configured RetroArcadeText - enabled: {retroText.enabled}, sineWave: {retroText.enableSineWaveMovement}");
            }
            else
            {
                Debug.Log("GameManager: RetroArcadeText already exists, keeping existing configuration");
            }

            // Hide it initially
            bonusMessageObject.SetActive(false);
            Debug.Log($"GameManager: BonusMessage hidden at startup");
        }
        else
        {
            Debug.LogError("GameManager: bonusMessageObject is NOT ASSIGNED in Inspector!");
        }

        // Try to find carousel if not assigned
        if (carousel == null)
        {
            carousel = FindFirstObjectByType<CircularCarousel>();
        }

        if (carousel != null)
        {
            Debug.Log("GameManager: Found carousel, setting reference");
            carousel.SetGameManager(this);
        }
        else
        {
            Debug.LogError("GameManager: Could not find CircularCarousel!");
        }

        // Initialize state
        isCarouselActive = true;
        isMonitorActive = false;
        hasExitedMonitorOnce = false;

        // Hide all monitor images at startup (backup to Awake)
        if (story != null)
        {
            foreach (var storyPoint in story)
            {
                if (storyPoint != null && storyPoint.monitorMessages != null)
                {
                    foreach (var message in storyPoint.monitorMessages)
                    {
                        if (message.imagesToShow != null)
                        {
                            foreach (var image in message.imagesToShow)
                            {
                                if (image != null)
                                {
                                    image.SetActive(false);
                                    Debug.Log($"GameManager: Hiding monitor image at startup: {image.name}");
                                }
                            }
                        }
                    }
                }
            }
        }

        // Initialize score display
        UpdateScoreDisplay();

        // Initialize email count display
        UpdateEmailCountDisplay();

        // Update email canvas visibility based on initial story point
        UpdateEmailCanvasVisibility();
    }

    void Update()
    {
        // Only use fallback system if MessageQueue doesn't exist
        if (MessageQueue.Instance == null)
        {
            ProcessBonusMessageQueue();
        }
    }

    public bool IsCarouselActive()
    {
        return isCarouselActive;
    }
    
    public bool IsMonitorActive()
    {
        return isMonitorActive;
    }
    
    // Called by CircularCarousel when user clicks on centered object
    public void OnCarouselObjectClicked(GameObject clickedObject)
    {
        Debug.LogWarning($"=== GAMEMANAGER: OnCarouselObjectClicked called for {clickedObject.name} === STACK TRACE:");
        Debug.LogWarning(System.Environment.StackTrace);
        Debug.Log($"State BEFORE: Carousel={isCarouselActive}, Monitor={isMonitorActive}");

        // IMPORTANT: Clicking on carousel items should NOT show any bonus messages
        // Only specific events (rabbit hits, phone calls, etc.) should show messages
        // So we hide any currently displaying message when opening a monitor
        HideBonusMessage();

        // Set global state
        isCarouselActive = false;
        isMonitorActive = true;

        // Disable carousel control
        if (carousel != null)
        {
            carousel.DisableCarousel();
        }

        // Enable the monitor
        var monitor = clickedObject.GetComponentInChildren<ClickableMonitor>();
        if (monitor != null)
        {
            Debug.Log($"GameManager: Found monitor '{monitor.gameObject.name}', enabling it now");
            currentActiveMonitor = monitor;

            // IMPORTANT: Set GameManager reference BEFORE enabling
            monitor.SetGameManager(this);

            // Check if already enabled
            Debug.Log($"GameManager: Monitor enabled state BEFORE: {monitor.enabled}, gameObject active: {monitor.gameObject.activeInHierarchy}");

            // Enable the monitor component - this will trigger OnEnable
            monitor.enabled = true;

            Debug.Log($"GameManager: Monitor enabled state AFTER: {monitor.enabled}");
            Debug.Log($"GameManager: Successfully enabled monitor on {clickedObject.name}");
        }
        else
        {
            Debug.LogError($"GameManager: No ClickableMonitor found on {clickedObject.name} or its children!");

            // Search more thoroughly and log what we find
            var allChildren = clickedObject.GetComponentsInChildren<Transform>();
            Debug.LogError($"GameManager: Object has {allChildren.Length} children. Listing components:");
            foreach (var child in allChildren)
            {
                var components = child.GetComponents<Component>();
                Debug.LogError($"  - {child.name}: {string.Join(", ", System.Array.ConvertAll(components, c => c.GetType().Name))}");
            }
        }

        Debug.Log($"State AFTER: Carousel={isCarouselActive}, Monitor={isMonitorActive}");
    }
    
    // Called by ClickableMonitor when user wants to exit/zoom out
    public void OnMonitorClosed()
    {
        Debug.Log("=== GAMEMANAGER: OnMonitorClosed called ===");
        Debug.Log($"State BEFORE: Carousel={isCarouselActive}, Monitor={isMonitorActive}");

        // Set global state FIRST
        isMonitorActive = false;
        isCarouselActive = true;

        // Disable the monitor
        if (currentActiveMonitor != null)
        {
            Debug.Log($"GameManager: Disabling monitor {currentActiveMonitor.gameObject.name}");
            currentActiveMonitor.enabled = false;
            currentActiveMonitor = null;
        }

        // Re-enable carousel control
        if (carousel != null)
        {
            Debug.Log("GameManager: Re-enabling carousel");
            carousel.EnableCarousel();
        }
        else
        {
            Debug.LogError("GameManager: Carousel is null, cannot re-enable!");
        }

        // Check if this is the first time exiting a monitor (do this AFTER state cleanup)
        if (!hasExitedMonitorOnce)
        {
            hasExitedMonitorOnce = true;
            Debug.Log("GameManager: First monitor exit - adding 20000 points and showing bonus message");
            AddScore(20000);

            // Get message from story array
            string message = "FIRST TASK COMPLETE!"; // Default
            if (story != null && story.Length > 0 && storyValue < story.Length && story[storyValue] != null)
            {
                message = story[storyValue].bonusMessage;
            }

            // Add message to queue with shorter duration so it rises faster
            ShowBonusMessage(message, duration: 1.5f);

            // Advance to next story point after induction is complete
            AdvanceStory();
        }

        Debug.Log($"State AFTER: Carousel={isCarouselActive}, Monitor={isMonitorActive}");
    }
    
    // Add score method for Lever_Drag_Slide
    public void AddScore(int points)
    {
        score += points;
        Debug.Log($"GameManager: Score added! Total score: {score}");
        UpdateScoreDisplay();
    }

    public int GetScore()
    {
        return score;
    }

    private void UpdateScoreDisplay()
    {
        if (scoreText != null)
        {
            scoreText.text = $"Score: {score}";
        }
        else
        {
            Debug.LogWarning("GameManager: scoreText is not assigned!");
        }
    }

    private void UpdateEmailCountDisplay()
    {
        if (emailCountText != null)
        {
            emailCountText.text = emailCount.ToString();
        }

        if (priorityEmailCountText != null)
        {
            priorityEmailCountText.text = priorityEmailCount.ToString();
        }

        Debug.Log($"GameManager: Email counts updated - Total: {emailCount}, Priority: {priorityEmailCount}");
    }

    private Coroutine bonusMessageCoroutine;
    private float bonusMessageStartTime = -999f;
    private float bonusMessageDuration = 0f;
    private Coroutine activeCallCoroutine;

    // Queue-based bonus message system
    private System.Collections.Generic.Queue<string> bonusMessageQueue = new System.Collections.Generic.Queue<string>();
    private bool isShowingBonusMessage = false;
    private float currentMessageTimer = 0f;
    private float messageDisplayDuration = 15f; // How long to show each message

    void ProcessBonusMessageQueue()
    {
        // Debug every 60 frames
        if (Time.frameCount % 60 == 0 && (isShowingBonusMessage || bonusMessageQueue.Count > 0))
        {
            Debug.Log($"GameManager ProcessBonusMessageQueue: isShowingBonusMessage={isShowingBonusMessage}, queueCount={bonusMessageQueue.Count}, timer={currentMessageTimer:F2}/{messageDisplayDuration}");
        }

        // If we're showing a message, update its timer
        if (isShowingBonusMessage)
        {
            currentMessageTimer += Time.deltaTime;

            // Check if it's time to hide the current message
            if (currentMessageTimer >= messageDisplayDuration)
            {
                Debug.Log("GameManager: Message display time expired, hiding message");
                HideBonusMessageImmediate();
                isShowingBonusMessage = false;
                currentMessageTimer = 0f; // Reset timer
            }
        }
        // If not showing a message and queue has messages, show the next one
        else if (bonusMessageQueue.Count > 0)
        {
            string nextMessage = bonusMessageQueue.Dequeue();
            Debug.Log($"GameManager: Processing queued message: '{nextMessage}' ({bonusMessageQueue.Count} remaining in queue)");
            ShowBonusMessageImmediate(nextMessage);
            isShowingBonusMessage = true;
            currentMessageTimer = 0f;
        }
    }

    void ShowBonusMessageImmediate(string message)
    {
        Debug.Log($"GameManager: ShowBonusMessageImmediate called with: '{message}'");

        if (bonusMessageObject == null)
        {
            Debug.LogError("GameManager: bonusMessageObject is null!");
            return;
        }

        // Reset position to starting Y
        RectTransform rectTransform = bonusMessageObject.GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.localPosition = new Vector3(rectTransform.localPosition.x, bonusMessageStartY, rectTransform.localPosition.z);
            Debug.Log($"GameManager: Reset position to y={bonusMessageStartY}");
        }

        // Don't override RetroArcadeText settings - let Inspector values be used
        var retroText = bonusMessageObject.GetComponent<RetroArcadeText>();
        if (retroText != null)
        {
            Debug.Log($"GameManager ShowBonusMessageImmediate: RetroArcadeText - riseSpeed={retroText.riseSpeed}, destroyHeight={retroText.destroyHeight}");
        }
        else
        {
            Debug.LogWarning("GameManager ShowBonusMessageImmediate: No RetroArcadeText component found!");
        }

        // Update text
        var bonusText = bonusMessageObject.GetComponent<TextMeshProUGUI>();
        if (bonusText != null)
        {
            bonusText.text = message;
            Debug.Log($"GameManager: Set text to '{message}'");
        }

        // Activate the object
        bonusMessageObject.SetActive(true);
        Debug.Log($"GameManager: BonusMessage activated and showing");
    }

    void HideBonusMessageImmediate()
    {
        if (bonusMessageObject != null && bonusMessageObject.activeSelf)
        {
            bonusMessageObject.SetActive(false);
            Debug.Log("GameManager: BonusMessage hidden");
        }
    }

    private System.Collections.IEnumerator ShowBonusMessageDelayed(string message, float delay)
    {
        // Show the message immediately
        ShowBonusMessage(message);

        // Track when it started and how long the "protected" period is
        bonusMessageStartTime = Time.time;
        bonusMessageDuration = delay;

        // Wait for the delay period
        yield return new WaitForSeconds(delay);

        // Clear the tracking
        bonusMessageStartTime = -999f;
        bonusMessageDuration = 0f;
    }

    public void ShowBonusMessage(string message, float duration = 3f, int priority = 10)
    {
        Debug.LogWarning($"=== ShowBonusMessage called with: '{message}' duration={duration}, priority={priority} ===");
        Debug.LogWarning("STACK TRACE:");
        Debug.LogWarning(System.Environment.StackTrace);

        // Use new MessageQueue system
        if (MessageQueue.Instance != null)
        {
            MessageQueue.Instance.EmitMessage(message, duration, priority);
        }
        else
        {
            Debug.LogError("GameManager: MessageQueue.Instance is null! Cannot emit message.");
            // Fallback to old system
            bonusMessageQueue.Enqueue(message);
            Debug.Log($"GameManager: Using fallback queue - now has {bonusMessageQueue.Count} messages");
        }
    }

    public void HideBonusMessage()
    {
        Debug.Log("=== HideBonusMessage called - HIDING current message only (keeping queue) ===");

        // Hide the message UI immediately
        if (bonusMessageObject != null && bonusMessageObject.activeSelf)
        {
            bonusMessageObject.SetActive(false);
            Debug.Log("GameManager: BonusMessage hidden immediately");
        }

        // Skip ONLY the currently displaying message, but keep queued messages
        // This allows important messages (like rabbit hits) to still show after interaction
        if (MessageQueue.Instance != null)
        {
            Debug.Log("GameManager: Skipping current message (keeping queue intact)");
            MessageQueue.Instance.SkipCurrentMessage(); // Skip current message only
        }
        else
        {
            Debug.LogWarning("GameManager: MessageQueue.Instance is null! Using fallback system.");

            // Legacy fallback system - hide current message but keep queue
            if (isShowingBonusMessage)
            {
                HideBonusMessageImmediate();
                isShowingBonusMessage = false;
                currentMessageTimer = 0f;
                Debug.Log("GameManager: Hidden fallback message immediately");
            }
            // Don't clear the queue - let messages continue
        }

        // Stop any pending bonus message coroutine (for legacy support)
        if (bonusMessageCoroutine != null)
        {
            StopCoroutine(bonusMessageCoroutine);
            bonusMessageCoroutine = null;
            bonusMessageStartTime = -999f;
            bonusMessageDuration = 0f;
            Debug.Log("GameManager: Stopped pending bonus message coroutine");
        }
    }

    // Update email canvas visibility based on current story point
    public void UpdateEmailCanvasVisibility()
    {
        if (emailCanvas == null)
        {
            Debug.LogError("GameManager: emailCanvas is NOT ASSIGNED in Inspector!");
            return;
        }

        Debug.Log($"GameManager: UpdateEmailCanvasVisibility called - Current story point: {storyValue}");
        Debug.Log($"GameManager: EmailCanvas state BEFORE: {emailCanvas.activeSelf}");

        // Check if we have a valid story point
        if (story != null && storyValue >= 0 && storyValue < story.Length && story[storyValue] != null)
        {
            bool shouldShow = story[storyValue].showEmailCanvas;
            emailCanvas.SetActive(shouldShow);
            Debug.Log($"GameManager: EmailCanvas set to {(shouldShow ? "VISIBLE" : "HIDDEN")} for story point {storyValue}");
            Debug.Log($"GameManager: EmailCanvas state AFTER: {emailCanvas.activeSelf}");
        }
        else
        {
            // Default to hidden if no valid story point
            emailCanvas.SetActive(false);
            Debug.Log("GameManager: EmailCanvas HIDDEN (no valid story point)");
            Debug.Log($"GameManager: EmailCanvas state AFTER: {emailCanvas.activeSelf}");
        }
    }

    // Advance to next story point
    public void AdvanceStory()
    {
        if (story != null && storyValue < story.Length - 1)
        {
            storyValue++;
            Debug.Log($"GameManager: Advanced to story point {storyValue}");

            // Check if this story point adds an email
            if (story[storyValue].addEmail && story[storyValue].emailToAdd != null)
            {
                AddEmail(story[storyValue].emailToAdd);
            }

            UpdateEmailCanvasVisibility();
        }
        else
        {
            Debug.LogWarning("GameManager: Cannot advance story - already at end");
        }
    }

    // Add an email to the emails array
    public void AddEmail(Email newEmail)
    {
        if (newEmail == null)
        {
            Debug.LogWarning("GameManager: Cannot add null email");
            return;
        }

        // Create a new array with one more slot
        Email[] newArray;
        if (emails == null || emails.Length == 0)
        {
            newArray = new Email[1];
            newArray[0] = newEmail;
        }
        else
        {
            newArray = new Email[emails.Length + 1];
            for (int i = 0; i < emails.Length; i++)
            {
                newArray[i] = emails[i];
            }
            newArray[emails.Length] = newEmail;
        }

        emails = newArray;
        Debug.Log($"GameManager: Email added! Total emails: {emailCount}, Priority emails: {priorityEmailCount}");

        // Update the UI display
        UpdateEmailCountDisplay();
    }

    // Get current story point index
    public int GetCurrentStoryPoint()
    {
        return storyValue;
    }

    // Get monitor messages for the current story point
    public MonitorMessage[] GetCurrentMonitorMessages()
    {
        if (story != null && storyValue >= 0 && storyValue < story.Length && story[storyValue] != null)
        {
            return story[storyValue].monitorMessages;
        }
        return null;
    }

    // Work Time Bar controls
    public void ResetWorkTimeBar()
    {
        if (workTimeBar != null)
        {
            workTimeBar.ResetTimer();
            Debug.Log("GameManager: Work time bar reset");
        }
        else
        {
            Debug.LogWarning("GameManager: workTimeBar is not assigned!");
        }
    }

    public float GetWorkTimeBarProgress()
    {
        if (workTimeBar != null)
        {
            return workTimeBar.GetProgress();
        }
        return 0f;
    }

    public bool IsWorkTimeBarComplete()
    {
        if (workTimeBar != null)
        {
            return workTimeBar.IsComplete();
        }
        return false;
    }

    public void OnWorkShiftComplete()
    {
        Debug.Log("=== GameManager: Work shift completed! Disabling all input ===");
        isWorkShiftComplete = true;

        // Don't show message here - WorkTimeBar already shows it

        // Disable carousel
        if (carousel != null)
        {
            carousel.DisableCarousel();
        }
    }

    public bool IsWorkShiftComplete()
    {
        return isWorkShiftComplete;
    }

    // Phone system - called when phone icon is clicked
    public void OnPhoneNumberCalled(string phoneNumber)
    {
        Debug.Log($"=== GameManager: Phone number called: '{phoneNumber}' ===");

        // Stop any previous call coroutine
        if (activeCallCoroutine != null)
        {
            StopCoroutine(activeCallCoroutine);
            Debug.Log("GameManager: Stopped previous call coroutine");
        }

        // Award points for making a call AND for success immediately
        AddScore(600); // 100 for calling + 500 for connection

        // Show calling message first (15 seconds - full rise time to reach top)
        ShowBonusMessage($"CALLING {phoneNumber}...", duration: 15f);

        // Show success message immediately after in queue
        ShowBonusMessage($"CALL CONNECTED! +500 POINTS", duration: 15f);

        // TODO: Add logic to check if this is the correct number for current story point
        // TODO: Trigger story events, play sounds, etc.
    }

    private System.Collections.IEnumerator ShowCallSuccessMessage(string phoneNumber)
    {
        // Wait for the first message to complete (15 seconds)
        yield return new WaitForSeconds(15f);

        // DON'T show success message - coroutines persist even when you change carousel items
        // The phone messages should only appear when emitted, not from delayed coroutines

        // Clear the coroutine reference
        activeCallCoroutine = null;
        Debug.Log($"GameManager: Call coroutine finished (NOT showing success message)");
    }
}