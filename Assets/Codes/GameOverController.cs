using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic; // Not strictly needed here but good practice if you expand

public class GameOverController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject GameOverScreen;
    public GameObject controlsUI;

    [Header("Scene References")]
    // *** REMOVED: public Transform playerTransform; ***
    public Vector3 respawnLocation = Vector3.zero; // Default respawn location
    public Quaternion respawnRotation = Quaternion.identity; // Default respawn rotation
    public AudioSource soundeffects; // For restart sound
    public GameObject driversCamera;
    public GameObject MeniuCamera; // Typo? Consider mainMenuCamera
    public AICarSpawner spawner; // For cleaning AI cars
    public GameObject effectsPrefab; // For crash effects

    // --- Private Variables ---
    private Transform _activePlayerCarTransform;
    private Rigidbody _activePlayerCarRigidbody; // Optional: if you need to reset velocity

    void Start()
    {
        // Ensure GameOverScreen is initially inactive
        if (GameOverScreen != null)
        {
            GameOverScreen.SetActive(false);
        }

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
                Debug.LogWarning("GameOverController: PlayerCarManager has no active car on Start. Waiting for OnPlayerCarChanged event.", this);
            }
        }
        else
        {
            Debug.LogError("GameOverController: PlayerCarManager.Instance is null! Make sure PlayerCarManager is in the scene and initialized before GameOverController.", this);
            // If this script is critical, you might disable it or parts of its functionality
            // enabled = false;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events to prevent errors
        if (PlayerCarManager.Instance != null)
        {
            PlayerCarManager.Instance.OnPlayerCarChanged -= HandleActivePlayerCarChanged;
        }
    }

    void HandleActivePlayerCarChanged(GameObject newPlayerCar)
    {
        if (newPlayerCar != null)
        {
            _activePlayerCarTransform = newPlayerCar.transform;
            _activePlayerCarRigidbody = newPlayerCar.GetComponent<Rigidbody>(); // Get Rigidbody if present
            Debug.Log($"GameOverController: Player car reference updated to '{newPlayerCar.name}'.", this);
        }
        else
        {
            Debug.LogWarning("GameOverController: Active player car reference became null.", this);
            _activePlayerCarTransform = null;
            _activePlayerCarRigidbody = null;
        }
    }

    public void RestartLevel()
    {
        Debug.Log("RestartLevel called.");

        if (_activePlayerCarTransform == null)
        {
            Debug.LogError("GameOverController: Cannot restart level, active player car transform is not set!", this);
            // Attempt to re-fetch from PlayerCarManager as a fallback
            if (PlayerCarManager.Instance != null && PlayerCarManager.Instance.CurrentPlayerCarGameObject != null)
            {
                HandleActivePlayerCarChanged(PlayerCarManager.Instance.CurrentPlayerCarGameObject);
                if (_activePlayerCarTransform == null) return; // Still null, exit
            }
            else
            {
                Debug.LogError("GameOverController: PlayerCarManager or its current car is null. Cannot proceed with restart.");
                return;
            }
        }

        // Reset player position and rotation
        _activePlayerCarTransform.position = respawnLocation;
        _activePlayerCarTransform.rotation = respawnRotation; // Use defined respawnRotation

        // Optional: Reset player's velocity and angular velocity if they have a Rigidbody
        if (_activePlayerCarRigidbody != null)
        {
            _activePlayerCarRigidbody.linearVelocity = Vector3.zero;
            _activePlayerCarRigidbody.angularVelocity = Vector3.zero;
        }

        // Deactivate crash effects
        if (effectsPrefab != null)
        {
            effectsPrefab.SetActive(false);
        }
        else
        {
            Debug.LogWarning("GameOverController: Effects Prefab not assigned.", this);
        }

        // Clean AI cars
        if (spawner != null)
        {
            spawner.CleanAiCars();
            // Consider re-initializing the spawner if needed, e.g., spawner.InitializeSpawning();
        }
        else
        {
            Debug.LogWarning("GameOverController: AICarSpawner (spawner) not assigned.", this);
        }

        

        // Play sound effect
        if (soundeffects != null)
        {
            soundeffects.Play();
        }
        else
        {
            Debug.LogWarning("GameOverController: Sound Effects AudioSource not assigned.", this);
        }

        // Get the active car controller instance
        CarController activeCar = PlayerCarManager.Instance.CurrentPlayerCarController;
        if (activeCar == null) return;

        
       
        float distanceInMeters = activeCar.GetDistanceThisRun();

        if (GameDataManager.Instance != null)
        {      
            GameDataManager.Instance.AddToTotalDistance(distanceInMeters);
        }



        GameDataManager.Instance.UpdateBestScore(ScoreManager._score);

        // Reset score
        if (ScoreManager.Instance != null)
        {
            ScoreManager.Instance.ResetScore();
        }
        else
        {
            Debug.LogWarning("GameOverController: ScoreManager.Instance is null. Cannot reset score.", this);
        }

        // Resume game time if it was paused
        Time.timeScale = 1f;
        if (PauseManager.IsGamePaused) // Assuming you have a PauseManager
        {
            PauseManager.Instance?.ResumeGame(); // Call resume if it exists
        }

        Debug.Log("Level Restarted.");
    }

    public void BackToMeniu() // Typo? Consider BackToMenu
    {
        Debug.Log("BackToMeniu called. Reloading current scene.");
        // Ensure game time is normal before scene transitions
        Time.timeScale = 1f;
        if (PauseManager.IsGamePaused)
        {
            PauseManager.Instance?.ResumeGame(); // Ensure game is unpaused
        }
        AudioListener.pause = false; // Ensure audio is unpaused

        Scene scene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(scene.name); // Reloads the current active scene
        // If your main menu is a different scene, use:
        // SceneManager.LoadScene("YourMainMenuSceneName");
    }
}
