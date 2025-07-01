using UnityEngine;
using System.Collections;
using System.Collections.Generic; // Required for List

public class CoinSpawner : MonoBehaviour
{
    [Header("References")]
    [Tooltip("List of Coin prefabs (each with CoinCollector script) to choose from.")]
    public List<GameObject> coinPrefabs = new List<GameObject>();
    // *** REMOVED: public Transform playerTransform; ***

    [Header("Spawning Parameters")]
    [Tooltip("How far ahead of the player (along Z, considering negative direction) to attempt spawning.")]
    public float spawnDistanceZ = 50f; // Still positive, represents distance magnitude
    [Tooltip("Minimum distance player must travel along Z (negative direction) before next spawn attempt.")]
    public float spawnTriggerDistanceStep = 40f; // Still positive, represents distance magnitude
    [Tooltip("Minimum number of coins in a group.")]
    public int minCoinsPerGroup = 3;
    [Tooltip("Maximum number of coins in a group.")]
    public int maxCoinsPerGroup = 8;
    [Tooltip("Spacing between individual coins within a group along the Z axis.")]
    public float coinSpacingZ = 1.5f;
    [Tooltip("List of X coordinates representing the center of each lane where coins can spawn.")]
    public List<float> laneXPositions = new List<float>() { 0f }; // Default to one lane at X=0
    [Tooltip("Fixed Y position for spawning coins.")]
    public float spawnYPosition = 1.0f; // Adjust so coins aren't in the ground

    [Header("Cleanup (Optional)")]
    [Tooltip("Enable destroying coins far behind the player?")]
    public bool enableCleanup = true;
    [Tooltip("Distance behind the player (along Z, positive relative direction) to destroy coins.")]
    public float cleanupDistanceZ = 50f; // Still positive, represents distance magnitude
    [Tooltip("How often (in seconds) to check for cleanup.")]
    public float cleanupCheckInterval = 5.0f;

    // --- Private Variables ---
    private Transform _activePlayerCarTransform; // Will be set by PlayerCarManager
    private float _lastSpawnTriggerZ; // Z position where the last spawn was triggered
    private List<GameObject> _spawnedCoins = new List<GameObject>(); // Track coins for cleanup
    private Coroutine _cleanupCoroutine;
    private bool _isInitialized = false; // To manage coroutine starting

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
                Debug.LogWarning("CoinSpawner: PlayerCarManager has no active car on Start. Waiting for OnPlayerCarChanged event.", this);
            }
        }
        else
        {
            Debug.LogError("CoinSpawner: PlayerCarManager.Instance is null! Make sure PlayerCarManager is in the scene and initialized before CoinSpawner.", this);
            enabled = false; // Disable spawner if it can't get the player manager
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
            Debug.Log($"CoinSpawner: Player car reference updated to '{newPlayerCar.name}'.", this);

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
            Debug.Log($"CoinSpawner: Initial lastSpawnTriggerZ set to: {_lastSpawnTriggerZ} for new car.");

            // Start or restart cleanup coroutine
            if (enableCleanup)
            {
                StopCleanupCoroutineIfActive(); // Stop existing one first
                _cleanupCoroutine = StartCoroutine(CleanupFarAwayCoinsCoroutine());
            }
            _isInitialized = true; // Mark as initialized with a player
            Debug.Log("CoinSpawner: Logic initialized/re-initialized after player car change.");
        }
        else
        {
            Debug.LogWarning("CoinSpawner: Active player car reference became null. Halting coin spawning.", this);
            _activePlayerCarTransform = null;
            StopCleanupCoroutineIfActive();
            _isInitialized = false;
            // ClearAllSpawnedCoins(); // Optional: if you want to remove coins when player disappears
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
        // Validate the List of coin prefabs
        if (coinPrefabs == null || coinPrefabs.Count == 0)
        {
            Debug.LogError("Coin Spawner: Coin Prefabs list is not assigned or is empty!", this);
            return false;
        }
        for (int i = 0; i < coinPrefabs.Count; i++)
        {
            if (coinPrefabs[i] == null)
            {
                Debug.LogError($"Coin Spawner: Element {i} in the Coin Prefabs list is null!", this);
                return false;
            }
            if (coinPrefabs[i].GetComponent<CoinCollector>() == null) // Assuming CoinCollector script exists
            {
                Debug.LogWarning($"Coin Spawner: Prefab '{coinPrefabs[i].name}' at index {i} is missing the CoinCollector script.", this);
            }
            Collider prefabCollider = coinPrefabs[i].GetComponent<Collider>();
            if (prefabCollider == null || !prefabCollider.isTrigger)
            {
                Debug.LogWarning($"Coin Spawner: Prefab '{coinPrefabs[i].name}' at index {i} needs a Collider component set to 'Is Trigger'.", this);
            }
        }

        // *** MODIFIED: Check internal _activePlayerCarTransform ***
        if (_activePlayerCarTransform == null)
        {
            Debug.LogError("Coin Spawner: Active Player Car Transform is not set! Cannot validate further.", this);
            return false;
        }
        // *** END MODIFICATION ***

        // Validate lane positions list
        if (laneXPositions == null || laneXPositions.Count == 0)
        {
            Debug.LogError("Coin Spawner: Lane X Positions list is empty! Please add at least one X coordinate in the Inspector.", this);
            return false;
        }
        // Validate distances and intervals
        if (spawnTriggerDistanceStep <= 0) { Debug.LogError("Coin Spawner: Spawn Trigger Distance Step must be positive!", this); return false; }
        if (minCoinsPerGroup <= 0 || maxCoinsPerGroup < minCoinsPerGroup) { Debug.LogError("Coin Spawner: Invalid Min/Max Coins Per Group settings!", this); return false; }
        if (coinSpacingZ <= 0) { Debug.LogError("Coin Spawner: Coin Spacing Z must be positive!", this); return false; }
        if (enableCleanup && cleanupCheckInterval <= 0) { Debug.LogError("Coin Spawner: Cleanup Check Interval must be positive if cleanup is enabled!", this); return false; }

        return true;
    }

    void Update()
    {
        if (!_activePlayerCarTransform || !_isInitialized) return; // Do nothing if no player or not initialized

        float currentZ = _activePlayerCarTransform.position.z;
        float thresholdZ = _lastSpawnTriggerZ - spawnTriggerDistanceStep;

        // Debug.Log($"CoinSpawner Update: Player Z: {currentZ:F1}, Threshold Z: {thresholdZ:F1}");

        if (currentZ < thresholdZ)
        {
            Debug.Log($"CoinSpawner: Player passed threshold ({currentZ:F1} < {thresholdZ:F1}). Attempting spawn.");
            TrySpawnCoinGroup();
            _lastSpawnTriggerZ = currentZ;
            // Debug.Log($"CoinSpawner: Updated lastSpawnTriggerZ to: {_lastSpawnTriggerZ}");
        }
    }

    void TrySpawnCoinGroup()
    {
        // Redundant checks as Update already checks _activePlayerCarTransform, but good for safety
        if (_activePlayerCarTransform == null || coinPrefabs == null || coinPrefabs.Count == 0 || laneXPositions == null || laneXPositions.Count == 0) return;
        // Debug.Log("CoinSpawner: Entering TrySpawnCoinGroup().");

        int laneIndex = Random.Range(0, laneXPositions.Count);
        float selectedX = laneXPositions[laneIndex];
        // Debug.Log($"CoinSpawner: Selected lane X position: {selectedX}");

        float startZ = _activePlayerCarTransform.position.z - spawnDistanceZ;
        int numberOfCoins = Random.Range(minCoinsPerGroup, maxCoinsPerGroup + 1);

        int coinPrefabIndex = Random.Range(0, coinPrefabs.Count);
        GameObject prefabToSpawn = coinPrefabs[coinPrefabIndex];
        // Debug.Log($"CoinSpawner: Selected coin type for this group: {prefabToSpawn.name}");

        // Debug.Log($"CoinSpawner: Spawning coin group of {numberOfCoins} (Type: {prefabToSpawn.name}) in lane X={selectedX:F1}, starting at Z ~ {startZ:F1}");

        for (int i = 0; i < numberOfCoins; i++)
        {
            float coinZ = startZ - (i * coinSpacingZ);
            Vector3 spawnPosition = new Vector3(selectedX, spawnYPosition, coinZ);

            // Debug.Log($"CoinSpawner: Instantiating coin {i + 1}/{numberOfCoins} (Type: {prefabToSpawn.name}) at {spawnPosition}");
            GameObject newCoin = Instantiate(prefabToSpawn, spawnPosition, prefabToSpawn.transform.rotation);

            if (enableCleanup)
            {
                _spawnedCoins.Add(newCoin);
            }
        }
        // Debug.Log("CoinSpawner: Finished TrySpawnCoinGroup().");
    }

    IEnumerator CleanupFarAwayCoinsCoroutine()
    {
        // Small delay at start to ensure player transform is fully set
        yield return new WaitForSeconds(0.2f);
        Debug.Log("CoinSpawner: Cleanup Coroutine Started.", this);

        while (enableCleanup && this.enabled)
        {
            if (_activePlayerCarTransform == null) // Check if player is still valid
            {
                Debug.LogWarning("CoinSpawner: Player Transform lost in Cleanup, stopping.", this);
                yield break;
            }

            float playerZ = _activePlayerCarTransform.position.z;
            for (int i = _spawnedCoins.Count - 1; i >= 0; i--)
            {
                GameObject coin = _spawnedCoins[i];
                if (coin == null)
                {
                    _spawnedCoins.RemoveAt(i);
                    continue;
                }
                if (coin.transform.position.z > playerZ + cleanupDistanceZ)
                {
                    // Debug.Log($"Cleaning up coin far behind player at Z={coin.transform.position.z:F1} (Player Z: {playerZ:F1})");
                    Destroy(coin);
                    _spawnedCoins.RemoveAt(i);
                }
            }
            yield return new WaitForSeconds(cleanupCheckInterval);
        }
        Debug.Log("CoinSpawner: Cleanup Coroutine Exited.", this);
    }

    // Optional: Method to clear all spawned coins if needed
    public void ClearAllSpawnedCoins()
    {
        foreach (GameObject coin in _spawnedCoins)
        {
            if (coin != null) Destroy(coin);
        }
        _spawnedCoins.Clear();
        Debug.Log("CoinSpawner: All spawned coins cleared.");
    }

    // OnDisable is not strictly needed if OnDestroy handles StopAllCoroutines
    // but it's good practice if the object might be disabled and re-enabled.
    // void OnDisable()
    // {
    //     StopCleanupCoroutineIfActive();
    //     _isInitialized = false;
    // }
}
