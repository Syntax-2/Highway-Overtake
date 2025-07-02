using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for using List

public class AICarSpawner : MonoBehaviour
{
    [Header("Spawning Setup")]
    [Tooltip("List of AI Car prefabs to choose from when spawning.")]
    public List<GameObject> aiCarPrefabs = new List<GameObject>();

    // *** REMOVED: public Transform playerTransform; ***

    [Tooltip("The desired number of AI cars managed by this spawner.")]
    public int targetCarCount = 15;
    [Tooltip("How often (in seconds) to check if new cars need to be spawned.")]
    public float spawnCheckInterval = 1.0f;

    [Header("Spawn Positioning")]
    [Tooltip("The fixed X position for spawning (e.g., center of the right lane).")]
    public float spawnXPosition = 0f;
    [Tooltip("The fixed Y position for spawning.")]
    public float spawnYPosition = 0f;
    [Tooltip("Minimum distance (absolute value) along Z from the player to spawn a car.")]
    public float minSpawnDistanceZ = 30f;
    [Tooltip("Maximum distance (absolute value) along Z from the player to spawn a car.")]
    public float maxSpawnDistanceZ = 250f;

    [Header("Cleanup Settings")]
    [Tooltip("Enable cleaning up cars that are too far away?")]
    public bool enableCleanup = true;
    [Tooltip("Maximum distance (absolute value) along Z from the player before an AI car is removed.")]
    public float cleanupDistanceZ = 350f;
    [Tooltip("How often (in seconds) to check for cars to clean up.")]
    public float cleanupCheckInterval = 2.0f;

    [Header("Organization")]
    [Tooltip("Optional: Parent the spawned cars under this object for hierarchy neatness.")]
    public bool parentToThisObject = true;

    [Header("Overlap Check (Requires Layer Setting)")]
    [Tooltip("Layer mask for cars, used ONLY to prevent spawning cars inside each other. Should match the AI car script's mask.")]
    public LayerMask carLayerMask;

    // --- Private Variables ---
    private Transform _activePlayerCarTransform; // Will be set by PlayerCarManager
    private List<GameObject> spawnedCars = new List<GameObject>();
    private Coroutine _spawnManagerCoroutine;
    private Coroutine _cleanupCoroutine;
    private bool _isInitialized = false; // To prevent starting coroutines multiple times if car changes

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
                Debug.LogWarning("AICarSpawner: PlayerCarManager has no active car on Start. Waiting for OnPlayerCarChanged event.", this);
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

    void OnDestroy() // Or OnDisable()
    {
        // Unsubscribe from events to prevent errors
        if (PlayerCarManager.Instance != null)
        {
            PlayerCarManager.Instance.OnPlayerCarChanged -= HandleActivePlayerCarChanged;
        }
        StopAllCoroutinesIfActive(); // Ensure coroutines are stopped
    }

    void HandleActivePlayerCarChanged(GameObject newPlayerCar)
    {
        if (newPlayerCar != null)
        {
            _activePlayerCarTransform = newPlayerCar.transform;
            Debug.Log($"AICarSpawner: Player car reference updated to '{newPlayerCar.name}'.", this);

            // Validate settings now that we have a player transform (or re-validate)
            if (!ValidateSettings())
            {
                enabled = false; // Disable script if settings are invalid
                StopAllCoroutinesIfActive();
                _isInitialized = false;
                return;
            }

            // Start or restart coroutines only if not already initialized for this player
            // or if you want to reset spawning logic on player change
            if (!_isInitialized || (_spawnManagerCoroutine == null && _cleanupCoroutine == null)) // Check if coroutines need starting
            {
                StopAllCoroutinesIfActive(); // Stop any existing ones first

                _spawnManagerCoroutine = StartCoroutine(SpawnManagerCoroutine());
                if (enableCleanup)
                {
                    _cleanupCoroutine = StartCoroutine(CleanupFarAwayCarsCoroutine());
                }
                _isInitialized = true;
                Debug.Log("AICarSpawner: Coroutines started/restarted after player car change.");
            }
        }
        else
        {
            Debug.LogWarning("AICarSpawner: Active player car reference became null. Halting AI spawning.", this);
            _activePlayerCarTransform = null;
            StopAllCoroutinesIfActive();
            _isInitialized = false;
            // Consider clearing spawned AI cars here if the player disappears:
            // CleanAiCars();
        }
    }

    void StopAllCoroutinesIfActive()
    {
        if (_spawnManagerCoroutine != null)
        {
            StopCoroutine(_spawnManagerCoroutine);
            _spawnManagerCoroutine = null;
        }
        if (_cleanupCoroutine != null)
        {
            StopCoroutine(_cleanupCoroutine);
            _cleanupCoroutine = null;
        }
    }


    // --- Validation Function ---
    bool ValidateSettings()
    {
        // Validate AI car prefabs
        if (aiCarPrefabs == null || aiCarPrefabs.Count == 0)
        {
            Debug.LogError("AI Car Spawner: AI Car Prefabs list is not assigned or is empty!", this);
            return false;
        }
        for (int i = 0; i < aiCarPrefabs.Count; i++)
        {
            if (aiCarPrefabs[i] == null)
            {
                Debug.LogError($"AI Car Spawner: Element {i} in the AI Car Prefabs list is null!", this);
                return false;
            }
        }

        // *** MODIFIED: Check internal _activePlayerCarTransform instead of public one ***
        if (_activePlayerCarTransform == null)
        {
            // This log might be redundant if HandleActivePlayerCarChanged already logged an error
            // but good for direct validation call.
            Debug.LogError("AI Car Spawner: Active Player Car Transform is not set! Cannot validate further.", this);
            return false;
        }
        // *** END MODIFICATION ***

        if (minSpawnDistanceZ < 0 || maxSpawnDistanceZ < 0) { Debug.LogError("AI Car Spawner: Spawn distances Z cannot be negative!", this); return false; }
        if (minSpawnDistanceZ >= maxSpawnDistanceZ) { Debug.LogError("AI Car Spawner: Min Spawn Distance Z must be less than Max Spawn Distance Z!", this); return false; }
        if (spawnCheckInterval <= 0) { Debug.LogError("AI Car Spawner: Spawn Check Interval must be positive!", this); return false; }
        if (enableCleanup)
        {
            if (cleanupDistanceZ <= maxSpawnDistanceZ) { Debug.LogWarning("AI Car Spawner: Cleanup Distance Z should ideally be larger than Max Spawn Distance Z to avoid immediate cleanup.", this); }
            if (cleanupDistanceZ < 0) { Debug.LogError("AI Car Spawner: Cleanup Distance Z cannot be negative!", this); return false; }
            if (cleanupCheckInterval <= 0) { Debug.LogError("AI Car Spawner: Cleanup Check Interval must be positive!", this); return false; }
        }
        return true;
    }

    // --- Coroutines now use _activePlayerCarTransform ---
    IEnumerator SpawnManagerCoroutine()
    {
        // Small delay at start to ensure player transform is fully set
        yield return new WaitForSeconds(0.1f);
        Debug.Log("AICarSpawner: Spawn Manager Coroutine Started.", this);

        while (this.enabled)
        {
            if (_activePlayerCarTransform == null) // Use internal reference
            {
                Debug.LogWarning("AI Car Spawner: Player Transform lost in SpawnManager, stopping.", this);
                yield break;
            }

            spawnedCars.RemoveAll(item => item == null);

            if (spawnedCars.Count < targetCarCount)
            {
                bool success = TrySpawnOneCar();
                yield return new WaitForSeconds(0.1f); // Small delay after spawn attempt
            }

            yield return new WaitForSeconds(spawnCheckInterval);
        }
        Debug.Log("AICarSpawner: Spawn Manager Coroutine Exited.", this);
    }

    bool TrySpawnOneCar()
    {
        if (_activePlayerCarTransform == null || !GameManager.gameStarted) return false; // Use internal reference
        if (aiCarPrefabs == null || aiCarPrefabs.Count == 0)
        {
            // This check is also in ValidateSettings, but good for safety during runtime
            Debug.LogError("TrySpawnOneCar: Cannot spawn car, AI Car Prefabs list is empty or null!", this);
            return false;
        }

        float playerZ = _activePlayerCarTransform.position.z; // Use internal reference
        bool spawnInFront = Random.value > 0.5f;
        float distanceMagnitude = Random.Range(minSpawnDistanceZ, maxSpawnDistanceZ);
        float zOffset = spawnInFront ? distanceMagnitude : -distanceMagnitude;
        float targetZ = playerZ + zOffset;
        Vector3 spawnPosition = new Vector3(spawnXPosition, spawnYPosition, targetZ);

        float checkRadius = 3.0f;
        Collider[] hitColliders = Physics.OverlapSphere(spawnPosition + Vector3.up * 0.5f, checkRadius, carLayerMask);
        bool spotBlocked = false;
        foreach (Collider col in hitColliders)
        {
            if (col.attachedRigidbody != null) { spotBlocked = true; break; }
        }
        if (spotBlocked) { return false; }

        int randomIndex = Random.Range(0, aiCarPrefabs.Count);
        GameObject prefabToSpawn = aiCarPrefabs[randomIndex];
        GameObject newCar = Instantiate(prefabToSpawn, spawnPosition, Quaternion.LookRotation(Vector3.back)); // Assumes AI cars face -Z by default
        spawnedCars.Add(newCar);

        if (parentToThisObject) { newCar.transform.parent = this.transform; }
        return true;
    }

    IEnumerator CleanupFarAwayCarsCoroutine()
    {
        // Small delay at start
        yield return new WaitForSeconds(cleanupCheckInterval * 1.5f);
        Debug.Log("AICarSpawner: Cleanup Coroutine Started.", this);

        while (enableCleanup && this.enabled)
        {
            if (_activePlayerCarTransform == null) // Use internal reference
            {
                Debug.LogWarning("AI Car Spawner: Player Transform lost in Cleanup, stopping.", this);
                yield break;
            }

            float playerZ = _activePlayerCarTransform.position.z; // Use internal reference
            // int cleanupCount = 0; // Not used, can remove if not debugging

            for (int i = spawnedCars.Count - 1; i >= 0; i--)
            {
                GameObject car = spawnedCars[i];
                if (car == null) { spawnedCars.RemoveAt(i); continue; }

                float carZ = car.transform.position.z;
                float distanceZ = Mathf.Abs(playerZ - carZ); // Absolute distance works for any Z direction

                if (distanceZ > cleanupDistanceZ)
                {
                    Destroy(car);
                    spawnedCars.RemoveAt(i);
                    // cleanupCount++;
                }
            }
            // if (cleanupCount > 0) Debug.Log($"AI Car Spawner: Cleaned up {cleanupCount} cars.");
            yield return new WaitForSeconds(cleanupCheckInterval);
        }
        Debug.Log("AICarSpawner: Cleanup Coroutine Exited.", this);
    }

    // --- CleanAiCars function (Unchanged) ---
    public void CleanAiCars()
    {
        int count = spawnedCars.Count;
        Debug.Log($"AICarSpawner: Starting CleanAiCars. Destroying {count} tracked cars.");
        foreach (GameObject car in spawnedCars)
        {
            if (car != null) Destroy(car);
        }
        spawnedCars.Clear();
        Debug.Log($"AICarSpawner: CleanAiCars finished. List cleared.");
    }

    // OnDisable is not strictly needed if OnDestroy handles StopAllCoroutines
    // But it's good practice if the object might be disabled and re-enabled.
    // The coroutines already check this.enabled.
    // void OnDisable()
    // {
    //     StopAllCoroutinesIfActive();
    //     _isInitialized = false;
    // }
}
