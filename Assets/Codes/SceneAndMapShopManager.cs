using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using System.Collections;
using System.Collections.Generic;

// This single, persistent component should be on a root GameObject.
// Your Canvas, panels, and cameras that need to persist should be children of this GameObject.
public class SceneAndMapShopManager : MonoBehaviour
{
    // --- Singleton Instance ---
    public static SceneAndMapShopManager Instance { get; private set; }

    // --- STATIC VARIABLE to pass state between scenes ---
    // This reliably tells the next scene which map index it should be.
    public static int nextSceneIndexToLoad = -1; // -1 means no specific index is requested.

    [Header("Map Configuration")]
    [Tooltip("List of all MapData assets. The order of this list DEFINES the next/previous map loading order.")]
    public List<MapData> allMaps;

    [Header("UI References (Must be children of this GameObject)")]
    [Tooltip("The main button for buying or selecting the map.")]
    public Button mainActionButton;
    [Tooltip("The TextMeshPro text component on the main action button.")]
    public TextMeshProUGUI mainActionButtonText;
    [Tooltip("The TextMeshPro text component to display the map's name.")]
    public TextMeshProUGUI mapNameText;

    // --- Private State ---
    private int _currentMapIndex = -1;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[SceneAndMapShopManager] Another instance found. Destroying new one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject); // This makes this object and all its children persist.
        Debug.Log("[SceneAndMapShopManager] Instance set and will persist.");
    }

    public void OnMapShopOpened()
    {
        Debug.Log("[SceneAndMapShopManager] Map Shop Opened. Initializing UI for current scene.");
        InitializeUIForCurrentScene();
    }


    public void OnMapShopClosed()
    {
        Debug.Log("[SceneAndMapShopManager] Map Shop Closed. Checking if revert is needed.");
        if (GameDataManager.Instance == null || CameraAndMenuManager.Instance == null) return;

        // --- MODIFIED LOGIC: ALWAYS go to main menu view when closing ---
        // First, set the desired state to be the main menu.
        CameraAndMenuManager.Instance.ActivateMainMenuState();
        // Save this intention so it will be applied after any potential scene load.
        CameraAndMenuManager.Instance.SaveCurrentState();

        string equippedMapScene = GameDataManager.Instance.SelectedMapName;
        string currentScene = SceneManager.GetActiveScene().name;

        // Check if we are currently previewing a map that is not our equipped one.
        if (equippedMapScene != currentScene)
        {
            Debug.Log($"Reverting from '{currentScene}' to equipped map '{equippedMapScene}' and will show main menu.");

            // We still need to load the correct equipped scene,
            // but CameraAndMenuManager now knows to show the main menu once it loads.
            int equippedIndex = -1;
            for (int i = 0; i < allMaps.Count; i++)
            {
                if (allMaps[i] != null && allMaps[i].sceneName == equippedMapScene)
                {
                    nextSceneIndexToLoad = i;
                    equippedIndex = i;
                    break;
                }
            }

            if (equippedIndex != -1)
            {
                LoadSceneByName(equippedMapScene);
            }
            else
            {
                Debug.LogError($"[SceneAndMapShopManager] Could not find equipped map '{equippedMapScene}' in the All Maps list to revert to.");
            }
        }
        else
        {
            // If we are already on the equipped map, the ActivateMainMenuState() call from above
            // has already switched the UI panel and camera. No scene change is needed.
            Debug.Log("Already on equipped map. Switched to Main Menu view.");
        }
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

    /// <summary>
    /// Called automatically every time a new scene finishes loading.
    /// This signature now correctly matches the UnityAction<Scene, LoadSceneMode> delegate.
    /// </summary>
    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SceneAndMapShopManager] Scene '{scene.name}' loaded. Initializing UI.");
        // This initialization is now for the map selection UI, which will be inactive
        // if CameraAndMenuManager has just activated the main menu. This is fine.
        InitializeUIForCurrentScene();
    }

    private void InitializeUIForCurrentScene()
    {
        int foundIndex = -1;

        if (nextSceneIndexToLoad != -1)
        {
            foundIndex = nextSceneIndexToLoad;
            nextSceneIndexToLoad = -1;
        }
        else
        {
            if (GameDataManager.Instance != null)
            {
                foundIndex = GameDataManager.Instance.LastViewedMapIndex;
            }
        }

        if (foundIndex != -1)
        {
            _currentMapIndex = foundIndex;
            if (GameDataManager.Instance != null)
            {
                GameDataManager.Instance.LastViewedMapIndex = _currentMapIndex;
            }
            UpdateUI();
        }
    }


    // --- Scene Loading Logic ---

    public void ShowNextMap()
    {
        if (allMaps.Count == 0 || GameDataManager.Instance == null) return;
        int currentMapIndex = GameDataManager.Instance.LastViewedMapIndex;
        int nextMapIndex = (currentMapIndex + 1) % allMaps.Count;
        nextSceneIndexToLoad = nextMapIndex;
        string sceneToLoad = allMaps[nextMapIndex].sceneName;

        // --- Important: Save the MAP SELECTION state before changing scenes ---
        CameraAndMenuManager.Instance?.SaveCurrentState();

        LoadSceneByName(sceneToLoad);
    }

    public void ShowPreviousMap()
    {
        if (allMaps.Count == 0 || GameDataManager.Instance == null) return;
        int currentMapIndex = GameDataManager.Instance.LastViewedMapIndex;
        int previousMapIndex = (currentMapIndex - 1 + allMaps.Count) % allMaps.Count;
        nextSceneIndexToLoad = previousMapIndex;
        string sceneToLoad = allMaps[previousMapIndex].sceneName;

        // --- Important: Save the MAP SELECTION state before changing scenes ---
        CameraAndMenuManager.Instance?.SaveCurrentState();

        LoadSceneByName(sceneToLoad);
    }

    private void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(sceneName);
    }

    // --- UI and Shop Logic ---

    public void UpdateUI()
    {
        if (_currentMapIndex == -1 || allMaps.Count <= _currentMapIndex || GameDataManager.Instance == null) return;

        MapData currentMap = allMaps[_currentMapIndex];

        // --- FIX: Ensure we have valid data before proceeding ---
        if (currentMap == null)
        {
            Debug.LogError($"[SceneAndMapShopManager] MapData at index {_currentMapIndex} is null. Aborting UI Update.");
            return;
        }

        bool isUnlocked = IsMapUnlocked(currentMap);

        // The check for equipped status MUST use the scene name from the MapData object
        bool isEquipped = (GameDataManager.Instance.SelectedMapName == currentMap.sceneName);

        if (mapNameText != null)
        {
            mapNameText.text = currentMap.mapName;
        }

        if (mainActionButton != null && mainActionButtonText != null)
        {
            if (isUnlocked)
            {
                mainActionButtonText.text = isEquipped ? "EQUIPPED" : "SELECT";
                mainActionButton.interactable = !isEquipped;
            }
            else
            {
                mainActionButtonText.text = currentMap.price + "$";
                mainActionButton.interactable = (GameDataManager.Instance.PlayerMoney >= currentMap.price);
            }
        }
    }

    private void OnMainActionButtonClicked()
    {
        if (_currentMapIndex < 0) return;
        MapData currentMap = allMaps[_currentMapIndex];
        if (IsMapUnlocked(currentMap)) { SelectMap(); } else { BuyMap(); }
    }

    private void BuyMap()
    {
        MapData mapToBuy = allMaps[_currentMapIndex];
        if (GameDataManager.Instance.SpendMoney(mapToBuy.price)) { UnlockMap(mapToBuy); UpdateUI(); }
    }

    private void SelectMap()
    {
        MapData mapToSelect = allMaps[_currentMapIndex];
        GameDataManager.Instance.SetSelectedMap(mapToSelect.sceneName);
        UpdateUI();
    }

    private void HandleMoneyChanged(int newMoneyAmount)
    {
        if (_currentMapIndex != -1 && !IsMapUnlocked(allMaps[_currentMapIndex])) { UpdateUI(); }
    }

    private string GetMapSaveKey(MapData map) { return "MapUnlocked_" + map.sceneName; }
    private bool IsMapUnlocked(MapData map) { return map.isUnlockedByDefault || PlayerPrefs.GetInt(GetMapSaveKey(map), 0) == 1; }
    private void UnlockMap(MapData map) { PlayerPrefs.SetInt(GetMapSaveKey(map), 1); PlayerPrefs.Save(); }
}
