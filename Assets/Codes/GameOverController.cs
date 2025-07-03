using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class GameOverController : MonoBehaviour
{
    [Header("UI Elements")]
    public GameObject GameOverScreen;
    public GameObject controlsUI;

    [Header("Scene References")]
    public Vector3 respawnLocation = Vector3.zero;
    public Quaternion respawnRotation = Quaternion.identity;
    public AudioSource soundeffects;
    public GameObject driversCamera;
    public GameObject mainMenuCamera; // Renamed from MeniuCamera
    public GameObject effectsPrefab;

    // --- Private Variables ---
    private Transform _activePlayerCarTransform;
    private Rigidbody _activePlayerCarRigidbody;
    private AICarSpawner _spawner; // Reference is now private and found automatically

    void Start()
    {
        // Ensure GameOverScreen is initially inactive
        if (GameOverScreen != null)
        {
            GameOverScreen.SetActive(false);
        }

        // --- MODIFIED: Find the AICarSpawner at runtime ---
        // ** FIX: Explicitly specified UnityEngine.Object to resolve ambiguity **
        _spawner = UnityEngine.Object.FindFirstObjectByType<AICarSpawner>();
        if (_spawner == null)
        {
            Debug.LogWarning("GameOverController: AICarSpawner not found in the scene. The CleanAiCars function will not be called on restart.", this);
        }
        // --- END MODIFICATION ---

        // Subscribe to PlayerCarManager events and get initial car
        if (PlayerCarManager.Instance != null)
        {
            PlayerCarManager.Instance.OnPlayerCarChanged += HandleActivePlayerCarChanged;
            if (PlayerCarManager.Instance.CurrentPlayerCarGameObject != null)
            {
                HandleActivePlayerCarChanged(PlayerCarManager.Instance.CurrentPlayerCarGameObject);
            }
        }
        else
        {
            Debug.LogError("GameOverController: PlayerCarManager.Instance is null! This script may not function correctly.", this);
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
            _activePlayerCarRigidbody = newPlayerCar.GetComponent<Rigidbody>();
        }
        else
        {
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
            return;
        }

        // Reset player position and rotation
        _activePlayerCarTransform.position = respawnLocation;
        _activePlayerCarTransform.rotation = respawnRotation;

        // Reset player's velocity
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

        // --- MODIFIED: Use the internal _spawner reference with a null check ---
        // The ?. is a null-conditional operator. It will only call CleanAiCars() if _spawner is not null.
        _spawner?.CleanAiCars();
        // --- END MODIFICATION ---

        // Manage cameras
        if (driversCamera != null) driversCamera.SetActive(false);
        if (mainMenuCamera != null) mainMenuCamera.SetActive(false);

        // Manage UI
        if (controlsUI != null) controlsUI.SetActive(false);
        if (GameOverScreen != null) GameOverScreen.SetActive(false);

        // Play sound effect
        soundeffects?.Play();

        // Get stats and update GameDataManager
        CarController activeCar = PlayerCarManager.Instance.CurrentPlayerCarController;
        if (activeCar != null && GameDataManager.Instance != null)
        {
            float distanceInMeters = activeCar.GetDistanceThisRun();
            GameDataManager.Instance.AddToTotalDistance(distanceInMeters);
            GameDataManager.Instance.UpdateBestScore(ScoreManager._score);
        }

        // Reset score for the new run
        ScoreManager.Instance?.ResetScore();

        // Resume game time if it was paused
        Time.timeScale = 1f;
        PauseManager.Instance?.ResumeGame();

        Debug.Log("Level Restarted.");
    }

    public void BackToMeniu()
    {
        Debug.Log("BackToMeniu called. Reloading current scene.");
        Time.timeScale = 1f;
        PauseManager.Instance?.ResumeGame();
        AudioListener.pause = false;
        SceneManager.LoadScene(SceneManager.GetActiveScene().name);
    }
}
