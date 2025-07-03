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
    public static int nextSceneIndexToLoad = -1;

    [Header("Map Configuration")]
    [Tooltip("List of all MapData assets. The order of this list DEFINES the next/previous map loading order.")]
    public List<MapData> allMaps;

    [Header("UI References (Must be children of this GameObject)")]
    public Button mainActionButton;
    public TextMeshProUGUI mainActionButtonText;
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
        DontDestroyOnLoad(gameObject);
        Debug.Log("[SceneAndMapShopManager] Instance set and will persist.");
    }

    // --- FIX: Added Start() method back to add the button listener ---
    private void Start()
    {
        if (mainActionButton != null)
        {
            // This line connects the button in the UI to our OnMainActionButtonClicked function.
            mainActionButton.onClick.AddListener(OnMainActionButtonClicked);
        }
        else
        {
            Debug.LogError("[SceneAndMapShopManager] Main Action Button is not assigned in the Inspector!", this);
        }

        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        }
    }
    // --- END FIX ---

    public void OnMapShopOpened()
    {
        Debug.Log("[SceneAndMapShopManager] Map Shop Opened. Initializing UI for current scene.");
        InitializeUIForCurrentScene();
    }

    public void OnMapShopClosed()
    {
        Debug.Log("[SceneAndMapShopManager] Map Shop Closed. Checking if revert is needed.");
        if (GameDataManager.Instance == null || CameraAndMenuManager.Instance == null) return;

        string equippedMapScene = GameDataManager.Instance.SelectedMapName;
        string currentScene = SceneManager.GetActiveScene().name;

        if (equippedMapScene != currentScene)
        {
            Debug.Log($"Reverting from '{currentScene}' to equipped map '{equippedMapScene}'.");
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
            Debug.Log("Already on equipped map. Switching to Main Menu view.");
            CameraAndMenuManager.Instance?.ActivateMainMenuState();
        }
    }

    private void OnEnable()
    {
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[SceneAndMapShopManager] Scene '{scene.name}' loaded. Initializing UI.");
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

    public void ShowNextMap()
    {
        if (allMaps.Count == 0 || GameDataManager.Instance == null) return;
        int currentMapIndex = GameDataManager.Instance.LastViewedMapIndex;
        int nextMapIndex = (currentMapIndex + 1) % allMaps.Count;
        nextSceneIndexToLoad = nextMapIndex;
        LoadSceneByName(allMaps[nextMapIndex].sceneName);
    }

    public void ShowPreviousMap()
    {
        if (allMaps.Count == 0 || GameDataManager.Instance == null) return;
        int currentMapIndex = GameDataManager.Instance.LastViewedMapIndex;
        int previousMapIndex = (currentMapIndex - 1 + allMaps.Count) % allMaps.Count;
        nextSceneIndexToLoad = previousMapIndex;
        LoadSceneByName(allMaps[previousMapIndex].sceneName);
    }

    private void LoadSceneByName(string sceneName)
    {
        Time.timeScale = 1f;
        AudioListener.pause = false;
        SceneManager.LoadScene(sceneName);
    }

    public void UpdateUI()
    {
        if (_currentMapIndex == -1 || allMaps.Count <= _currentMapIndex || GameDataManager.Instance == null) return;
        MapData currentMap = allMaps[_currentMapIndex];
        bool isUnlocked = IsMapUnlocked(currentMap);
        bool isEquipped = (GameDataManager.Instance.SelectedMapName == currentMap.sceneName);
        if (mapNameText != null) mapNameText.text = currentMap.mapName;
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
        // --- FIX: Added debug log to confirm the button click is registered ---
        Debug.Log("[SceneAndMapShopManager] Main Action Button Clicked!");

        if (_currentMapIndex < 0) return;
        MapData currentMap = allMaps[_currentMapIndex];
        if (IsMapUnlocked(currentMap))
        {
            SelectMap();
        }
        else
        {
            BuyMap();
        }
    }

    private void BuyMap()
    {
        Debug.Log("[SceneAndMapShopManager] Entering BuyMap() method...");
        MapData mapToBuy = allMaps[_currentMapIndex];
        if (GameDataManager.Instance.SpendMoney(mapToBuy.price))
        {
            Debug.Log($"[SceneAndMapShopManager] Purchase successful for '{mapToBuy.mapName}'!");
            UnlockMap(mapToBuy);
            UpdateUI();
        }
        else
        {
            Debug.LogWarning($"[SceneAndMapShopManager] Purchase failed for '{mapToBuy.mapName}'. Not enough money.");
        }
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
