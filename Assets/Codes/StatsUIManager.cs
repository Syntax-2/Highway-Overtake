using UnityEngine;
using TMPro;

public class StatsUIManager : MonoBehaviour
{
    [Header("UI Text References")]
    public TextMeshProUGUI bestScoreText;
    public TextMeshProUGUI totalCoinsText;
    public TextMeshProUGUI totalDistanceText;
    public TextMeshProUGUI carsUnlockedText;
    public TextMeshProUGUI mapsUnlockedText;

    /// <summary>
    /// Call this public function from the UI button that OPENS your stats panel.
    /// It fetches the latest stats and updates all the text fields.
    /// </summary>
    public void OnStatsPanelOpened()
    {
        if (GameDataManager.Instance == null)
        {
            Debug.LogError("StatsUIManager: GameDataManager not found! Cannot display stats.");
            return;
        }

        // --- MODIFIED: Get all data directly from GameDataManager ---
        int bestScore = GameDataManager.Instance.BestScore;
        int totalCoins = GameDataManager.Instance.TotalCoinsCollected;
        float totalDistance = GameDataManager.Instance.TotalDistanceDriven;

        int totalCars = GameDataManager.Instance.allCars.Length;
        int unlockedCars = GameDataManager.Instance.GetUnlockedCarCount();

        int totalMaps = GameDataManager.Instance.allMaps.Length;
        int unlockedMaps = GameDataManager.Instance.GetUnlockedMapCount();
        // --- END MODIFICATION ---

        // Update the UI Text elements
        if (bestScoreText != null) bestScoreText.text = $"Best Score: {bestScore}";
        if (totalCoinsText != null) totalCoinsText.text = $"Total Coins Collected: {totalCoins}";
        if (totalDistanceText != null) totalDistanceText.text = $"Total Distance Driven: {totalDistance:F1} km";
        if (carsUnlockedText != null) carsUnlockedText.text = $"Cars Unlocked: {unlockedCars} / {totalCars}";
        if (mapsUnlockedText != null) mapsUnlockedText.text = $"Maps Unlocked: {unlockedMaps} / {totalMaps}";
    }
}
