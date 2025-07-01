using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using static UnityEngine.Rendering.DebugUI.Table;
using UnityEngine.InputSystem;

public class RoadSpawner : MonoBehaviour
{
    [Header("Road Setup")]
    public GameObject[] roadPrefabs;
    public float roadSegmentLength = 30f;
    public int segmentsAhead = 5;    // How many segments to keep spawned ahead of the player
    public int segmentsBehind = 2;   // How many segments to keep spawned behind the player
    public float height = 0f;        // Y position for road segments
    public float side = 0f;          // X position for road segments

    // --- Private Variables ---
    private Transform _activePlayerCarTransform; // Will be set by PlayerCarManager
    private List<GameObject> _activeSegments = new List<GameObject>();
    private float _frontSpawnZ; // Z-coordinate for the next segment to spawn ahead (more negative)
    private float _backSpawnZ;  // Z-coordinate for the next segment to spawn behind (less negative)


    void Awake()
    {
        // This log will run before the Start() method
        Debug.Log("RoadSpawner AWAKE called. At this point, PlayerCarManager.Instance is: " + (PlayerCarManager.Instance == null ? "NULL" : "SET"));
    }

    void Start()
    {
        // Subscribe to PlayerCarManager events and get initial car
        if (PlayerCarManager.Instance != null)
        {
            PlayerCarManager.Instance.OnPlayerCarChanged += HandlePlayerCarChanged;
            // Get the initially selected car
            if (PlayerCarManager.Instance.CurrentPlayerCarGameObject != null)
            {
                HandlePlayerCarChanged(PlayerCarManager.Instance.CurrentPlayerCarGameObject);
            }
            else
            {
                Debug.LogError("RoadSpawner: PlayerCarManager has no active car on Start!", this);
                enabled = false;
                return;
            }
        }
        else
        {
            // If this error still appears, it confirms PlayerCarManager.Awake() never ran.
            Debug.LogError("RoadSpawner START: PlayerCarManager.Instance is null! Make sure PlayerCarManager is in the scene and initialized before RoadSpawner.");
            enabled = false;
            return;
        }

        // Initial setup continues in HandlePlayerCarChanged if _activePlayerCarTransform is set
    }

    void OnDestroy() // Or OnDisable
    {
        // Unsubscribe from events to prevent errors
        if (PlayerCarManager.Instance != null)
        {
            PlayerCarManager.Instance.OnPlayerCarChanged -= HandlePlayerCarChanged;
        }
    }

    /// <summary>
    /// Called when the PlayerCarManager signals a change in the active player car.
    /// Also used for initial setup.
    /// </summary>
    void HandlePlayerCarChanged(GameObject newPlayerCar)
    {
        if (newPlayerCar != null)
        {
            _activePlayerCarTransform = newPlayerCar.transform;
            Debug.Log($"RoadSpawner: Player car set to '{newPlayerCar.name}'. Initializing road.", this);
            InitializeRoadSpawning(); // Initialize or re-initialize road spawning
        }
        else
        {
            Debug.LogError("RoadSpawner: Received null player car from PlayerCarManager. Road spawning halted.", this);
            _activePlayerCarTransform = null;
            // Optionally clear existing roads or handle this state as needed
            // ClearAllSegments();
            enabled = false; // Disable spawner if player is lost
        }
    }

    void InitializeRoadSpawning()
    {
        if (!_activePlayerCarTransform)
        {
            Debug.LogError("RoadSpawner: Cannot initialize road, activePlayerCarTransform is null!", this);
            enabled = false;
            return;
        }
        if (roadPrefabs == null || roadPrefabs.Length == 0)
        {
            Debug.LogError("RoadSpawner: roadPrefabs array is not set or empty!", this);
            enabled = false;
            return;
        }

        // Clear any existing segments if re-initializing
        ClearAllSegments();

        // Initialize spawn Z points based on the current player's Z
        // _frontSpawnZ will be where the *next* segment ahead is placed
        // _backSpawnZ will be where the *next* segment behind is placed

        // Spawn the segment the player is currently on (or closest to)
        GameObject centralSegment = Instantiate(roadPrefabs[Random.Range(0, roadPrefabs.Length)]);
        centralSegment.transform.position = new Vector3(side, height, _activePlayerCarTransform.position.z);
        _activeSegments.Add(centralSegment);
        // Debug.Log($"RoadSpawner: Spawned central segment at Z: {_activePlayerCarTransform.position.z}");

        // Set up the Z positions for the *next* segments to be spawned
        _frontSpawnZ = _activePlayerCarTransform.position.z - roadSegmentLength; // Ahead is more negative
        _backSpawnZ = _activePlayerCarTransform.position.z + roadSegmentLength; // Behind is less negative

        // Spawn initial segments ahead (excluding the central one already spawned)
        for (int i = 0; i < segmentsAhead - 1; i++) // -1 because one "ahead" position is covered by central or next _frontSpawnZ
        {
            SpawnSegment(true);
        }

        // Spawn initial segments behind
        for (int i = 0; i < segmentsBehind; i++)
        {
            SpawnSegment(false);
        }
        Debug.Log("RoadSpawner: Initial road segments created.");
        enabled = true; // Ensure spawner is enabled after successful initialization
    }


    void Update()
    {
        if (!_activePlayerCarTransform) return; // Do nothing if there's no active player

        // Player moves in negative Z direction (forward)
        // If player's Z is more negative than (_frontSpawnZ plus a buffer of segmentsAhead)
        // This means player is approaching the point where new "ahead" segments are needed.
        // _frontSpawnZ is the Z of the *next* segment to be spawned ahead.
        // The "edge" of currently spawned ahead segments is roughly _frontSpawnZ + (roadSegmentLength * segmentsAhead)
        // We want to spawn if player.z gets close to _frontSpawnZ.
        if (_activePlayerCarTransform.position.z < _frontSpawnZ + (roadSegmentLength * (segmentsAhead - 1))) // Adjusted condition
        {
            SpawnSegment(true);
        }

        // Player moves in positive Z direction (backward)
        // If player's Z is less negative than (_backSpawnZ minus a buffer of segmentsBehind)
        // This means player is approaching the point where new "behind" segments are needed.
        // _backSpawnZ is the Z of the *next* segment to be spawned behind.
        if (_activePlayerCarTransform.position.z > _backSpawnZ - (roadSegmentLength * (segmentsBehind - 1))) // Adjusted condition
        {
            SpawnSegment(false);
        }

        CleanupSegments();
    }

    void SpawnSegment(bool spawnAhead)
    {
        if (roadPrefabs.Length == 0) return;

        GameObject newSegmentPrefab = roadPrefabs[Random.Range(0, roadPrefabs.Length)];
        GameObject newSegment = Instantiate(newSegmentPrefab);
        float spawnZ;

        if (spawnAhead) // Spawning ahead (player moving towards negative Z)
        {
            spawnZ = _frontSpawnZ;
            newSegment.transform.position = new Vector3(side, height, spawnZ);
            _frontSpawnZ -= roadSegmentLength; // Move the "next ahead" spawn point further negative
        }
        else // Spawning behind (player moving towards positive Z, or initial setup)
        {
            spawnZ = _backSpawnZ;
            newSegment.transform.position = new Vector3(side, height, spawnZ);
            _backSpawnZ += roadSegmentLength; // Move the "next behind" spawn point further positive
        }
        _activeSegments.Add(newSegment);
        // Debug.Log($"Spawned segment at Z: {spawnZ}. Next FrontZ: {_frontSpawnZ}, Next BackZ: {_backSpawnZ}");
    }

    void CleanupSegments()
    {
        if (!_activePlayerCarTransform) return;

        // For negative Z movement:
        // "Too far ahead" means Z < _frontSpawnZ (more negative than the next segment to be spawned ahead)
        // "Too far behind" means Z > _backSpawnZ (more positive than the next segment to be spawned behind)

        // More robust cleanup: based on distance from player
        float playerZ = _activePlayerCarTransform.position.z;
        // Calculate cleanup thresholds based on player's current position
        // Segments more negative than this are too far ahead
        float cleanupThresholdFarAhead = playerZ - ((segmentsAhead + 2) * roadSegmentLength);
        // Segments more positive than this are too far behind
        float cleanupThresholdFarBehind = playerZ + ((segmentsBehind + 2) * roadSegmentLength);


        for (int i = _activeSegments.Count - 1; i >= 0; i--)
        {
            if (_activeSegments[i] == null)
            {
                _activeSegments.RemoveAt(i);
                continue;
            }

            float segmentZ = _activeSegments[i].transform.position.z;

            if (segmentZ < cleanupThresholdFarAhead || segmentZ > cleanupThresholdFarBehind)
            {
                // Debug.Log($"Cleaning up segment at Z: {segmentZ}");
                Destroy(_activeSegments[i]);
                _activeSegments.RemoveAt(i);
            }
        }
    }

    void ClearAllSegments()
    {
        foreach (GameObject segment in _activeSegments)
        {
            if (segment != null) Destroy(segment);
        }
        _activeSegments.Clear();
        Debug.Log("RoadSpawner: All active road segments cleared.");
    }
}
