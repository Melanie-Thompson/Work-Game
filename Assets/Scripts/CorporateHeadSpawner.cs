using UnityEngine;
using TMPro;

public class CorporateHeadSpawner : MonoBehaviour
{
    [System.Serializable]
    public class PhoneNumberMapping
    {
        [Tooltip("Phone number to dial (e.g., '#111')")]
        public string phoneNumber;

        [Tooltip("Corporate Head GameObject to spawn/activate")]
        public GameObject corporateHead;

        [Header("Transform Settings")]
        [Tooltip("Spawn position in world space")]
        public Vector3 spawnPosition = Vector3.zero;

        [Tooltip("Spawn rotation in Euler angles")]
        public Vector3 spawnRotation = Vector3.zero;

        [Tooltip("Scale of the spawned object")]
        public Vector3 spawnScale = Vector3.one;

        [Tooltip("If true, activates existing object. If false, instantiates a new one")]
        public bool activateExisting = true;

        [Tooltip("If true, uses the spawn rotation/position/scale. If false, keeps the object's original transform")]
        public bool overrideTransform = false;

        [Header("Dialogue Settings")]
        [Tooltip("Speech bubble GameObject (child of Corporate Head)")]
        public GameObject speechBubble;

        [Tooltip("UI Text to display dialogue (in PhoneCanvas)")]
        public TextMeshProUGUI speechBubbleText;

        [Tooltip("Delay before showing speech bubble (seconds)")]
        public float speechBubbleDelay = 0.2f;

        [Tooltip("Array of things this Corporate Head can say")]
        [TextArea(2, 5)]
        public string[] dialogue;

        [Tooltip("Show dialogue when Corporate Head appears")]
        public bool showDialogueOnSpawn = true;

        [Tooltip("If true, shows random dialogue. If false, shows in order")]
        public bool randomDialogue = true;

        [HideInInspector]
        public int currentDialogueIndex = 0;

        [HideInInspector]
        public bool allDialogueShown = false;
    }

    [Header("Phone Number to Corporate Head Mappings")]
    [Tooltip("Map phone numbers to Corporate Head GameObjects")]
    public PhoneNumberMapping[] phoneNumberMappings;

    [Header("Phone UI References")]
    [Tooltip("Phone hang-up icon to show after all dialogue is displayed")]
    public GameObject phoneHangUpIcon;

    [Header("Spawn Settings")]
    [Tooltip("Parent transform for spawned/activated heads (optional)")]
    public Transform spawnParent;

    [Tooltip("Award bonus points when a head is spawned")]
    public int bonusPoints = 1000;

    [Tooltip("Show bonus message when head spawns")]
    public bool showBonusMessage = true;

    [Tooltip("Bonus message to show (use {0} for phone number)")]
    public string bonusMessageTemplate = "CORPORATE HEAD SUMMONED!";

    void Awake()
    {
        Debug.Log("=== CorporateHeadSpawner: Awake called ===");
        // Initially hide all corporate heads that will be activated (do this in Awake to hide them ASAP)
        HideAllCorporateHeadsAtStart();
    }

    void Start()
    {
        Debug.Log("=== CorporateHeadSpawner: Start called ===");
        // Double-check they're hidden (backup to Awake)
        HideAllCorporateHeadsAtStart();
    }

    void HideAllCorporateHeadsAtStart()
    {
        if (phoneNumberMappings == null || phoneNumberMappings.Length == 0)
        {
            Debug.LogWarning("CorporateHeadSpawner: No phone number mappings configured!");
            return;
        }

        foreach (var mapping in phoneNumberMappings)
        {
            if (mapping.activateExisting && mapping.corporateHead != null)
            {
                bool wasActive = mapping.corporateHead.activeSelf;
                mapping.corporateHead.SetActive(false);
                Debug.Log($"CorporateHeadSpawner: Hiding {mapping.corporateHead.name} for phone number '{mapping.phoneNumber}' (was active: {wasActive})");
            }
            else if (mapping.corporateHead == null)
            {
                Debug.LogWarning($"CorporateHeadSpawner: Corporate Head is NULL for phone number '{mapping.phoneNumber}'!");
            }

            // Also hide speech bubbles and text initially
            if (mapping.speechBubble != null)
            {
                mapping.speechBubble.SetActive(false);
                Debug.Log($"CorporateHeadSpawner: Hiding speech bubble for phone number '{mapping.phoneNumber}'");
            }

            if (mapping.speechBubbleText != null)
            {
                mapping.speechBubbleText.gameObject.SetActive(false);
                Debug.Log($"CorporateHeadSpawner: Hiding speech bubble text for phone number '{mapping.phoneNumber}'");
            }
        }
    }

    // Called by GameManager when a phone number is dialed AND phone icon is clicked
    public void OnPhoneNumberCalled(string phoneNumber)
    {
        Debug.Log($"=== CorporateHeadSpawner: OnPhoneNumberCalled with '{phoneNumber}' ===");
        Debug.Log("STACK TRACE:");
        Debug.Log(System.Environment.StackTrace);

        // Find matching phone number
        foreach (var mapping in phoneNumberMappings)
        {
            Debug.Log($"CorporateHeadSpawner: Comparing '{phoneNumber}' with '{mapping.phoneNumber}'");
            if (mapping.phoneNumber == phoneNumber)
            {
                Debug.Log($"CorporateHeadSpawner: MATCH FOUND! Spawning {mapping.corporateHead.name}");
                SpawnCorporateHead(mapping);
                return;
            }
        }

        Debug.Log($"CorporateHeadSpawner: No mapping found for phone number '{phoneNumber}'");
    }

    void SpawnCorporateHead(PhoneNumberMapping mapping)
    {
        if (mapping.corporateHead == null)
        {
            Debug.LogError($"CorporateHeadSpawner: Corporate head GameObject is null for phone number {mapping.phoneNumber}!");
            return;
        }

        GameObject spawnedHead = null;

        if (mapping.activateExisting)
        {
            // Activate existing object
            mapping.corporateHead.SetActive(true);
            spawnedHead = mapping.corporateHead;
            Debug.Log($"CorporateHeadSpawner: Activated existing Corporate Head: {mapping.corporateHead.name}");

            // Only override transform if specified
            if (mapping.overrideTransform)
            {
                spawnedHead.transform.position = mapping.spawnPosition;
                spawnedHead.transform.rotation = Quaternion.Euler(mapping.spawnRotation);
                spawnedHead.transform.localScale = mapping.spawnScale;
                Debug.Log($"CorporateHeadSpawner: Override transform - pos:{mapping.spawnPosition}, rot:{mapping.spawnRotation}, scale:{mapping.spawnScale}");
            }
            else
            {
                Debug.Log($"CorporateHeadSpawner: Keeping original transform");
            }
        }
        else
        {
            // Instantiate new object
            spawnedHead = Instantiate(
                mapping.corporateHead,
                mapping.spawnPosition,
                Quaternion.Euler(mapping.spawnRotation),
                spawnParent
            );
            spawnedHead.transform.localScale = mapping.spawnScale;
            Debug.Log($"CorporateHeadSpawner: Instantiated new Corporate Head: {spawnedHead.name}");
        }

        // Hide the hang-up icon when corporate head spawns and mark dialogue as active
        if (phoneHangUpIcon != null)
        {
            phoneHangUpIcon.SetActive(false);
            Debug.Log($"CorporateHeadSpawner: Hidden hang-up icon for {mapping.phoneNumber}");
        }

        // Set dialogue as active to prevent hang-up icon from showing
        DialRotaryPhone.IsDialogueActive = true;

        // Reset dialogue tracking
        mapping.currentDialogueIndex = 0;
        mapping.allDialogueShown = false;

        // Award bonus points
        if (bonusPoints > 0 && GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(bonusPoints);
            Debug.Log($"CorporateHeadSpawner: Awarded {bonusPoints} bonus points!");
        }

        // Show bonus message (only the summon message, not dialogue)
        if (showBonusMessage && GameManager.Instance != null)
        {
            string message = string.Format(bonusMessageTemplate, mapping.phoneNumber);
            GameManager.Instance.ShowBonusMessage(message, duration: 3f, priority: 15);
        }

        // Dialogue will be shown on the speech bubble itself, not as bonus messages

        // Show speech bubble after a delay
        if (mapping.speechBubble != null)
        {
            StartCoroutine(ShowSpeechBubbleDelayed(mapping));
        }
    }

    System.Collections.IEnumerator ShowSpeechBubbleDelayed(PhoneNumberMapping mapping)
    {
        // Wait for the specified delay
        yield return new WaitForSeconds(mapping.speechBubbleDelay);

        Debug.Log($"=== ShowSpeechBubbleDelayed: After delay for {mapping.phoneNumber} ===");
        Debug.Log($"  speechBubble is null? {mapping.speechBubble == null}");
        Debug.Log($"  speechBubbleText is null? {mapping.speechBubbleText == null}");
        Debug.Log($"  dialogue is null? {mapping.dialogue == null}");
        Debug.Log($"  dialogue.Length: {(mapping.dialogue != null ? mapping.dialogue.Length : 0)}");

        // Show the speech bubble
        if (mapping.speechBubble != null)
        {
            mapping.speechBubble.SetActive(true);
            Debug.Log($"CorporateHeadSpawner: Speech bubble GameObject activated for {mapping.phoneNumber}");

            // Show dialogue text
            if (mapping.speechBubbleText != null && mapping.dialogue != null && mapping.dialogue.Length > 0)
            {
                // Reset dialogue index
                mapping.currentDialogueIndex = 0;
                mapping.allDialogueShown = false;

                // Show first line
                string firstLine = mapping.dialogue[0];
                mapping.speechBubbleText.text = firstLine;
                mapping.speechBubbleText.gameObject.SetActive(true);

                Debug.Log($"=== SETTING TEXT ===");
                Debug.Log($"  Text set to: '{firstLine}'");
                Debug.Log($"  Text GameObject: {mapping.speechBubbleText.gameObject.name}");
                Debug.Log($"  Text GameObject active? {mapping.speechBubbleText.gameObject.activeSelf}");
                Debug.Log($"  Text color: {mapping.speechBubbleText.color}");
                Debug.Log($"  Text font: {(mapping.speechBubbleText.font != null ? mapping.speechBubbleText.font.name : "NULL")}");
                Debug.Log($"  Text fontSize: {mapping.speechBubbleText.fontSize}");
                Debug.Log($"  *** CLICK SPEECH BUBBLE TO ADVANCE DIALOGUE ***");

                // Add click handler script directly to text component (UI-based approach)
                var clickHandler = mapping.speechBubbleText.GetComponent<DialogueTextClickHandler>();
                if (clickHandler == null)
                {
                    clickHandler = mapping.speechBubbleText.gameObject.AddComponent<DialogueTextClickHandler>();
                    Debug.Log("CorporateHeadSpawner: Added DialogueTextClickHandler to text component");
                }

                // Initialize the click handler with mapping reference
                clickHandler.Initialize(this, mapping);

                Debug.Log($"CorporateHeadSpawner: Dialogue text click handler configured");
            }
            else
            {
                Debug.LogError($"CorporateHeadSpawner: Cannot show dialogue - speechBubbleText null? {mapping.speechBubbleText == null}, dialogue null? {mapping.dialogue == null}, length: {(mapping.dialogue != null ? mapping.dialogue.Length : 0)}");
            }
        }
        else
        {
            Debug.LogError($"CorporateHeadSpawner: speechBubble is NULL for {mapping.phoneNumber}");
        }
    }

    public void OnSpeechBubbleClicked(PhoneNumberMapping mapping)
    {
        Debug.Log($"=== OnSpeechBubbleClicked for {mapping.phoneNumber} ===");

        if (mapping.dialogue == null || mapping.dialogue.Length == 0)
        {
            Debug.LogWarning("CorporateHeadSpawner: No dialogue to show");
            return;
        }

        // Move to next dialogue line
        mapping.currentDialogueIndex++;

        // Check if we've shown all dialogue
        if (mapping.currentDialogueIndex >= mapping.dialogue.Length)
        {
            Debug.Log($"CorporateHeadSpawner: All dialogue shown for {mapping.phoneNumber}!");
            mapping.allDialogueShown = true;

            // Hide the speech bubble
            if (mapping.speechBubble != null)
            {
                mapping.speechBubble.SetActive(false);
                Debug.Log("CorporateHeadSpawner: Hidden speech bubble");
            }

            // Also hide the text
            if (mapping.speechBubbleText != null)
            {
                mapping.speechBubbleText.gameObject.SetActive(false);
                Debug.Log("CorporateHeadSpawner: Hidden speech bubble text");
            }

            // Mark dialogue as inactive so hang-up icon can be shown
            DialRotaryPhone.IsDialogueActive = false;

            // Show the hang-up icon
            if (phoneHangUpIcon != null)
            {
                phoneHangUpIcon.SetActive(true);
                Debug.Log("CorporateHeadSpawner: Shown hang-up icon - you can now hang up!");
            }
            else
            {
                Debug.LogWarning("CorporateHeadSpawner: phoneHangUpIcon is not assigned!");
            }
        }
        else
        {
            // Show next dialogue line
            if (mapping.speechBubbleText != null)
            {
                mapping.speechBubbleText.text = mapping.dialogue[mapping.currentDialogueIndex];
                Debug.Log($"CorporateHeadSpawner: Showing dialogue line {mapping.currentDialogueIndex}/{mapping.dialogue.Length}: '{mapping.dialogue[mapping.currentDialogueIndex]}'");
            }
        }
    }

    string GetDialogueLine(PhoneNumberMapping mapping)
    {
        if (mapping.dialogue == null || mapping.dialogue.Length == 0)
        {
            return "";
        }

        string line;
        if (mapping.randomDialogue)
        {
            // Pick a random dialogue line
            int randomIndex = Random.Range(0, mapping.dialogue.Length);
            line = mapping.dialogue[randomIndex];
        }
        else
        {
            // Show dialogue in order
            line = mapping.dialogue[mapping.currentDialogueIndex];
            mapping.currentDialogueIndex = (mapping.currentDialogueIndex + 1) % mapping.dialogue.Length;
        }

        return line;
    }

    // Public method to hide a Corporate Head by phone number
    public void HideCorporateHead(string phoneNumber)
    {
        foreach (var mapping in phoneNumberMappings)
        {
            if (mapping.phoneNumber == phoneNumber && mapping.activateExisting)
            {
                if (mapping.corporateHead != null)
                {
                    mapping.corporateHead.SetActive(false);
                    Debug.Log($"CorporateHeadSpawner: Hidden Corporate Head for phone number {phoneNumber}");
                }

                // Also hide the speech bubble and text
                if (mapping.speechBubble != null)
                {
                    mapping.speechBubble.SetActive(false);
                    Debug.Log($"CorporateHeadSpawner: Hidden speech bubble for phone number {phoneNumber}");
                }

                if (mapping.speechBubbleText != null)
                {
                    mapping.speechBubbleText.gameObject.SetActive(false);
                    Debug.Log($"CorporateHeadSpawner: Hidden speech bubble text for phone number {phoneNumber}");
                }
                return;
            }
        }
    }

    // Public method to hide all Corporate Heads
    public void HideAllCorporateHeads()
    {
        foreach (var mapping in phoneNumberMappings)
        {
            if (mapping.activateExisting && mapping.corporateHead != null)
            {
                mapping.corporateHead.SetActive(false);
            }

            // Also hide speech bubbles
            if (mapping.speechBubble != null)
            {
                mapping.speechBubble.SetActive(false);
            }
        }
        Debug.Log("CorporateHeadSpawner: Hidden all Corporate Heads and speech bubbles");
    }

    // Public method to make a Corporate Head speak (by phone number)
    public void SpeakDialogue(string phoneNumber)
    {
        foreach (var mapping in phoneNumberMappings)
        {
            if (mapping.phoneNumber == phoneNumber)
            {
                if (mapping.dialogue != null && mapping.dialogue.Length > 0 && GameManager.Instance != null)
                {
                    string dialogueLine = GetDialogueLine(mapping);
                    GameManager.Instance.ShowBonusMessage(dialogueLine, duration: 5f, priority: 20);
                    Debug.Log($"CorporateHeadSpawner: {phoneNumber} says: '{dialogueLine}'");
                }
                return;
            }
        }
    }
}
