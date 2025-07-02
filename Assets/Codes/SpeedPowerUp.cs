using UnityEngine;

public class SpeedPowerUp : MonoBehaviour
{
    [Header("Power-Up Settings")]
    [Tooltip("How much to multiply the car's max motor torque by.")]
    public float torqueMultiplier = 1.5f; // e.g., 50% more torque
    [Tooltip("How much to multiply the car's max speed by.")]
    public float speedMultiplier = 1.5f;  // e.g., 50% faster max speed
    [Tooltip("How much to multiply the car's acceleration rate by.")]
    public float accelerationMultiplier = 2.0f; // e.g., Double the acceleration rate
    [Tooltip("How long the speed boost lasts in seconds.")]
    public float boostDuration = 5.0f;

    [Header("Effects")]
    [Tooltip("Optional: Particle system prefab to instantiate on collection.")]
    public GameObject collectionEffectPrefab;
    [Tooltip("Optional: Sound effect to play on collection.")]
    public AudioClip collectionSound;

    // Shared AudioSource for playing sounds
    private static AudioSource _powerUpAudioSource;

    void Start()
    {
        // Attempt to find or create a shared AudioSource for power-up sounds
        if (_powerUpAudioSource == null)
        {
            GameObject audioSourceObject = GameObject.Find("PowerUpAudioSource");
            if (audioSourceObject == null)
            {
                audioSourceObject = new GameObject("PowerUpAudioSource");
                _powerUpAudioSource = audioSourceObject.AddComponent<AudioSource>();
            }
            else
            {
                _powerUpAudioSource = audioSourceObject.GetComponent<AudioSource>();
            }

            if (_powerUpAudioSource == null)
            {
                Debug.LogError("Could not find or create AudioSource for power-ups on SpeedPowerUp.");
            }
        }
    }


    private void OnTriggerEnter(Collider other)
    {
        // Check if the colliding object is the player
        if (other.CompareTag("Player"))
        {
            // Try to get the CarController component from the player
            CarController carController = other.GetComponent<CarController>();

            if (carController != null)
            {
                // Activate the speed boost on the car
                carController.ActivateSpeedBoost(torqueMultiplier, speedMultiplier, accelerationMultiplier, boostDuration);

                // --- Visual/Audio Feedback ---
                if (collectionEffectPrefab != null)
                {
                    Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
                }

                if (collectionSound != null && _powerUpAudioSource != null)
                {
                    _powerUpAudioSource.PlayOneShot(collectionSound);
                }

                // Destroy the power-up GameObject after it's collected
                Destroy(gameObject);
            }
            else
            {
                Debug.LogWarning("SpeedPowerUp collided with Player, but CarController component was not found.");
            }
        }
    }
}
