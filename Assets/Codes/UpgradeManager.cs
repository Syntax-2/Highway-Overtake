using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System.Collections.Generic; // Required for List

public class UpgradeManager : MonoBehaviour
{
    [Header("Upgrades Configuration")]
    [Tooltip("A list of all CarUpgrades ScriptableObject assets in your game.")]
    public CarUpgrades[] allCarUpgrades;

    [Header("UI References")]
    public TextMeshProUGUI carNameToUpgradeText;

    [Header("Engine UI")]
    public Button engineUpgradeButton;
    public TextMeshProUGUI engineUpgradeText;
    [Tooltip("Text to display the current engine level, e.g., 'Level: 1/5'.")]
    public TextMeshProUGUI engineLevelText; // New

    [Header("Turbo UI")]
    public Button turboUpgradeButton;
    public TextMeshProUGUI turboUpgradeText;
    [Tooltip("Text to display the current turbo level.")]
    public TextMeshProUGUI turboLevelText; // New

    [Header("Brakes UI")]
    public Button brakesUpgradeButton;
    public TextMeshProUGUI brakesUpgradeText;
    [Tooltip("Text to display the current brakes level.")]
    public TextMeshProUGUI brakesLevelText; // New


    private CarUpgrades _currentCarUpgrades;
    private string _currentCarName;

    private void Start()
    {
        // Subscribe to money changes here for robustness
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnMoneyChanged += HandleMoneyChanged;
        }
    }

    private void OnDestroy()
    {
        // Unsubscribe here when the object is destroyed
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnMoneyChanged -= HandleMoneyChanged;
        }
    }

    /// <summary>
    /// Call this when the Upgrade Panel is opened to show stats for the currently equipped car.
    /// </summary>
    public void OnUpgradePanelOpened()
    {
        if (GameDataManager.Instance == null || PlayerCarManager.Instance == null)
        {
            Debug.LogError("UpgradeManager cannot function without GameDataManager and PlayerCarManager.");
            return;
        }

        GameObject currentCarObject = PlayerCarManager.Instance.CurrentPlayerCarGameObject;
        if (currentCarObject == null)
        {
            Debug.LogError("No car is currently equipped. Cannot open upgrade panel.");
            return;
        }

        _currentCarName = currentCarObject.name;
        _currentCarUpgrades = System.Array.Find(allCarUpgrades, upgrade => upgrade.carName == _currentCarName);

        if (_currentCarUpgrades == null)
        {
            Debug.LogError($"No CarUpgrades asset found for car named '{_currentCarName}'. Make sure the carName in the asset matches the GameObject name.");
            // Optionally, disable all buttons if no upgrade data is found for this car
            if (engineUpgradeButton != null) engineUpgradeButton.interactable = false;
            if (turboUpgradeButton != null) turboUpgradeButton.interactable = false;
            if (brakesUpgradeButton != null) brakesUpgradeButton.interactable = false;
            if (engineUpgradeText != null) engineUpgradeText.text = "N/A";
            if (turboUpgradeText != null) turboUpgradeText.text = "N/A";
            if (brakesUpgradeText != null) brakesUpgradeText.text = "N/A";
            return;
        }

        UpdateAllUIButtons();
    }

    private void UpdateAllUIButtons()
    {
        if (carNameToUpgradeText != null)
        {
            carNameToUpgradeText.text = _currentCarName + " Upgrades";
        }

        UpdateSingleButtonUI(engineUpgradeButton, engineUpgradeText, engineLevelText, "Engine", _currentCarUpgrades.engineUpgrades);
        UpdateSingleButtonUI(turboUpgradeButton, turboUpgradeText, turboLevelText, "Turbo", _currentCarUpgrades.turboUpgrades);
        UpdateSingleButtonUI(brakesUpgradeButton, brakesUpgradeText, brakesLevelText, "Brakes", _currentCarUpgrades.brakeUpgrades);
    }

    /// <summary>
    /// Updates a single upgrade element (button text, level text, interactability).
    /// </summary>
    private void UpdateSingleButtonUI(Button button, TextMeshProUGUI buttonText, TextMeshProUGUI levelText, string upgradeName, List<CarUpgrades.UpgradeLevel> upgradePath)
    {
        if (button == null || buttonText == null || levelText == null) return;

        int currentLevel = GameDataManager.Instance.GetUpgradeLevel(_currentCarName, upgradeName);
        int maxLevel = upgradePath.Count;

        // Update level display text, e.g., "Level: 2 / 5"
        levelText.text = $"Level: {currentLevel} / {maxLevel}";

        if (currentLevel >= maxLevel) // Max level reached
        {
            buttonText.text = "MAX LEVEL";
            button.interactable = false;
        }
        else
        {
            CarUpgrades.UpgradeLevel nextUpgrade = upgradePath[currentLevel];
            int costOfNextLevel = nextUpgrade.cost;

            // Build a string showing the benefits of the next upgrade
            string benefitText = GetBenefitText(nextUpgrade);

            // Set the button text to include the cost and benefits
            // Using rich text to make the benefit text smaller
            buttonText.text = $"UPGRADE ({costOfNextLevel}$)\n<size=80%>{benefitText}</size>";
            button.interactable = GameDataManager.Instance.PlayerMoney >= costOfNextLevel;
        }
    }

    /// <summary>
    /// Generates a description of the stat increases for an upgrade level.
    /// </summary>
    private string GetBenefitText(CarUpgrades.UpgradeLevel upgradeLevel)
    {
        string benefits = "";
        if (upgradeLevel.motorTorque_Increase > 0) benefits += $"+{upgradeLevel.motorTorque_Increase} Torque ";
        if (upgradeLevel.maxSpeed_Increase > 0) benefits += $"+{upgradeLevel.maxSpeed_Increase} Top Speed ";
        if (upgradeLevel.accelerationRate_Increase > 0) benefits += $"+{upgradeLevel.accelerationRate_Increase} Accel ";
        if (upgradeLevel.brakeTorque_Increase > 0) benefits += $"+{upgradeLevel.brakeTorque_Increase} Brakes ";

        return benefits.Trim(); // Return the formatted string
    }

    public void OnUpgradeEngineClicked()
    {
        if (_currentCarUpgrades == null) return;
        TryPerformUpgrade("Engine", _currentCarUpgrades.engineUpgrades);
    }

    public void OnUpgradeTurboClicked()
    {
        if (_currentCarUpgrades == null) return;
        TryPerformUpgrade("Turbo", _currentCarUpgrades.turboUpgrades);
    }

    public void OnUpgradeBrakesClicked()
    {
        if (_currentCarUpgrades == null) return;
        TryPerformUpgrade("Brakes", _currentCarUpgrades.brakeUpgrades);
    }

    private void TryPerformUpgrade(string upgradeName, List<CarUpgrades.UpgradeLevel> upgradePath)
    {
        if (_currentCarUpgrades == null)
        {
            Debug.LogError("Cannot perform upgrade, no CarUpgrades data loaded for current car.");
            return;
        }

        int currentLevel = GameDataManager.Instance.GetUpgradeLevel(_currentCarName, upgradeName);

        if (currentLevel >= upgradePath.Count)
        {
            Debug.Log($"{upgradeName} is already at max level.");
            return;
        }

        int cost = upgradePath[currentLevel].cost;
        if (GameDataManager.Instance.SpendMoney(cost))
        {
            // Purchase successful
            int newLevel = currentLevel + 1;
            GameDataManager.Instance.SetUpgradeLevel(_currentCarName, upgradeName, newLevel);
            Debug.Log($"Successfully upgraded {upgradeName} for {_currentCarName} to level {newLevel}");

            // Re-apply upgrades to the currently active car if it's the one being upgraded
            if (PlayerCarManager.Instance.CurrentPlayerCarGameObject != null && PlayerCarManager.Instance.CurrentPlayerCarGameObject.name == _currentCarName)
            {
                CarController controller = PlayerCarManager.Instance.CurrentPlayerCarController;
                if (controller != null)
                {
                    controller.ApplyAllUpgrades();
                }
            }
            UpdateAllUIButtons();
        }
        else
        {
            Debug.Log($"Not enough money to upgrade {upgradeName}.");
        }
    }

    private void HandleMoneyChanged(int newMoneyAmount)
    {
        // When money changes, refresh the buttons to see if they should become interactable.
        if (_currentCarUpgrades != null)
        {
            UpdateAllUIButtons();
        }
    }
}