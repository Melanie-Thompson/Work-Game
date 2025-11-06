using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;

public class DialogueTextClickHandler : MonoBehaviour, IPointerClickHandler
{
    private CorporateHeadSpawner spawner;
    private CorporateHeadSpawner.PhoneNumberMapping mapping;

    void Awake()
    {
        Debug.Log($"DialogueTextClickHandler: Awake on {gameObject.name}");
    }

    public void Initialize(CorporateHeadSpawner spawner, CorporateHeadSpawner.PhoneNumberMapping mapping)
    {
        this.spawner = spawner;
        this.mapping = mapping;

        Debug.Log($"DialogueTextClickHandler: Initialized for {mapping.phoneNumber} on {gameObject.name}");

        // Make sure the text is raycast target
        var text = GetComponent<TextMeshProUGUI>();
        if (text != null)
        {
            text.raycastTarget = true;
            Debug.Log($"DialogueTextClickHandler: Set raycastTarget=true on text");

            // Expand the clickable area by adding padding to the RectTransform
            RectTransform rectTransform = GetComponent<RectTransform>();
            if (rectTransform != null)
            {
                // Add vertical padding only (top and bottom) - keep horizontal size the same
                Vector2 currentSize = rectTransform.sizeDelta;
                rectTransform.sizeDelta = new Vector2(currentSize.x, currentSize.y + 200f);
                Debug.Log($"DialogueTextClickHandler: Expanded clickable area from {currentSize} to {rectTransform.sizeDelta} (vertical only)");
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        Debug.Log($"*** DialogueTextClickHandler: TEXT CLICKED! ***");

        if (spawner != null && mapping != null)
        {
            spawner.OnSpeechBubbleClicked(mapping);
        }
        else
        {
            Debug.LogError("DialogueTextClickHandler: spawner or mapping is null!");
        }
    }
}
