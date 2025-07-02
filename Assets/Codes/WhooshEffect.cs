using UnityEngine;
using Unity.Cinemachine; // Use Unity.Cinemachine for modern Cinemachine

[RequireComponent(typeof(Rigidbody))]
public class WhooshEffect : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The audio clip to play for the whoosh sound.")]
    public AudioClip whooshSound;
    [Tooltip("The tag assigned to all AI cars.")]
    public string aiCarTag = "AICar"; // Make sure your AI cars have this tag

    [Header("Effect Settings")]
    [Tooltip("The minimum relative speed between cars to trigger the effect.")]
    public float speedThreshold = 20f; // e.g., a combined speed difference of 72 km/h
    [Tooltip("The volume of the whoosh sound, scaled by relative speed.")]
    [Range(0f, 1f)]
    public float maxVolume = 0.8f;
    [Tooltip("The intensity of the camera shake on whoosh.")]
    public float shakeIntensity = 0.5f;

    // --- Private Variables ---
    private Rigidbody _playerRb;
    private AudioSource _audioSource;
    private CinemachineImpulseSource _impulseSource;

    void Start()
    {
        _playerRb = GetComponent<Rigidbody>();

        // Add an AudioSource component to play the sound from
        _audioSource = gameObject.AddComponent<AudioSource>();
        _audioSource.playOnAwake = false;
        _audioSource.spatialBlend = 1.0f; // Make it a 3D sound

        // Get the impulse source for camera shake
        _impulseSource = GetComponent<CinemachineImpulseSource>();
        if (_impulseSource == null)
        {
            Debug.LogWarning("WhooshEffect: CinemachineImpulseSource not found on this car. Camera shake will not work for whooshes.", this);
        }
    }

    /// <summary>
    /// This is called when another collider enters our trigger zone.
    /// </summary>
    private void OnTriggerEnter(Collider other)
    {
        // Check if the object that entered is an AI car
        if (other.CompareTag(aiCarTag))
        {
            Rigidbody otherRb = other.GetComponent<Rigidbody>();
            if (otherRb != null)
            {
                // Calculate the relative velocity between the two cars
                Vector3 relativeVelocity = _playerRb.linearVelocity - otherRb.linearVelocity;
                float relativeSpeed = relativeVelocity.magnitude;

                // If the relative speed is high enough, trigger the effect
                if (relativeSpeed >= speedThreshold)
                {
                    // Calculate volume based on how much faster the relative speed is than the threshold
                    float volumeScale = Mathf.Clamp01((relativeSpeed - speedThreshold) / speedThreshold);

                    // Play the whoosh sound
                    if (whooshSound != null)
                    {
                        _audioSource.PlayOneShot(whooshSound, maxVolume * volumeScale);
                    }

                    // Trigger a small camera shake
                    if (_impulseSource != null)
                    {
                        // Generate a small impulse in the direction opposite to the relative velocity
                        Vector3 impulseDirection = -relativeVelocity.normalized;
                        _impulseSource.GenerateImpulseWithVelocity(impulseDirection * shakeIntensity);
                    }

                    Debug.Log($"Whoosh effect triggered! Relative speed: {relativeSpeed:F1}");
                }
            }
        }
    }
}
