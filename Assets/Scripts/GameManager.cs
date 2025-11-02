using UnityEngine;
using TMPro;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }

    [Header("References")]
    public CircularCarousel carousel;
    public TextMeshProUGUI scoreText;
    public GameObject bonusMessageObject; // Reference to BonusMessage GameObject
    public GameObject emailCanvas; // Reference to Email Canvas
    public TextMeshProUGUI emailCountText; // Reference to Email Count UI text
    public TextMeshProUGUI priorityEmailCountText; // Reference to Priority Email Count UI text

    [Header("Bonus Message Settings")]
    public float bonusMessageStartY = -1400f; // Starting Y position for bonus message

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
                retroText.destroyWhenOffScreen = true;
                retroText.destroyHeight = 1000f;
                retroText.riseSpeed = 50f;
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
        Debug.Log($"=== GAMEMANAGER: OnCarouselObjectClicked called for {clickedObject.name} ===");
        Debug.Log($"State BEFORE: Carousel={isCarouselActive}, Monitor={isMonitorActive}");

        // Hide bonus message if it's showing
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
            monitor.SetGameManager(this);

            // Check if already enabled
            Debug.Log($"GameManager: Monitor enabled state BEFORE: {monitor.enabled}");
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
            Debug.Log("GameManager: First monitor exit - adding 20000 points and showing bonus message with delay");
            AddScore(20000);

            // Get message from story array
            string message = "FIRST TASK COMPLETE!"; // Default
            if (story != null && story.Length > 0 && storyValue < story.Length && story[storyValue] != null)
            {
                message = story[storyValue].bonusMessage;
            }

            bonusMessageCoroutine = StartCoroutine(ShowBonusMessageDelayed(message, 0.5f));

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

    public void ShowBonusMessage(string message)
    {
        if (bonusMessageObject != null)
        {
            Debug.Log($"GameManager: ShowBonusMessage called with message: {message}");

            // Reset position to starting Y position
            RectTransform rectTransform = bonusMessageObject.GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Store original position
                Vector3 originalPos = rectTransform.localPosition;
                // Start at configured Y position
                rectTransform.localPosition = new Vector3(originalPos.x, bonusMessageStartY, originalPos.z);
                Debug.Log($"GameManager: Reset bonus message position to y={bonusMessageStartY}");
            }

            // Update the text
            var bonusText = bonusMessageObject.GetComponent<TMP_Text>();
            if (bonusText == null)
            {
                bonusText = bonusMessageObject.GetComponent<TextMeshProUGUI>();
            }

            if (bonusText != null)
            {
                bonusText.text = message;
                Debug.Log($"GameManager: Set bonus text to: {message}");
            }
            else
            {
                Debug.LogError("GameManager: No TextMeshPro component found on bonusMessageObject!");
            }

            // Show the bonus message
            bonusMessageObject.SetActive(true);
            Debug.Log($"GameManager: BonusMessage shown");
        }
        else
        {
            Debug.LogWarning("GameManager: bonusMessageObject is not assigned!");
        }
    }

    public void HideBonusMessage()
    {
        Debug.Log("=== HideBonusMessage called ===");

        // Use Unity's implicit bool conversion to properly check for destroyed objects
        if (!bonusMessageObject)
        {
            Debug.Log("GameManager: bonusMessageObject is null or destroyed, nothing to hide");
            return;
        }

        Debug.Log($"GameManager: bonusMessageObject exists and active? {bonusMessageObject.activeSelf}");
        Debug.Log($"GameManager: Time.time: {Time.time}, bonusMessageStartTime: {bonusMessageStartTime}, bonusMessageDuration: {bonusMessageDuration}");

        // Check if we're within the timeout duration
        bool inTimeoutPeriod = (Time.time - bonusMessageStartTime) < bonusMessageDuration;
        Debug.Log($"GameManager: In timeout period? {inTimeoutPeriod}");

        if (inTimeoutPeriod)
        {
            Debug.Log($"GameManager: Click during timeout period! Time elapsed: {Time.time - bonusMessageStartTime}s / {bonusMessageDuration}s");
        }

        // Stop any pending bonus message coroutine
        if (bonusMessageCoroutine != null)
        {
            StopCoroutine(bonusMessageCoroutine);
            bonusMessageCoroutine = null;
            bonusMessageStartTime = -999f;
            bonusMessageDuration = 0f;
            Debug.Log("GameManager: Stopped pending bonus message coroutine");
        }

        // Accelerate the bonus message off screen if it's showing
        // Use try-catch to handle race condition where object might be destroyed between checks
        try
        {
            if (bonusMessageObject.activeSelf)
            {
                Debug.Log("GameManager: bonusMessageObject is active, looking for RetroArcadeText component");
                var retroText = bonusMessageObject.GetComponent<RetroArcadeText>();
                Debug.Log($"GameManager: RetroArcadeText found? {retroText != null}");

                if (retroText != null)
                {
                    Debug.Log($"GameManager: Current riseSpeed: {retroText.riseSpeed}");

                    // Always dramatically increase speed - make it shoot off screen fast!
                    // If already accelerated, make it even faster
                    float currentSpeed = retroText.riseSpeed;
                    float accelerationSpeed = currentSpeed < 100f ? 1000f : currentSpeed * 2f;

                    retroText.riseSpeed = accelerationSpeed;
                    Debug.Log($"GameManager: BonusMessage accelerating off screen - NEW speed: {accelerationSpeed} (was {currentSpeed})");
                }
                else
                {
                    // If no RetroArcadeText, just hide it
                    bonusMessageObject.SetActive(false);
                    Debug.Log("GameManager: BonusMessage hidden (no RetroArcadeText)");
                }
            }
            else
            {
                Debug.Log("GameManager: bonusMessageObject is not active, nothing to hide");
            }
        }
        catch (MissingReferenceException)
        {
            Debug.Log("GameManager: bonusMessageObject was destroyed between checks, ignoring");
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
}