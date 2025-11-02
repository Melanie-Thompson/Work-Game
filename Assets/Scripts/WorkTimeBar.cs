using UnityEngine;
using UnityEngine.Events;
using System.Collections.Generic;

public class WorkTimeBar : MonoBehaviour
{
    [Header("Bar Setup - Group References")]
    [Tooltip("The three child groups in order: Greens, Yellows, Reds")]
    public Transform greensGroup;
    public Transform yellowsGroup;
    public Transform redsGroup;

    [Header("Timing")]
    [Tooltip("Time in seconds for each piece to become visible")]
    public float secondsPerPiece = 2f;

    [Header("Events")]
    [Tooltip("Called when each group completes filling")]
    public UnityEvent<int> onGroupComplete;
    [Tooltip("Called when all groups are complete")]
    public UnityEvent onAllGroupsComplete;

    [Header("Score Settings")]
    [Tooltip("Points awarded when each group completes")]
    public int pointsPerGroup = 500;
    [Tooltip("Bonus points awarded when all groups complete")]
    public int bonusPointsOnComplete = 2000;

    private float elapsedTime = 0f;
    private List<Transform> allPieces = new List<Transform>();
    private int currentVisiblePieces = 0;
    private bool[] groupCompletedFlags;
    private bool allGroupsCompleted = false;
    private int[] groupStartIndices; // Where each group starts in the allPieces list
    private int[] groupEndIndices;   // Where each group ends in the allPieces list

    void Start()
    {
        Debug.Log("WorkTimeBar: Start() method called");

        // Validate groups
        if (greensGroup == null || yellowsGroup == null || redsGroup == null)
        {
            Debug.LogError("WorkTimeBar: One or more group references are not assigned!");
            return;
        }

        groupCompletedFlags = new bool[3];
        groupStartIndices = new int[3];
        groupEndIndices = new int[3];

        // Collect all children from each group in order
        int currentIndex = 0;

        // Greens group
        groupStartIndices[0] = currentIndex;
        foreach (Transform child in greensGroup)
        {
            allPieces.Add(child);
            child.gameObject.SetActive(false);
            Debug.Log($"WorkTimeBar: Added {child.name} to Greens group at index {currentIndex}");
            currentIndex++;
        }
        groupEndIndices[0] = currentIndex - 1;

        // Yellows group
        groupStartIndices[1] = currentIndex;
        foreach (Transform child in yellowsGroup)
        {
            allPieces.Add(child);
            child.gameObject.SetActive(false);
            Debug.Log($"WorkTimeBar: Added {child.name} to Yellows group at index {currentIndex}");
            currentIndex++;
        }
        groupEndIndices[1] = currentIndex - 1;

        // Reds group
        groupStartIndices[2] = currentIndex;
        foreach (Transform child in redsGroup)
        {
            allPieces.Add(child);
            child.gameObject.SetActive(false);
            Debug.Log($"WorkTimeBar: Added {child.name} to Reds group at index {currentIndex}");
            currentIndex++;
        }
        groupEndIndices[2] = currentIndex - 1;

        // Show the first piece immediately at start
        if (allPieces.Count > 0)
        {
            allPieces[0].gameObject.SetActive(true);
            currentVisiblePieces = 1;
            Debug.Log($"WorkTimeBar: First piece ({allPieces[0].name}) visible at start");
        }

        float totalTime = allPieces.Count * secondsPerPiece;
        Debug.Log($"WorkTimeBar: Initialized - {allPieces.Count} total pieces, {secondsPerPiece}s per piece, total time: {totalTime}s ({totalTime / 60f:F1} minutes)");
        Debug.Log($"WorkTimeBar: Greens: indices {groupStartIndices[0]}-{groupEndIndices[0]}, Yellows: {groupStartIndices[1]}-{groupEndIndices[1]}, Reds: {groupStartIndices[2]}-{groupEndIndices[2]}");
    }

    void Update()
    {
        // Increment elapsed time
        elapsedTime += Time.deltaTime;

        // Calculate how many pieces should be visible based on elapsed time
        // +1 because we start with the first piece already visible
        int targetVisiblePieces = Mathf.FloorToInt(elapsedTime / secondsPerPiece) + 1;
        targetVisiblePieces = Mathf.Clamp(targetVisiblePieces, 1, allPieces.Count);

        // Make newly visible pieces appear
        if (targetVisiblePieces > currentVisiblePieces)
        {
            for (int i = currentVisiblePieces; i < targetVisiblePieces; i++)
            {
                if (i < allPieces.Count)
                {
                    allPieces[i].gameObject.SetActive(true);
                    Debug.Log($"WorkTimeBar: Piece {i + 1}/{allPieces.Count} ({allPieces[i].name}) now visible");

                    // Check if we completed a group
                    for (int groupIndex = 0; groupIndex < 3; groupIndex++)
                    {
                        if (i == groupEndIndices[groupIndex] && !groupCompletedFlags[groupIndex])
                        {
                            groupCompletedFlags[groupIndex] = true;
                            OnGroupCompleted(groupIndex);
                        }
                    }
                }
            }
            currentVisiblePieces = targetVisiblePieces;
        }

        // Check if all pieces are complete
        if (!allGroupsCompleted && currentVisiblePieces >= allPieces.Count)
        {
            allGroupsCompleted = true;
            OnAllGroupsCompleted();
        }
    }

    void OnGroupCompleted(int groupIndex)
    {
        string[] groupNames = { "Greens", "Yellows", "Reds" };
        int piecesInGroup = groupEndIndices[groupIndex] - groupStartIndices[groupIndex] + 1;
        float minutesElapsed = (elapsedTime / 60f);

        Debug.Log($"WorkTimeBar: {groupNames[groupIndex]} group completed! ({piecesInGroup} pieces, {minutesElapsed:F2} minutes elapsed)");

        // Award points
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(pointsPerGroup);
            Debug.Log($"WorkTimeBar: Awarded {pointsPerGroup} points for {groupNames[groupIndex]} group");
        }

        // Invoke event
        onGroupComplete?.Invoke(groupIndex);
    }

    void OnAllGroupsCompleted()
    {
        Debug.Log($"WorkTimeBar: All {allPieces.Count} pieces completed! Total time: {elapsedTime / 60f:F2} minutes");

        // Award bonus points
        if (GameManager.Instance != null)
        {
            GameManager.Instance.AddScore(bonusPointsOnComplete);
            GameManager.Instance.ShowBonusMessage($"WORK SHIFT COMPLETE! +{bonusPointsOnComplete} BONUS!");
            Debug.Log($"WorkTimeBar: Awarded {bonusPointsOnComplete} bonus points for completing all groups");
        }

        // Invoke event
        onAllGroupsComplete?.Invoke();
    }

    // Public method to reset the timer
    public void ResetTimer()
    {
        elapsedTime = 0f;
        currentVisiblePieces = 0;
        allGroupsCompleted = false;

        // Hide all pieces
        for (int i = 0; i < allPieces.Count; i++)
        {
            if (allPieces[i] != null)
            {
                allPieces[i].gameObject.SetActive(false);
            }
        }

        // Reset group flags
        for (int i = 0; i < groupCompletedFlags.Length; i++)
        {
            groupCompletedFlags[i] = false;
        }

        Debug.Log("WorkTimeBar: Timer reset");
    }

    // Public method to get current progress (0-1)
    public float GetProgress()
    {
        return allPieces.Count > 0 ? (float)currentVisiblePieces / allPieces.Count : 0f;
    }

    // Public method to check if complete
    public bool IsComplete()
    {
        return currentVisiblePieces >= allPieces.Count;
    }

    void OnDrawGizmosSelected()
    {
        // Visualize the groups in the editor
        if (greensGroup != null)
        {
            Gizmos.color = Color.green;
            DrawGroupGizmo(greensGroup);
        }
        if (yellowsGroup != null)
        {
            Gizmos.color = Color.yellow;
            DrawGroupGizmo(yellowsGroup);
        }
        if (redsGroup != null)
        {
            Gizmos.color = Color.red;
            DrawGroupGizmo(redsGroup);
        }
    }

    void DrawGroupGizmo(Transform group)
    {
        Renderer[] renderers = group.GetComponentsInChildren<Renderer>(true);
        foreach (Renderer r in renderers)
        {
            Gizmos.DrawWireCube(r.bounds.center, r.bounds.size);
        }
    }
}
