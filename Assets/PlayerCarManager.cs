using UnityEngine;
using System; // Required for Action
using System.Collections.Generic; // Required for List

public class PlayerCarManager : MonoBehaviour
{
    public static PlayerCarManager Instance { get; private set; }

    [Header("Car Setup")]
    [Tooltip("Add all potential player car GameObjects from your scene here. One will be active.")]
    public List<GameObject> availablePlayerCars = new List<GameObject>();

    [Tooltip("A fallback index if GameDataManager isn't available. The value from GameDataManager will be used if it exists.")]
    public int initiallySelectedCarIndex = 0;

    // --- Public Properties to Access Current Car ---
    public GameObject CurrentPlayerCarGameObject { get; private set; }
    public Transform CurrentPlayerTransform => CurrentPlayerCarGameObject != null ? CurrentPlayerCarGameObject.transform : null;
    public CarController CurrentPlayerCarController => CurrentPlayerCarGameObject != null ? CurrentPlayerCarGameObject.GetComponent<CarController>() : null;

    // --- Event ---
    public event Action<GameObject> OnPlayerCarChanged;

    private void Awake()
    {
        // Awake's ONLY job is to set up the Singleton and persist.
        // DO NOT communicate with other Singletons here.
        Debug.Log("[PlayerCarManager] Awake: Setting up instance.");
        if (Instance != null && Instance != this)
        {
            Debug.LogWarning("[PlayerCarManager] Another instance found, destroying this one.");
            Destroy(this.gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(this.gameObject);
        Debug.Log("[PlayerCarManager] Awake: Instance is set and will persist.");
    }

    void Start()
    {
        // In Start(), all other Awake() methods are guaranteed to have run.
        // It is now safe to communicate with other managers.
        Debug.Log("[PlayerCarManager] Start: Initializing car selection.");

        // Get the saved selected car index from GameDataManager
        if (GameDataManager.Instance != null)
        {
            initiallySelectedCarIndex = GameDataManager.Instance.SelectedPlayerCarIndex;
            Debug.Log($"[PlayerCarManager] Loaded 'initiallySelectedCarIndex' from GameDataManager: {initiallySelectedCarIndex}");
        }
        else
        {
            Debug.LogWarning("[PlayerCarManager] GameDataManager not found in Start. Using Inspector value for 'initiallySelectedCarIndex'.");
        }


        // Now, proceed with the rest of the original logic.
        if (availablePlayerCars == null || availablePlayerCars.Count == 0)
        {
            Debug.LogError("[PlayerCarManager] 'Available Player Cars' list is empty! Please assign car GameObjects in the Inspector.", this);
            enabled = false;
            return;
        }

        if (initiallySelectedCarIndex < 0 || initiallySelectedCarIndex >= availablePlayerCars.Count)
        {
            Debug.LogWarning($"[PlayerCarManager] 'Initially Selected Car Index' ({initiallySelectedCarIndex}) is out of bounds. Defaulting to 0.", this);
            initiallySelectedCarIndex = 0;
        }

        for (int i = 0; i < availablePlayerCars.Count; i++)
        {
            if (availablePlayerCars[i] != null)
            {
                availablePlayerCars[i].SetActive(false);
            }
        }

        SelectCarByIndex(initiallySelectedCarIndex, true); // true for initial setup
        Debug.Log("[PlayerCarManager] Start: Initial car selection completed.");

        Debug.Log(CurrentPlayerCarGameObject.name);


    }

    public void SelectCarByIndex(int carIndex, bool isInitialSetup = false)
    {
        if (availablePlayerCars == null || carIndex < 0 || carIndex >= availablePlayerCars.Count)
        {
            Debug.LogError($"[PlayerCarManager] Invalid car index ({carIndex}) or 'Available Cars' list is not set up.", this);
            return;
        }

        GameObject carToSelect = availablePlayerCars[carIndex];
        if (carToSelect == null)
        {
            Debug.LogError($"[PlayerCarManager] Car GameObject at selected index {carIndex} is null. Cannot activate.", this);
            return;
        }

        if (CurrentPlayerCarGameObject != null && CurrentPlayerCarGameObject != carToSelect)
        {
            CurrentPlayerCarGameObject.SetActive(false);
        }

        CurrentPlayerCarGameObject = carToSelect;
        CurrentPlayerCarGameObject.SetActive(true);

        if (!isInitialSetup)
        {
            Debug.Log($"[PlayerCarManager] Activated new car '{CurrentPlayerCarGameObject.name}' at index {carIndex}.");
        }
        else
        {
            Debug.Log($"[PlayerCarManager] Initially activated car '{CurrentPlayerCarGameObject.name}' at index {carIndex}.");
        }

        OnPlayerCarChanged?.Invoke(CurrentPlayerCarGameObject);
    }

    public void SelectNextCar()
    {
        if (availablePlayerCars == null || availablePlayerCars.Count <= 1) return;
        int currentIndex = (CurrentPlayerCarGameObject != null) ? availablePlayerCars.IndexOf(CurrentPlayerCarGameObject) : 0;
        int nextIndex = (currentIndex + 1) % availablePlayerCars.Count;
        SelectCarByIndex(nextIndex);
    }

    public void SelectPreviousCar()
    {
        if (availablePlayerCars == null || availablePlayerCars.Count <= 1) return;
        int currentIndex = (CurrentPlayerCarGameObject != null) ? availablePlayerCars.IndexOf(CurrentPlayerCarGameObject) : 0;
        int prevIndex = (currentIndex - 1 + availablePlayerCars.Count) % availablePlayerCars.Count;
        SelectCarByIndex(prevIndex);
    }
}
