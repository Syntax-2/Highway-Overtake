using UnityEngine;

public class GameManager : MonoBehaviour
{
    public GameObject meniuCamera;
    public GameObject controllersObject;
    public GameObject mainMeniu;
    public float delayForControlls;
    public static bool gameStarted;

    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        gameStarted = false;
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    public void startGameBTN(float DelayDuration = 0)
    {
        mainMeniu.SetActive(false);
        meniuCamera.SetActive(false);
        
        Invoke("turnOnControllers", DelayDuration);
        
        
    }

    private void turnOnControllers()
    {
        gameStarted=true;
        controllersObject.SetActive(true);

        if (PlayerCarManager.Instance != null)
        {
            // 2. Get the CarController component from the currently active car.
            CarController activeCar = PlayerCarManager.Instance.CurrentPlayerCarController;

            // 3. Check if the controller was found, then call the method on that specific instance.
            if (activeCar != null)
            {
                activeCar.IsNOTKinematic();
            }
            else
            {
                Debug.LogError("Could not start game: PlayerCarManager has no active CarController.", this);
            }
        }
        else
        {
            Debug.LogError("Could not start game: PlayerCarManager.Instance not found.", this);
        }


    }

    





}
