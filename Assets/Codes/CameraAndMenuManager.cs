using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;
using System.Collections;

public class CameraAndMenuManager : MonoBehaviour
{
    // --- Singleton Instance ---
    public static CameraAndMenuManager Instance { get; private set; }

    [Header("Managed Objects (Assign in Inspector)")]
    [Tooltip("Add all Camera GameObjects that this manager should control.")]
    public List<GameObject> availableCameras = new List<GameObject>();

    [Tooltip("Add all Menu Panel GameObjects that this manager should control.")]
    public List<GameObject> availableMenuPanels = new List<GameObject>();

    [Header("Initial State (Fallback)")]
    [Tooltip("Name of the camera GameObject to activate if no saved state is found.")]
    public string initialActiveCameraName;
    [Tooltip("Name of the menu panel GameObject to activate if no saved state is found.")]
    public string initialActiveMenuPanelName;


    // --- Private State for Saving ---
    private string _activePanelName;
    private string _activeCameraName;

    // --- PlayerPrefs Keys ---
    private const string ACTIVE_PANEL_KEY = "LastActiveMenuPanel";
    private const string ACTIVE_CAMERA_KEY = "LastActiveMenuCamera";

    private void Awake()
    {
        // Singleton Pattern to ensure only one instance exists
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[CameraAndMenuManager] Another instance found. Destroying new one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;

        // This makes this object and all its direct children persist across scene loads
        DontDestroyOnLoad(gameObject);
        Debug.Log("[CameraAndMenuManager] Instance set and will persist along with its children.");

        // Load the saved state when the game first launches
        LoadLastState();
    }

    private void OnEnable()
    {
        // Subscribe the OnSceneLoaded method to the sceneLoaded event
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        // Unsubscribe to prevent errors when this object is destroyed
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    
    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[CameraAndMenuManager] Scene '{scene.name}' loaded. Applying last saved state.");
        LoadLastState();
        ApplyState();
    }


    private void ApplyState()
    {
        ActivateObjectByName(_activePanelName, availableMenuPanels, false, true); // Suppress save, we just loaded
        ActivateObjectByName(_activeCameraName, availableCameras, true, true); // Suppress save, we just loaded
    }

  
    public void SaveCurrentState()
    {
        PlayerPrefs.SetString(ACTIVE_PANEL_KEY, _activePanelName);
        PlayerPrefs.SetString(ACTIVE_CAMERA_KEY, _activeCameraName);
        PlayerPrefs.Save();
        Debug.Log($"[CameraAndMenuManager] State Saved -> Panel: '{_activePanelName}', Camera: '{_activeCameraName}'");
    }


    private void LoadLastState()
    {
        _activePanelName = PlayerPrefs.GetString(ACTIVE_PANEL_KEY, initialActiveMenuPanelName);
        _activeCameraName = PlayerPrefs.GetString(ACTIVE_CAMERA_KEY, initialActiveCameraName);
        Debug.Log($"[CameraAndMenuManager] State Loaded -> Desired Panel: '{_activePanelName}', Desired Camera: '{_activeCameraName}'");
    }


    public void ActivatePanel(GameObject panelToActivate)
    {
        if (panelToActivate == null)
        {
            Debug.LogError("[CameraAndMenuManager] ActivatePanel was called with a null panel reference.", this);
            return;
        }
        // Call the internal activation method by name
        ActivateObjectByName(panelToActivate.name, availableMenuPanels, false);
    }


    public void ActivateCamera(GameObject cameraToActivate)
    {
        if (cameraToActivate == null)
        {
            Debug.LogError("[CameraAndMenuManager] ActivateCamera was called with a null camera reference.", this);
            return;
        }
        // Call the internal activation method by name
        ActivateObjectByName(cameraToActivate.name, availableCameras, true);
    }


    public void ActivateMainMenuState()
    {
        Debug.Log($"[CameraAndMenuManager] Activating Main Menu State. Panel: '{initialActiveMenuPanelName}', Camera: '{initialActiveCameraName}'.");
        if (string.IsNullOrEmpty(initialActiveMenuPanelName) || string.IsNullOrEmpty(initialActiveCameraName))
        {
            Debug.LogError("[CameraAndMenuManager] Cannot activate main menu state: 'Initial Active Camera Name' or 'Initial Active Menu Panel Name' is not set in the Inspector!", this);
            return;
        }
        ActivateObjectByName(initialActiveMenuPanelName, availableMenuPanels, false); // suppressSave is false, so it saves
        ActivateObjectByName(initialActiveCameraName, availableCameras, true);     // suppressSave is false, so it saves
    }
   

    // Internal method that handles the actual activation and state updating/saving
    private void ActivateObjectByName(string nameToActivate, List<GameObject> list, bool isCamera, bool suppressSave = false)
    {
        if (string.IsNullOrEmpty(nameToActivate)) return;

        // Update the internal state variable
        if (isCamera)
        {
            _activeCameraName = nameToActivate;
        }
        else
        {
            _activePanelName = nameToActivate;
        }

        Debug.Log($"[CameraAndMenuManager] Activating: {nameToActivate}");

        foreach (GameObject item in list)
        {
            if (item != null)
            {
                item.SetActive(item.name == nameToActivate);
            }
        }

        // Save the new state if this action was not suppressed
        if (!suppressSave)
        {
            SaveCurrentState();
        }
    }


    public void Wait(float seconds)
    {
        StartCoroutine(WaitCoroutine(seconds));
    }

    
    private IEnumerator WaitCoroutine(float seconds)
    {
        Debug.Log($"[CameraAndMenuManager] Starting a wait for {seconds} seconds.");
        // Use WaitForSecondsRealtime to ensure the delay works even if the game is paused (Time.timeScale = 0)
        yield return new WaitForSecondsRealtime(seconds);
        Debug.Log($"[CameraAndMenuManager] Wait finished after {seconds} seconds.");
        // This is where you would trigger another event or call another function if needed.
    }


}
