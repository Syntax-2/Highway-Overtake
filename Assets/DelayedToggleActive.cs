using UnityEngine;
using System.Collections; // Required for Coroutines

public class DelayedToggleActive : MonoBehaviour
{
    [Header("Target GameObject")]
    [Tooltip("The GameObject whose active state will be toggled.")]
    public GameObject targetGameObject;

    [Header("Delay Settings")]
    [Tooltip("The delay in seconds before the toggle action occurs.")]
    public float delay = 1.0f;

    // No longer need a private Button reference, as this script won't be on the button itself.

    void Start()
    {
        // Validate that a target GameObject has been assigned.
        // This script can now exist on an empty GameObject, so it doesn't need a Button component on itself.
        if (targetGameObject == null)
        {
            Debug.LogError("DelayedToggleActive: Target GameObject not assigned in the Inspector! This script needs a target to function.", this);
            // Optionally, you could disable this script if no target is set,
            // though it won't do anything until its public method is called.
            // enabled = false;
        }
    }

    /// <summary>
    /// Public method to be called by a UI Button's OnClick event (or from other scripts).
    /// This will initiate the delayed toggle process.
    /// </summary>
    public void TriggerDelayedToggle()
    {
        if (targetGameObject == null)
        {
            Debug.LogWarning("DelayedToggleActive: TriggerDelayedToggle called, but no Target GameObject is assigned.", this);
            return;
        }

        // Stop any existing coroutine for this target to prevent multiple toggles if button is spammed
        // (Optional, but good practice if the button can be clicked rapidly)
        StopAllCoroutines(); // Stops all coroutines started by this MonoBehaviour instance.
                             // If you have other coroutines in this script you don't want stopped,
                             // you'd need to store a reference to the specific coroutine and stop that.

        // Start the coroutine to handle the delayed toggle
        StartCoroutine(ToggleActiveAfterDelayCoroutine());
    }

    /// <summary>
    /// Coroutine that waits for the specified delay and then toggles the target GameObject's active state.
    /// </summary>
    private IEnumerator ToggleActiveAfterDelayCoroutine()
    {
        Debug.Log($"DelayedToggleActive: Starting delay of {delay} seconds for '{targetGameObject.name}'.", this);
        // Wait for the specified delay
        yield return new WaitForSeconds(delay);

        // Check if the targetGameObject still exists (it might have been destroyed during the delay)
        if (targetGameObject != null)
        {
            // Toggle the active state
            bool currentActiveState = targetGameObject.activeSelf;
            targetGameObject.SetActive(!currentActiveState);

            Debug.Log($"DelayedToggleActive: Toggled '{targetGameObject.name}' to {(!currentActiveState ? "active" : "inactive")} after {delay} seconds.", this);
        }
        else
        {
            Debug.LogWarning("DelayedToggleActive: Target GameObject was destroyed before the toggle action could complete.", this);
        }
    }

    // OnDestroy is no longer needed to remove a button listener, as we're not adding one directly.
}
