using UnityEngine;
using TMPro; // Required for TextMeshPro

[RequireComponent(typeof(TextMeshProUGUI))]
public class MoneyUIDisplay : MonoBehaviour
{
    private TextMeshProUGUI _moneyText;

    private void Awake()
    {
        // Get the TextMeshPro component attached to this same GameObject
        _moneyText = GetComponent<TextMeshProUGUI>();
        if (_moneyText == null)
        {
            Debug.LogError("MoneyUIDisplay: No TextMeshProUGUI component found on this GameObject!", this);
            enabled = false;
        }
    }

    private void OnEnable()
    {
        // Subscribe to the OnMoneyChanged event when this UI element becomes active
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnMoneyChanged += UpdateMoneyDisplay;
            // Immediately update the display with the current value
            UpdateMoneyDisplay(GameDataManager.Instance.PlayerMoney);
            Debug.Log($"MoneyUIDisplay: Subscribed to OnMoneyChanged and updated display. Current money: {GameDataManager.Instance.PlayerMoney}", this);
        }
        else
        {
            Debug.LogError("MoneyUIDisplay: GameDataManager.Instance not found on Enable! Display will not be updated.", this);
            // Optionally, hide or show a default text if the manager isn't ready
            _moneyText.text = "---$";
        }
    }

    private void OnDisable()
    {
        // Unsubscribe from the event when this UI element becomes inactive or is destroyed
        // This is important to prevent errors
        if (GameDataManager.Instance != null)
        {
            GameDataManager.Instance.OnMoneyChanged -= UpdateMoneyDisplay;
        }
    }

    /// <summary>
    /// This method is called by the OnMoneyChanged event from the GameDataManager.
    /// </summary>
    /// <param name="newMoneyAmount">The new total money amount.</param>
    private void UpdateMoneyDisplay(int newMoneyAmount)
    {
        if (_moneyText != null)
        {
            // Update the text with the new value
            _moneyText.text = newMoneyAmount + "$";
        }
    }
}