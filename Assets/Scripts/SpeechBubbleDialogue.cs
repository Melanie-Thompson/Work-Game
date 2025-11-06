using UnityEngine;
using TMPro;
using UnityEngine.EventSystems;

public class SpeechBubbleDialogue : MonoBehaviour, IPointerClickHandler
{
    [Header("References")]
    [Tooltip("TextMeshPro component to display dialogue")]
    public TextMeshProUGUI dialogueText;

    [Header("Dialogue Settings")]
    [Tooltip("Array of dialogue lines to cycle through")]
    [TextArea(2, 5)]
    public string[] dialogueLines;

    [Tooltip("If true, cycles through dialogue in order. If false, shows random lines")]
    public bool cycleThroughInOrder = true;

    [Tooltip("If true, loops back to start when reaching the end")]
    public bool loopDialogue = true;

    private int currentIndex = 0;

    void OnEnable()
    {
        // Show the first line when the speech bubble appears
        currentIndex = 0;
        ShowCurrentLine();

        // Auto-advance through dialogue every 3 seconds
        InvokeRepeating("NextLine", 3f, 3f);
    }

    void OnDisable()
    {
        // Stop auto-advancing when hidden
        CancelInvoke("NextLine");
    }

    // Called when the speech bubble is clicked (Unity UI Event System)
    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log("SpeechBubbleDialogue: Clicked!");
        NextLine();
    }

    // Advance to the next dialogue line
    public void NextLine()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            Debug.LogWarning("SpeechBubbleDialogue: No dialogue lines configured!");
            return;
        }

        if (cycleThroughInOrder)
        {
            // Move to next line
            currentIndex++;

            // Check if we've reached the end
            if (currentIndex >= dialogueLines.Length)
            {
                if (loopDialogue)
                {
                    currentIndex = 0; // Loop back to start
                }
                else
                {
                    currentIndex = dialogueLines.Length - 1; // Stay on last line
                }
            }
        }
        else
        {
            // Pick a random line
            currentIndex = Random.Range(0, dialogueLines.Length);
        }

        ShowCurrentLine();
    }

    // Display the current dialogue line
    void ShowCurrentLine()
    {
        if (dialogueLines == null || dialogueLines.Length == 0)
        {
            if (dialogueText != null)
            {
                dialogueText.text = "";
            }
            return;
        }

        if (dialogueText != null)
        {
            dialogueText.text = dialogueLines[currentIndex];
            Debug.Log($"SpeechBubbleDialogue: Showing line {currentIndex}: '{dialogueLines[currentIndex]}'");
        }
        else
        {
            Debug.LogError("SpeechBubbleDialogue: dialogueText is not assigned!");
        }
    }

    // Public method to set dialogue from external script
    public void SetDialogue(string[] newDialogue, bool randomOrder = false)
    {
        dialogueLines = newDialogue;
        cycleThroughInOrder = !randomOrder;
        currentIndex = 0;
        ShowCurrentLine();
    }

    // Public method to show a specific line
    public void ShowLine(int index)
    {
        if (dialogueLines != null && index >= 0 && index < dialogueLines.Length)
        {
            currentIndex = index;
            ShowCurrentLine();
        }
    }
}
