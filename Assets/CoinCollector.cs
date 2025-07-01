using UnityEngine;

public class CoinCollector : MonoBehaviour
{
    [Header("Effects")]
    [Tooltip("Optional: Particle system prefab to instantiate on collection.")]
    public GameObject collectionEffectPrefab;
    [Tooltip("Optional: Sound effect to play on collection.")]
    public AudioClip collectionSound; // Assign an AudioClip in the Inspector

    // Reference to an AudioSource component (can be added to the coin or a central manager)
    private static AudioSource _audioSource; // Static to potentially share one AudioSource

    public int CoinValue;

    void Start()
    {
        // Find or create a shared AudioSource if needed
        if (_audioSource == null)
        {
            GameObject audioSourceObject = GameObject.Find("CoinAudioSource");
            if (audioSourceObject == null)
            {
                audioSourceObject = new GameObject("CoinAudioSource");
                _audioSource = audioSourceObject.AddComponent<AudioSource>();
                // Optional: Configure AudioSource settings here (volume, spatial blend, etc.)
            }
            else
            {
                _audioSource = audioSourceObject.GetComponent<AudioSource>();
            }

            if (_audioSource == null)
            {
                Debug.LogError("Could not find or create AudioSource for coins.");
            }
        }
    }


    // This function is called when another Collider enters this trigger
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered the trigger has the "Player" tag
        if (other.CompareTag("Player"))
        {
            CollectCoin(other.gameObject); // Pass the player object if needed
        }
    }

    private void CollectCoin(GameObject player)
    {
        // --- Add your collection logic here ---
        Debug.Log("Coin Collected by Player!");
        // Example: Increment score (you'll need a ScoreManager script)
        ScoreManager.Instance.AddScore(CoinValue);

        // --- Visual/Audio Feedback ---
        // Instantiate particle effect at the coin's position
        if (collectionEffectPrefab != null)
        {
            Instantiate(collectionEffectPrefab, transform.position, Quaternion.identity);
        }

        // Play sound effect using the shared AudioSource
        if (collectionSound != null && _audioSource != null)
        {
            // PlayOneShot allows multiple sounds to overlap slightly if collected quickly
            _audioSource.PlayOneShot(collectionSound);
        }

        // --- Destroy the coin ---
        // This removes the coin GameObject from the scene
        Destroy(gameObject);
    }
}
