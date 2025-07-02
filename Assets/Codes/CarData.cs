using UnityEngine;

// This attribute allows you to create instances of this class from the Assets menu.
[CreateAssetMenu(fileName = "NewCarData", menuName = "Car Game/Car Data")]
public class CarData : ScriptableObject
{
    [Header("Car Details")]
    [Tooltip("The name of the car, used for display and saving.")]
    public string carName = "New Car";

    [Tooltip("The price of the car in the shop.")]
    public int price = 1000;

    [Tooltip("Is this car unlocked from the very start of the game? (Should be true for the default car).")]
    public bool isUnlockedByDefault = false;

    // You could add more unique data here later, like:
    // public GameObject carPrefab; // If you were spawning cars from prefabs instead of having them in the scene
    // public float topSpeedStat;
    // public float handlingStat;
}