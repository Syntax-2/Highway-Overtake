using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List

public class ObstacleSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("List of Obstacle prefabs to choose from when spawning.")]
    public List<GameObject> obstaclePrefabs = new List<GameObject>(); // Use a List for variety
    // *** REMOVED: public Transform playerTransform; ***

    [Header("Spawning Parameters")]
    [Tooltip("How far ahead of the player (along Z, considering negative direction) to attempt spawning.")]
    public float spawnDistanceZ = 70f; // Often further than coins
    [Tooltip("Minimum distance player must travel along Z (negative direction) before next spawn attempt.")]
    public float spawnTriggerDistanceStep = 25f; // Can be different from coins
    [Tooltip("List of X coordinates representing the center of each lane where obstacles can spawn.")]
    public List<float> laneXPositions = new List<float>() { 0f }; // Default to one lane at X=0
    [Tooltip("Fixed Y position for spawning obstacles (relative to ground).")]
    public float spawnYPosition = 0.1f; // Adjust based on obstacle pivot points

    [Header("Spawn Chance & Density")]
    [Tooltip("Chance (0.0 to 1.0) that an obstacle will actually spawn when a trigger point is reached.")]
    [Range(0f, 1f)]
    public float spawnChance = 0.6f; // e.g., 60% chance to spawn each time

    [Header("Cleanup (Optional)")]
    [Tooltip("Enable destroying obstacles far behind the player?")]
    public bool enableCleanup = true;
    [Tooltip("Distance behind the player (along Z, positive relative direction) to destroy obstacles.")]
    public float cleanupDistanceZ = 40f;
    [Tooltip("How often (in seconds) to check for cleanup.")]
    public float cleanupCheckInterval = 5.0f;

    // --- Private Variables ---
    private Transform _activePlayerCarTransform; // Will be set by PlayerCarManager
    private float _lastSpawnTriggerZ; // Z position where the last spawn was triggered
    private List<GameObject> _spawnedObstacles = new List<GameObject>(); // Track obstacles for cleanup
    private Coroutine _cleanupCoroutine;
    private bool _isInitialized = false; // To manage coroutine starting

    void Awake()
    {
        Debug.Log("RoadSpawner AWAKE called. At this point, PlayerCarManager.Instance is: " + (PlayerCarManager.Instance == null ? "NULL" : "SET"));
    }

    void Start()
    {
        

        // Subscribe to PlayerCarManager events and get initial car
        if (PlayerCarManager.Instance != null)
        {
            PlayerCarManager.Instance.OnPlayerCarChanged += HandleActivePlayerCarChanged;
            // Get the initially selected car
            if (PlayerCarManager.Instance.CurrentPlayerCarGameObject != null)
            {
                HandleActivePlayerCarChanged(PlayerCarManager.Instance.CurrentPlayerCarGameObject);
            }
            else
            {
                Debug.LogWarning("ObstacleSpawner: PlayerCarManager has no active car on Start. Waiting for OnPlayerCarChanged event.", this);
            }
        }
        else
        {
            // If this error still appears, it confirms PlayerCarManager.Awake() never ran.
            Debug.LogError("RoadSpawner START: PlayerCarManager.Instance is null! Make sure PlayerCarManager is in the scene and initialized before RoadSpawner.");
            enabled = false;
            return;
        }
    }

    void OnDestroy() // Or OnDisable
    {
        // Unsubscribe from events to prevent errors
        if (PlayerCarManager.Instance != null)
        {
            PlayerCarManager.Instance.OnPlayerCarChanged -= HandleActivePlayerCarChanged;
        }
        StopCleanupCoroutineIfActive(); // Ensure coroutine is stopped
    }

    void HandleActivePlayerCarChanged(GameObject newPlayerCar)
    {
        if (newPlayerCar != null)
        {
            _activePlayerCarTransform = newPlayerCar.transform;
            Debug.Log($"ObstacleSpawner: Player car reference updated to '{newPlayerCar.name}'.", this);

            // Validate settings now that we have a player transform
            if (!ValidateSettings()) // ValidateSettings now uses _activePlayerCarTransform
            {
                enabled = false; // Disable script if settings are invalid
                StopCleanupCoroutineIfActive();
                _isInitialized = false;
                return;
            }

            // Initialize _lastSpawnTriggerZ based on the new player car's position
            _lastSpawnTriggerZ = _activePlayerCarTransform.position.z;
            Debug.Log($"ObstacleSpawner: Initial lastSpawnTriggerZ set to: {_lastSpawnTriggerZ} for new car.");

            // Start or restart cleanup coroutine
            if (enableCleanup)
            {
                StopCleanupCoroutineIfActive(); // Stop existing one first
                _cleanupCoroutine = StartCoroutine(CleanupFarAwayObstaclesCoroutine());
            }
            _isInitialized = true; // Mark as initialized with a player
            Debug.Log("ObstacleSpawner: Logic initialized/re-initialized after player car change.");
        }
        else
        {
            Debug.LogWarning("ObstacleSpawner: Active player car reference became null. Halting obstacle spawning.", this);
            _activePlayerCarTransform = null;
            StopCleanupCoroutineIfActive();
            _isInitialized = false;
            // Consider clearing spawned obstacles here if the player disappears
            // ClearAllSpawnedObstacles();
        }
    }

    void StopCleanupCoroutineIfActive()
    {
        if (_cleanupCoroutine != null)
        {
            StopCoroutine(_cleanupCoroutine);
            _cleanupCoroutine = null;
        }
    }

    bool ValidateSettings()
    {
        // Validate obstacle prefabs list
        if (obstaclePrefabs == null || obstaclePrefabs.Count == 0)
        {
            Debug.LogError("Obstacle Spawner: Obstacle Prefabs list is not assigned or is empty!", this);
            return false;
        }
        for (int i = 0; i < obstaclePrefabs.Count; i++)
        {
            if (obstaclePrefabs[i] == null)
            {
                Debug.LogError($"Obstacle Spawner: Element {i} in the Obstacle Prefabs list is null!", this);
                return false;
            }
            if (obstaclePrefabs[i].GetComponentInChildren<Collider>() == null)
            {
                Debug.LogWarning($"Obstacle Spawner: Prefab '{obstaclePrefabs[i].name}' seems to be missing a Collider.", this);
            }
        }

        // *** MODIFIED: Check internal _activePlayerCarTransform ***
        if (_activePlayerCarTransform == null)
        {
            // This might be called before HandleActivePlayerCarChanged sets it,
            // so this check is important if ValidateSettings is called from elsewhere too.
            // However, HandleActivePlayerCarChanged ensures it's set before calling ValidateSettings.
            Debug.LogError("Obstacle Spawner: Active Player Car Transform is not set! Cannot validate further.", this);
            return false;
        }
        // *** END MODIFICATION ***

        // Validate lane positions list
        if (laneXPositions == null || laneXPositions.Count == 0)
        {
            Debug.LogError("Obstacle Spawner: Lane X Positions list is empty! Please add at least one X coordinate in the Inspector.", this);
            return false;
        }

        // Validate distances and intervals
        if (spawnTriggerDistanceStep <= 0) { Debug.LogError("Obstacle Spawner: Spawn Trigger Distance Step must be positive!", this); return false; }
        if (enableCleanup && cleanupCheckInterval <= 0) { Debug.LogError("Obstacle Spawner: Cleanup Check Interval must be positive if cleanup is enabled!", this); return false; }

        return true;
    }

    void Update()
    {
        if (!_activePlayerCarTransform || !_isInitialized) return; // Do nothing if no player or not initialized

        float currentZ = _activePlayerCarTransform.position.z;
        float thresholdZ = _lastSpawnTriggerZ - spawnTriggerDistanceStep;

        // Debug.Log($"ObstacleSpawner Update: Player Z: {currentZ:F1}, Threshold Z: {thresholdZ:F1}");

        if (currentZ < thresholdZ)
        {
            if (Random.value <= spawnChance)
            {
                Debug.Log($"ObstacleSpawner: Player passed threshold ({currentZ:F1} < {thresholdZ:F1}) and passed spawn chance. Attempting spawn.");
                TrySpawnOneObstacle();
            }
            else
            {
                // Debug.Log($"ObstacleSpawner: Player passed threshold but failed spawn chance.");
            }
            _lastSpawnTriggerZ = currentZ;
        }
    }

    void TrySpawnOneObstacle()
    {
        // Redundant checks as Update already checks _activePlayerCarTransform, but good for safety
        if (_activePlayerCarTransform == null || obstaclePrefabs == null || obstaclePrefabs.Count == 0 || laneXPositions == null || laneXPositions.Count == 0) return;

        int laneIndex = Random.Range(0, laneXPositions.Count);
        float selectedX = laneXPositions[laneIndex];

        int prefabIndex = Random.Range(0, obstaclePrefabs.Count);
        GameObject prefabToSpawn = obstaclePrefabs[prefabIndex];

        float spawnZ = _activePlayerCarTransform.position.z - spawnDistanceZ;
        Vector3 spawnPosition = new Vector3(selectedX, spawnYPosition, spawnZ);

        Debug.Log($"ObstacleSpawner: Spawning obstacle '{prefabToSpawn.name}' in lane X={selectedX:F1} at Z={spawnZ:F1}");

        GameObject newObstacle = Instantiate(prefabToSpawn, spawnPosition, prefabToSpawn.transform.rotation);

        if (enableCleanup)
        {
            _spawnedObstacles.Add(newObstacle);
        }
    }

    IEnumerator CleanupFarAwayObstaclesCoroutine()
    {
        // Small delay at start to ensure player transform is fully set by HandleActivePlayerCarChanged
        yield return new WaitForSeconds(0.2f);
        Debug.Log("ObstacleSpawner: Cleanup Coroutine Started.", this);

        while (enableCleanup && this.enabled)
        {
            if (_activePlayerCarTransform == null) // Check if player is still valid
            {
                Debug.LogWarning("ObstacleSpawner: Player Transform lost in Cleanup, stopping.", this);
                yield break;
            }

            float playerZ = _activePlayerCarTransform.position.z;
            for (int i = _spawnedObstacles.Count - 1; i >= 0; i--)
            {
                GameObject obstacle = _spawnedObstacles[i];
                if (obstacle == null)
                {
                    _spawnedObstacles.RemoveAt(i);
                    continue;
                }

                // Obstacle is "behind" if its Z is greater than player's Z (since player moves to negative Z)
                // And far enough if obstacle.Z > player.Z + cleanupDistance
                if (obstacle.transform.position.z > playerZ + cleanupDistanceZ)
                {
                    // Debug.Log($"Cleaning up obstacle far behind player at Z={obstacle.transform.position.z:F1} (Player Z: {playerZ:F1})");
                    Destroy(obstacle);
                    _spawnedObstacles.RemoveAt(i);
                }
            }
            yield return new WaitForSeconds(cleanupCheckInterval);
        }
        Debug.Log("ObstacleSpawner: Cleanup Coroutine Exited.", this);
    }

    // Optional: Method to clear all spawned obstacles if needed (e.g., on game reset)
    public void ClearAllSpawnedObstacles()
    {
        foreach (GameObject obs in _spawnedObstacles)
        {
            if (obs != null) Destroy(obs);
        }
        _spawnedObstacles.Clear();
        Debug.Log("ObstacleSpawner: All spawned obstacles cleared.");
    }

    // OnDisable is not strictly needed if OnDestroy handles StopAllCoroutines
    // but it's good practice if the object might be disabled and re-enabled.
    // void OnDisable()
    // {
    //     StopCleanupCoroutineIfActive();
    //     _isInitialized = false;
    // }
}
