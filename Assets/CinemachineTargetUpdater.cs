using UnityEngine;
using Unity.Cinemachine; // Required for Cinemachine components

// Updated to use CinemachineCamera as CinemachineVirtualCamera is obsolete
[RequireComponent(typeof(CinemachineCamera))]
public class CinemachineTargetUpdater : MonoBehaviour
{
    // Updated type to CinemachineCamera
    private CinemachineCamera _virtualCamera;

    void Awake()
    {
        // Get the CinemachineCamera component attached to this GameObject
        _virtualCamera = GetComponent<CinemachineCamera>();
        if (_virtualCamera == null)
        {
            Debug.LogError("CinemachineTargetUpdater: CinemachineCamera component not found on this GameObject!", this);
            enabled = false; // Disable this script if no virtual camera is found
            return;
        }
    }

    void Start()
    {
        // Subscribe to the PlayerCarManager's event and set initial target
        if (PlayerCarManager.Instance != null)
        {
            PlayerCarManager.Instance.OnPlayerCarChanged += HandleActivePlayerCarChanged;

            // Set initial target if a car is already active in PlayerCarManager
            if (PlayerCarManager.Instance.CurrentPlayerCarGameObject != null)
            {
                // Pass the correct GameObject to the handler
                HandleActivePlayerCarChanged(PlayerCarManager.Instance.CurrentPlayerCarGameObject);
            }
            else
            {
                Debug.LogWarning("CinemachineTargetUpdater: PlayerCarManager has no active car on Start. Camera will not have a target initially.", this);
                // Optionally, clear targets if no car is active
                if (_virtualCamera != null) // Ensure _virtualCamera is not null before accessing
                {
                    _virtualCamera.Follow = null;
                    _virtualCamera.LookAt = null;
                }
            }
        }
        else
        {
            Debug.LogError("CinemachineTargetUpdater: PlayerCarManager.Instance is null! Make sure PlayerCarManager is in the scene and initialized before this script.", this);
            // Optionally, disable camera or set a default target if PlayerCarManager is missing
            if (_virtualCamera != null) // Ensure _virtualCamera is not null
            {
                _virtualCamera.Follow = null;
                _virtualCamera.LookAt = null;
            }
            enabled = false;
        }
    }

    void OnDestroy() // Or OnDisable
    {
        // Unsubscribe from the event when this object is destroyed or disabled to prevent errors
        if (PlayerCarManager.Instance != null)
        {
            PlayerCarManager.Instance.OnPlayerCarChanged -= HandleActivePlayerCarChanged;
        }
    }

    /// <summary>
    /// Called when the PlayerCarManager signals a change in the active player car.
    /// Updates the Cinemachine Camera's Follow and LookAt targets.
    /// </summary>
    /// <param name="newActivePlayerCar">The new GameObject that is the active player car.</param>
    private void HandleActivePlayerCarChanged(GameObject newActivePlayerCar) // Parameter name is correct here
    {
        if (_virtualCamera == null)
        {
            Debug.LogError("CinemachineTargetUpdater: _virtualCamera is null in HandleActivePlayerCarChanged. This should not happen if Awake succeeded.", this);
            return;
        }

        if (newActivePlayerCar != null)
        {
            Transform newTargetTransform = newActivePlayerCar.transform;
            _virtualCamera.Follow = newTargetTransform;
            _virtualCamera.LookAt = newTargetTransform; // Often, Follow and LookAt are the same for car cameras
            Debug.Log($"CinemachineTargetUpdater: Camera target updated to '{newActivePlayerCar.name}'.");
        }
        else
        {
            // If the new active car is null (e.g., player car destroyed and not replaced)
            _virtualCamera.Follow = null;
            _virtualCamera.LookAt = null;
            Debug.LogWarning("CinemachineTargetUpdater: Active player car became null. Camera targets cleared.");
        }
    }
}
