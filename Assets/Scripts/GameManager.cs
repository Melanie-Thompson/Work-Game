using UnityEngine;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    
    [Header("References")]
    public CircularCarousel carousel;
    
    private ClickableMonitor currentActiveMonitor;
    private int score = 0;
    
    // Global state flags
    private bool isCarouselActive = true;
    private bool isMonitorActive = false;
    
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
    }
    
    void Start()
    {
        Debug.Log("=== GAMEMANAGER START ===");
        
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
            Debug.Log($"GameManager: Found monitor, enabling it");
            currentActiveMonitor = monitor;
            monitor.SetGameManager(this);
            monitor.enabled = true;
            Debug.Log($"GameManager: Enabled monitor on {clickedObject.name}");
        }
        else
        {
            Debug.LogError($"GameManager: No ClickableMonitor found on {clickedObject.name} or its children!");
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
        
        Debug.Log($"State AFTER: Carousel={isCarouselActive}, Monitor={isMonitorActive}");
    }
    
    // Add score method for Lever_Drag_Slide
    public void AddScore(int points)
    {
        score += points;
        Debug.Log($"GameManager: Score added! Total score: {score}");
    }
    
    public int GetScore()
    {
        return score;
    }
}