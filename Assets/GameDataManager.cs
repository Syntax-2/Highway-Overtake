using UnityEngine;
using UnityEngine.SceneManagement;
using System; // Required for Action event

public class GameDataManager : MonoBehaviour
{
    // --- Singleton Instance ---
    public static GameDataManager Instance { get; private set; }

    // --- Game Data ---
    [Header("Player Selection Data")]
    public int SelectedPlayerCarIndex { get; private set; } = 0;
    public string CurrentMapName { get; private set; } = "";

    // This will store the index of the map being previewed in the map selection menu.
    public int LastViewedMapIndex { get; set; } = 0;

    [HideInInspector] // We don't need to see this in the Inspector
    public string TargetSceneToLoad { get; set; }

    [Tooltip("Name of the map scene selected by the player to play.")]
    public string SelectedMapName { get; private set; } = "BridgeMap"; // Set a default map scene name

    [Header("Player Currency")]
    public int PlayerMoney { get; private set; } = 0;

    // --- Events ---
    public event Action<int> OnMoneyChanged;
    public event Action<int> OnSelectedCarChanged;
    public event Action<string> OnMapChanged;

    // --- PlayerPrefs Keys ---
    private const string PLAYER_MONEY_KEY = "PlayerTotalMoney";
    private const string SELECTED_CAR_INDEX_KEY = "SelectedPlayerCarIndex";
    private const string SELECTED_MAP_NAME_KEY = "SelectedMapName";


    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        Debug.Log("GameDataManager Initialized and Persisting.");

        LoadGameData();
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void Start()
    {
        if (string.IsNullOrEmpty(CurrentMapName))
        {
            UpdateCurrentMapName(SceneManager.GetActiveScene().name);
        }
    }

    public void SetSelectedPlayerCar(int carIndex)
    {
        SelectedPlayerCarIndex = carIndex;
        OnSelectedCarChanged?.Invoke(SelectedPlayerCarIndex);
        SaveSelectedCarIndex();
        Debug.Log($"GameDataManager: Selected Player Car Index set to {SelectedPlayerCarIndex}");
    }

    public void SetSelectedMap(string mapSceneName)
    {
        SelectedMapName = mapSceneName;
        PlayerPrefs.SetString(SELECTED_MAP_NAME_KEY, SelectedMapName);
        PlayerPrefs.Save();
        Debug.Log($"GameDataManager: Selected Map Name set to {SelectedMapName}");
    }

    public void AddMoney(int amount)
    {
        if (amount < 0) return;
        PlayerMoney += amount;
        OnMoneyChanged?.Invoke(PlayerMoney);
        SaveMoney();
        Debug.Log($"GameDataManager: Added {amount} money. New total: {PlayerMoney}");
    }

    public void AddCurrentScore()
    {
        int scoreToAdd = 0;
        if (ScoreManager.Instance != null)
        {
            scoreToAdd = ScoreManager.Instance.GetScore();
        }
        else
        {
            Debug.LogWarning("AddCurrentScore: ScoreManager.Instance not found.");
            return;
        }

        if (scoreToAdd <= 0) return;

        PlayerMoney += scoreToAdd;
        OnMoneyChanged?.Invoke(PlayerMoney);
        SaveMoney();
        Debug.Log($"GameDataManager: Added {scoreToAdd} from score. New total: {PlayerMoney}");
    }

    public bool SpendMoney(int amount)
    {
        if (amount < 0) return false;
        if (PlayerMoney >= amount)
        {
            PlayerMoney -= amount;
            OnMoneyChanged?.Invoke(PlayerMoney);
            SaveMoney();
            Debug.Log($"GameDataManager: Spent {amount} money. Remaining: {PlayerMoney}");
            return true;
        }
        else
        {
            Debug.LogWarning($"GameDataManager: Not enough money to spend {amount}. Current: {PlayerMoney}");
            return false;
        }
    }

    public void UpdateCurrentMapName(string mapName)
    {
        CurrentMapName = mapName;
        OnMapChanged?.Invoke(CurrentMapName);
        Debug.Log($"GameDataManager: Current Map Name updated to '{CurrentMapName}'");
    }

    public void LoadGameData()
    {
        PlayerMoney = PlayerPrefs.GetInt(PLAYER_MONEY_KEY, 0);
        SelectedPlayerCarIndex = PlayerPrefs.GetInt(SELECTED_CAR_INDEX_KEY, 0);
        SelectedMapName = PlayerPrefs.GetString(SELECTED_MAP_NAME_KEY, "BridgeMap"); // Ensure you have a valid default map name
        Debug.Log($"GameDataManager: Loaded Money: {PlayerMoney}, Loaded Selected Car Index: {SelectedPlayerCarIndex}, Loaded Selected Map: {SelectedMapName}");

        OnMoneyChanged?.Invoke(PlayerMoney);
        OnSelectedCarChanged?.Invoke(SelectedPlayerCarIndex);
    }

    private void SaveMoney()
    {
        PlayerPrefs.SetInt(PLAYER_MONEY_KEY, PlayerMoney);
        PlayerPrefs.Save();
    }

    private void SaveSelectedCarIndex()
    {
        PlayerPrefs.SetInt(SELECTED_CAR_INDEX_KEY, SelectedPlayerCarIndex);
        PlayerPrefs.Save();
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        UpdateCurrentMapName(scene.name);
        LoadGameData();
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this)
        {
            Instance = null;
        }
    }

    public int GetUpgradeLevel(string carName, string upgradeType)
    {
        string key = $"UpgradeLevel_{carName}_{upgradeType}";
        return PlayerPrefs.GetInt(key, 0); // Default to level 0
    }

    public void SetUpgradeLevel(string carName, string upgradeType, int newLevel)
    {
        string key = $"UpgradeLevel_{carName}_{upgradeType}";
        PlayerPrefs.SetInt(key, newLevel);
        PlayerPrefs.Save();
        Debug.Log($"Saved {carName} {upgradeType} Upgrade Level: {newLevel}");
    }

    public void ResetAllSavedData()
    {
        // --- MODIFIED: Use DeleteAll() to clear everything ---
        PlayerPrefs.DeleteAll();
        Debug.Log("GameDataManager: ALL PlayerPrefs have been deleted.");
        // --- END MODIFICATION ---

        // Reset the in-memory variables to their default states
        PlayerMoney = 0;
        SelectedPlayerCarIndex = 0;
        SelectedMapName = "BridgeMap"; // Reset to default map
        LastViewedMapIndex = 0; // Reset this too

        // Notify any listeners that the data has been reset
        OnMoneyChanged?.Invoke(PlayerMoney);
        OnSelectedCarChanged?.Invoke(SelectedPlayerCarIndex);
        Debug.Log("GameDataManager: In-memory data reset to defaults.");
    }
}
