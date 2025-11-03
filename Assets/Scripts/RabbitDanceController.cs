using UnityEngine;

public class RabbitDanceController : MonoBehaviour
{
    void Start()
    {
        // Get the Animator component
        Animator animator = GetComponent<Animator>();

        if (animator != null)
        {
            Debug.Log("Rabbit Animator found! Starting dance animation.");

            // Make sure the animator is enabled
            animator.enabled = true;

            // Play the dance animation
            animator.Play("RabbitDance");
        }
        else
        {
            Debug.LogError("No Animator component found on Rabbit!");
        }

        // Log the position for debugging
        Debug.Log($"Rabbit position: {transform.position}");
        Debug.Log($"Rabbit scale: {transform.localScale}");
    }
}
