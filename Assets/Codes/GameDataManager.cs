using UnityEngine;
using UnityEngine.SceneManagement;
using System; // Required for Action event
using System.Collections.Generic; // Required for List

public class GameDataManager : MonoBehaviour
{
    // --- Singleton Instance ---
    public static GameDataManager Instance { get; private set; }

    [Header("Game Content Configuration")]
    [Tooltip("A list of all CarData assets available in the game.")]
    public CarData[] allCars;
    [Tooltip("A list of all MapData assets available in the game.")]
    public MapData[] allMaps;

    // --- Game Data ---
    [Header("Player Selection Data")]
    public int SelectedPlayerCarIndex { get; private set; } = 0;
    public string SelectedMapName { get; private set; } = "BridgeMap";
    public int LastViewedMapIndex { get; set; } = 0;

    [Header("Player Currency")]
    public int PlayerMoney { get; private set; } = 0;

    [Header("Player Statistics")]
    public int BestScore { get; private set; } = 0;
    public int TotalCoinsCollected { get; private set; } = 0;
    public float TotalDistanceDriven { get; private set; } = 0;

    // --- Events ---
    public event Action<int> OnMoneyChanged;
    public event Action<int> OnSelectedCarChanged;

    // --- PlayerPrefs Keys ---
    private const string PLAYER_MONEY_KEY = "PlayerTotalMoney";
    private const string SELECTED_CAR_INDEX_KEY = "SelectedPlayerCarIndex";
    private const string SELECTED_MAP_NAME_KEY = "SelectedMapName";
    private const string BEST_SCORE_KEY = "PlayerBestScore";
    private const string TOTAL_COINS_KEY = "PlayerTotalCoins";
    private const string TOTAL_DISTANCE_KEY = "PlayerTotalDistance";

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        LoadGameData();
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
        OnMoneyChanged?.Invoke(PlayerMoney);
        OnSelectedCarChanged?.Invoke(SelectedPlayerCarIndex);
    }

    public void SetSelectedPlayerCar(int carIndex)
    {
        SelectedPlayerCarIndex = carIndex;
        PlayerPrefs.SetInt(SELECTED_CAR_INDEX_KEY, SelectedPlayerCarIndex);
        PlayerPrefs.Save();
        OnSelectedCarChanged?.Invoke(SelectedPlayerCarIndex);
    }

    public void SetSelectedMap(string mapSceneName)
    {
        SelectedMapName = mapSceneName;
        PlayerPrefs.SetString(SELECTED_MAP_NAME_KEY, SelectedMapName);
        PlayerPrefs.Save();
    }

    public void AddMoney(int amount)
    {
        if (amount <= 0) return;
        PlayerMoney += amount;
        TotalCoinsCollected += amount;
        SaveMoney();
        OnMoneyChanged?.Invoke(PlayerMoney);
    }

    public bool SpendMoney(int amount)
    {
        if (amount < 0 || PlayerMoney < amount) return false;
        PlayerMoney -= amount;
        SaveMoney();
        OnMoneyChanged?.Invoke(PlayerMoney);
        return true;
    }

    public void UpdateBestScore(int newScore)
    {
        if (newScore > BestScore)
        {
            BestScore = newScore;
            PlayerPrefs.SetInt(BEST_SCORE_KEY, BestScore);
            PlayerPrefs.Save();
        }
    }

    public void AddToTotalDistance(float meters)
    {
        if (meters <= 0) return;
        TotalDistanceDriven += (meters / 1000f);
        PlayerPrefs.SetFloat(TOTAL_DISTANCE_KEY, TotalDistanceDriven);
        PlayerPrefs.Save();
    }

    // --- MODIFIED: Helper methods now use the internal lists ---
    public int GetUnlockedCarCount()
    {
        int count = 0;
        foreach (var carData in allCars)
        {
            if (carData.isUnlockedByDefault || PlayerPrefs.GetInt("CarUnlocked_" + carData.carName.Replace(" ", ""), 0) == 1)
            {
                count++;
            }
        }
        return count;
    }

    public int GetUnlockedMapCount()
    {
        int count = 0;
        foreach (var mapData in allMaps)
        {
            if (mapData.isUnlockedByDefault || PlayerPrefs.GetInt("MapUnlocked_" + mapData.sceneName, 0) == 1)
            {
                count++;
            }
        }
        return count;
    }
    // --- END MODIFICATION ---

    private void SaveMoney()
    {
        PlayerPrefs.SetInt(PLAYER_MONEY_KEY, PlayerMoney);
        PlayerPrefs.SetInt(TOTAL_COINS_KEY, TotalCoinsCollected);
        PlayerPrefs.Save();
    }

    private void SaveSelectedCarIndex()
    {
        PlayerPrefs.SetInt(SELECTED_CAR_INDEX_KEY, SelectedPlayerCarIndex);
        PlayerPrefs.Save();
    }

    public void LoadGameData()
    {
        PlayerMoney = PlayerPrefs.GetInt(PLAYER_MONEY_KEY, 0);
        SelectedPlayerCarIndex = PlayerPrefs.GetInt(SELECTED_CAR_INDEX_KEY, 0);
        SelectedMapName = PlayerPrefs.GetString(SELECTED_MAP_NAME_KEY, "BridgeMap");

        BestScore = PlayerPrefs.GetInt(BEST_SCORE_KEY, 0);
        TotalCoinsCollected = PlayerPrefs.GetInt(TOTAL_COINS_KEY, 0);
        TotalDistanceDriven = PlayerPrefs.GetFloat(TOTAL_DISTANCE_KEY, 0f);

        Debug.Log($"GameDataManager: Data Loaded. Money: {PlayerMoney}, Best Score: {BestScore}");
    }

    public void ResetAllSavedData()
    {
        PlayerPrefs.DeleteAll();
        Debug.Log("GameDataManager: ALL PlayerPrefs have been deleted.");

        PlayerMoney = 0;
        SelectedPlayerCarIndex = 0;
        SelectedMapName = "BridgeMap";
        LastViewedMapIndex = 0;
        BestScore = 0;
        TotalCoinsCollected = 0;
        TotalDistanceDriven = 0f;

        OnMoneyChanged?.Invoke(PlayerMoney);
        OnSelectedCarChanged?.Invoke(SelectedPlayerCarIndex);
    }

    public int GetUpgradeLevel(string carName, string upgradeType)
    {
        string key = $"UpgradeLevel_{carName}_{upgradeType}";
        return PlayerPrefs.GetInt(key, 0);
    }

    public void SetUpgradeLevel(string carName, string upgradeType, int newLevel)
    {
        string key = $"UpgradeLevel_{carName}_{upgradeType}";
        PlayerPrefs.SetInt(key, newLevel);
        PlayerPrefs.Save();
    }
}
