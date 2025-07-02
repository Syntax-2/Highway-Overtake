using UnityEngine;
using UnityEngine.UI; // Required for Button
using TMPro; // Required for TextMeshPro

public class CarShopManager : MonoBehaviour
{
    [Header("Car Configuration")]
    [Tooltip("List of all CarData assets for the cars available in the game. The order here MUST match the order of car GameObjects in the PlayerCarManager.")]
    public CarData[] allCars;

    [Header("UI References")]
    [Tooltip("The main button for buying or selecting a car.")]
    public Button mainActionButton;
    [Tooltip("The TextMeshPro text component on the main action button.")]
    public TextMeshProUGUI mainActionButtonText;
    [Tooltip("The TextMeshPro text component to display the car's name.")]
    public TextMeshProUGUI carNameText;

    // --- Private State ---
    private int _currentlyDisplayedCarIndex = 0;

    public static CarShopManager Instance { get; private set; }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[CarShopManager] Another instance found. Destroying new one.");
            Destroy(gameObject);
            return;
        }
        Instance = this;
        // If your CarShopManager needs to persist across scenes, add DontDestroyOnLoad(gameObject); here.
    }


    private void Start()
    {
        // Add a listener to the main button
        if (mainActionButton != null)
        {
            mainActionButton.onClick.AddListener(OnMainActionButtonClicked);
        }
        else
        {
            Debug.LogError("CarShopManager: Main Action Button is not assigned in the Inspector!", this);
        }

        // Subscribe to money changes here
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        }
    }

    // --- REMOVED OnEnable() and OnDisable() to prevent state reset issues ---
    // The subscription is now handled in Start() and cleaned up in OnDestroy()

    private void OnDestroy()
    {
        // Unsubscribe to prevent errors if this object is destroyed
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }
    }

    /// <summary>
    /// NEW: Call this public function from the UI button that OPENS the garage/shop panel.
    /// It ensures the shop always opens showing the currently equipped car.
    /// </summary>
    public void OnShopPanelOpened()
    {
        Debug.Log("[CarShopManager] Shop panel opened via UI call. Resetting view to equipped car.");

        // Ensure necessary managers exist
        if (GameDataManager.Instance == null || PlayerCarManager.Instance == null)
        {
            Debug.LogError("CarShopManager: GameDataManager or PlayerCarManager not found! This script cannot function.", this);
            return;
        }

        // Get the index of the car that is actually equipped from the GameDataManager.
        _currentlyDisplayedCarIndex = GameDataManager.Instance.SelectedPlayerCarIndex;
        // Tell the PlayerCarManager to show this car visually.
        PlayerCarManager.Instance.SelectCarByIndex(_currentlyDisplayedCarIndex);
        // Update the UI (button, text) for this car.
        UpdateUIForCurrentCar();
    }


    /// <summary>
    /// Called by a UI "Next" button. It now only changes the car being displayed, it does not "select" it.
    /// </summary>
    public void ShowNextCar()
    {
        _currentlyDisplayedCarIndex = (_currentlyDisplayedCarIndex + 1) % allCars.Length;
        // Just update the visual in PlayerCarManager, don't save the selection yet.
        PlayerCarManager.Instance.SelectCarByIndex(_currentlyDisplayedCarIndex);
        UpdateUIForCurrentCar();
    }

    /// <summary>
    /// Called by a UI "Previous" button. It now only changes the car being displayed.
    /// </summary>
    public void ShowPreviousCar()
    {
        _currentlyDisplayedCarIndex = (_currentlyDisplayedCarIndex - 1 + allCars.Length) % allCars.Length;
        // Just update the visual in PlayerCarManager, don't save the selection yet.
        PlayerCarManager.Instance.SelectCarByIndex(_currentlyDisplayedCarIndex);
        UpdateUIForCurrentCar();
    }


    /// <summary>
    /// This is the core logic that updates the main button's text and state for the currently displayed car.
    /// </summary>
    private void UpdateUIForCurrentCar()
    {
        if (allCars.Length == 0) return;
        if (GameDataManager.Instance == null) return; // Add safety check

        CarData currentCar = allCars[_currentlyDisplayedCarIndex];
        bool isUnlocked = IsCarUnlocked(currentCar);

        if (carNameText != null)
        {
            carNameText.text = currentCar.carName;
        }

        if (mainActionButton == null || mainActionButtonText == null) return;

        if (isUnlocked)
        {
            // Check if the car being displayed is the one that's actually equipped.
            if (GameDataManager.Instance.SelectedPlayerCarIndex == _currentlyDisplayedCarIndex)
            {
                mainActionButtonText.text = "EQUIPPED";
                mainActionButton.interactable = false; // Can't equip a car that's already equipped
            }
            else
            {
                mainActionButtonText.text = "SELECT";
                mainActionButton.interactable = true;
            }
        }
        else // Car is locked
        {
            mainActionButtonText.text = currentCar.price + "$";
            // Grey out the button if player can't afford it
            mainActionButton.interactable = (GameDataManager.Instance.PlayerMoney >= currentCar.price);
        }
    }

    /// <summary>
    /// Called when the main action button is clicked. Decides whether to buy or select.
    /// </summary>
    private void OnMainActionButtonClicked()
    {
        CarData currentCar = allCars[_currentlyDisplayedCarIndex];
        bool isUnlocked = IsCarUnlocked(currentCar);

        if (isUnlocked)
        {
            // This is the only place where the selection is officially changed and saved.
            SelectCar();
        }
        else
        {
            BuyCar();
        }
    }

    private void BuyCar()
    {
        CarData carToBuy = allCars[_currentlyDisplayedCarIndex];
        Debug.Log($"Attempting to buy car: {carToBuy.carName} for {carToBuy.price}$");

        if (GameDataManager.Instance.SpendMoney(carToBuy.price))
        {
            Debug.Log($"Successfully bought {carToBuy.carName}!");
            UnlockCar(carToBuy);
            // After buying, the car is unlocked. Update the UI to show the "SELECT" button.
            UpdateUIForCurrentCar();
        }
        else
        {
            Debug.Log($"Failed to buy {carToBuy.carName}. Not enough money.");
        }
    }

    private void SelectCar()
    {
        CarData carToSelect = allCars[_currentlyDisplayedCarIndex];
        Debug.Log($"Selecting car: {carToSelect.carName}");

        // Tell the GameDataManager to save this new selection. This is the official change.
        GameDataManager.Instance.SetSelectedPlayerCar(_currentlyDisplayedCarIndex);

        // Update the UI. The button will now show "EQUIPPED" for this car.
        UpdateUIForCurrentCar();
    }

    private void HandleMoneyChanged(int newMoneyAmount)
    {
        // We only need to update the UI if the currently displayed car is one that is locked for purchase.
        // If it's unlocked, the money amount doesn't affect the button state.
        if (allCars.Length > 0 && !IsCarUnlocked(allCars[_currentlyDisplayedCarIndex]))
        {
            UpdateUIForCurrentCar();
        }
    }

    /// <summary>
    /// Call this from your "Close" or "Back" button for this panel.
    /// This ensures the displayed car reverts to the one that is actually equipped.
    /// </summary>
    public void OnShopPanelClosed()
    {
        Debug.Log("[CarShopManager] Shop panel closed. Reverting visual to equipped car.");
        // Get the index of the truly selected car from the GameDataManager
        int equippedCarIndex = GameDataManager.Instance.SelectedPlayerCarIndex;

        // If the car currently being displayed is not the equipped one, revert it.
        if (_currentlyDisplayedCarIndex != equippedCarIndex)
        {
            // Update the internal index to match the equipped one
            _currentlyDisplayedCarIndex = equippedCarIndex;
            // Tell the PlayerCarManager to display that car
            PlayerCarManager.Instance.SelectCarByIndex(equippedCarIndex);
            // We don't need to update the UI here as the panel is closing.
        }
    }


    // --- SAVE/LOAD & STATE CHECKING (using PlayerPrefs) ---

    private string GetCarSaveKey(CarData car)
    {
        // Create a unique key for each car, e.g., "CarUnlocked_SportRacer"
        return "CarUnlocked_" + car.carName.Replace(" ", "");
    }

    private bool IsCarUnlocked(CarData car)
    {
        if (car.isUnlockedByDefault)
        {
            return true;
        }
        // Check PlayerPrefs. GetInt returns 0 (false) by default if the key doesn't exist.
        return PlayerPrefs.GetInt(GetCarSaveKey(car), 0) == 1;
    }

    private void UnlockCar(CarData car)
    {
        // Set the value to 1 (true) in PlayerPrefs to mark it as unlocked
        PlayerPrefs.SetInt(GetCarSaveKey(car), 1);
        PlayerPrefs.Save(); // Make sure to save changes
    }
}
