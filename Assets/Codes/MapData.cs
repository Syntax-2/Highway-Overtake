using UnityEngine;
using UnityEngine.UI; // For Image/Sprite

// This attribute allows you to create instances of this class from the Assets menu.
[CreateAssetMenu(fileName = "NewMapData", menuName = "Car Game/Map Data")]
public class MapData : ScriptableObject
{
    [Header("Map Details")]
    [Tooltip("The display name of the map.")]
    public string mapName = "New Map";

    [Tooltip("The exact name of the scene file to load for this map.")]
    public string sceneName = "YourMapSceneName";

    [Tooltip("An image/screenshot of the map to display in the UI.")]
    public Sprite mapImage;

    [Tooltip("The price of the map in the shop.")]
    public int price = 2500;

    [Tooltip("Is this map unlocked from the very start of the game? (Should be true for your first map).")]
    public bool isUnlockedByDefault = false;
}
