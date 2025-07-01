using UnityEngine;
using UnityEngine.SceneManagement;
using System.Collections.Generic;

public class SimpleSceneLoader : MonoBehaviour
{
    [Header("Scene Setup")]
    [Tooltip("Add the names of your map scenes here. This order MUST match the order of MapData assets in the MapShopManager.")]
    public List<string> mapSceneNames = new List<string>();

    void Start()
    {
        if (mapSceneNames == null || mapSceneNames.Count == 0)
        {
            Debug.LogError("SimpleSceneLoader: 'Map Scene Names' list is empty!", this);
            enabled = false;
            return;
        }
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("SimpleSceneLoader: GameDataManager not found! This script requires it to function.", this);
            enabled = false;
        }
    }

    public void LoadNextMap()
    {
        if (mapSceneNames.Count == 0) return;

        int currentMapIndex = GameDataManager.Instance.LastViewedMapIndex;
        int nextMapIndex = (currentMapIndex + 1) % mapSceneNames.Count;
        GameDataManager.Instance.LastViewedMapIndex = nextMapIndex;

        string sceneToLoad = mapSceneNames[nextMapIndex];
        Debug.Log($"[SimpleSceneLoader] Current Index: {currentMapIndex}. Loading NEXT map '{sceneToLoad}' at index {nextMapIndex}.");
        LoadSceneByName(sceneToLoad);
    }

    public void LoadPreviousMap()
    {
        if (mapSceneNames.Count == 0) return;

        int currentMapIndex = GameDataManager.Instance.LastViewedMapIndex;
        int previousMapIndex = (currentMapIndex - 1 + mapSceneNames.Count) % mapSceneNames.Count;
        GameDataManager.Instance.LastViewedMapIndex = previousMapIndex;

        string sceneToLoad = mapSceneNames[previousMapIndex];
        Debug.Log($"[SimpleSceneLoader] Current Index: {currentMapIndex}. Loading PREVIOUS map '{sceneToLoad}' at index {previousMapIndex}.");
        LoadSceneByName(sceneToLoad);
    }

    /// <summary>
    /// Helper function to load a scene by its name.
    /// </summary>
    private void LoadSceneByName(string sceneName)
    {
        // --- ADDED THIS PART ---
        // Save the state of the UI *before* we leave this scene
        if (CameraAndMenuManager.Instance != null)
        {
            CameraAndMenuManager.Instance.SaveCurrentState();
        }
        // --- END ADDITION ---

        Time.timeScale = 1f;
        AudioListener.pause = false;

        SceneManager.LoadScene(sceneName);
    }

    public void LoadSelectedMapToPlay()
    {
        if (GameDataManager.Instance != null)
        {
            string selectedScene = GameDataManager.Instance.SelectedMapName;
            if (!string.IsNullOrEmpty(selectedScene))
            {
                Debug.Log($"[SimpleSceneLoader] Loading selected map to PLAY: '{selectedScene}'.");
                LoadSceneByName(selectedScene);
            }
            else
            {
                Debug.LogError("[SimpleSceneLoader] Cannot load selected map, SelectedMapName in GameDataManager is empty!");
            }
        }
    }
}