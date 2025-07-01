using UnityEngine;
using UnityEngine.SceneManagement; // Optional: if you want a "Quit to Main Menu" button

public class PauseManager : MonoBehaviour
{
    [Header("Pause Menu UI")]
    [Tooltip("Assign the UI Panel that serves as your pause menu.")]
    public GameObject pauseMenuUI; // Assign your Pause Menu Panel here in the Inspector

    // --- State ---
    public static bool IsGamePaused { get; private set; } = false;

    // --- Singleton Instance (Optional but often useful) ---
    public static PauseManager Instance { get; private set; }

    private void Awake()
    {
        // Singleton setup
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
        }
        else
        {
            Instance = this;
            // DontDestroyOnLoad(this.gameObject); // Uncomment if you want this manager to persist across scenes
        }
    }

    void Start()
    {
        // Ensure the pause menu is hidden at the start of the game
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        IsGamePaused = false; // Ensure state is correct at start
        Time.timeScale = 1f; // Ensure game is running at start
    }

    void Update()
    {
        
    }

    public void TogglePause()
    {
        if (IsGamePaused)
        {
            ResumeGame();
        }
        else
        {
            PauseGame();
        }
    }

    public void PauseGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true);
        }
        Time.timeScale = 0f; // This stops most time-dependent game activities
        IsGamePaused = true;
        Debug.Log("Game Paused. Time.timeScale = 0");

        // Optional: Unlock and show cursor if it was locked (common in FPS games)
        // Cursor.lockState = CursorLockMode.None;
        // Cursor.visible = true;

        // Note: AudioSources might need to be paused individually if they
        // are not affected by Time.timeScale (e.g., if their AudioSource.ignoreListenerPause is true
        // or if they are playing UI sounds you want to continue).
        // For a simple global pause, you can set AudioListener.pause = true;
        AudioListener.pause = true;
    }

    public void ResumeGame()
    {
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false);
        }
        Time.timeScale = 1f; // Resume normal game speed
        IsGamePaused = false;
        Debug.Log("Game Resumed. Time.timeScale = 1");

        // Optional: Re-lock cursor if needed
        // Cursor.lockState = CursorLockMode.Locked;
        // Cursor.visible = false;

        AudioListener.pause = false;
    }

    // --- Example Pause Menu Button Functions ---

    public void LoadMainMenu()
    {
        Debug.Log("Loading Main Menu...");
        Time.timeScale = 1f; // IMPORTANT: Always reset timeScale before loading a new scene
        IsGamePaused = false; // Reset pause state
        AudioListener.pause = false; // Unpause audio
        // SceneManager.LoadScene("MainMenuScene"); // Replace "MainMenuScene" with your actual scene name
    }

    public void QuitGame()
    {
        Debug.Log("Quitting Game...");
        Application.Quit(); // This works in a built game, not always in the editor

#if UNITY_EDITOR
        // If running in the Unity Editor, stop playing
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
