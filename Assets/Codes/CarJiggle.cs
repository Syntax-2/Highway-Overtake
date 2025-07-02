using UnityEngine;

[RequireComponent(typeof(Rigidbody))]
public class CarJiggle : MonoBehaviour
{
    [Header("Setup")]
    [Tooltip("The Transform of the child GameObject that represents the car's visual model. This is what will be jiggled.")]
    public Transform carVisualBody;

    [Header("Jiggle Settings")]
    [Tooltip("How much the car body can tilt and roll. A small value like 0.5 or 1 is usually best.")]
    public float jiggleAmplitude = 0.75f;
    [Tooltip("How fast the jiggle motion is. Higher values mean faster vibrations.")]
    public float jiggleFrequency = 5f;
    [Tooltip("How quickly the jiggle effect smooths out when stopping.")]
    public float smoothingSpeed = 5f;

    // --- Private Variables ---
    private Rigidbody _rb;
    private Vector3 _initialLocalPosition; // To prevent the jiggle from moving the car body
    private float _noiseOffsetX;
    private float _noiseOffsetZ;

    void Start()
    {
        _rb = GetComponent<Rigidbody>();
        if (carVisualBody == null)
        {
            Debug.LogError("CarJiggle: 'Car Visual Body' is not assigned! This script cannot function without it.", this);
            enabled = false;
            return;
        }

        // Store the starting local position and rotation to always return to it
        _initialLocalPosition = carVisualBody.localPosition;

        // Use random offsets to ensure every car jiggles differently if you have multiple
        _noiseOffsetX = Random.Range(0f, 1000f);
        _noiseOffsetZ = Random.Range(0f, 1000f);
    }

    void Update()
    {
        if (carVisualBody == null) return;

        // Calculate a speed factor (0 when stopped, 1 at high speed)
        // Using 20 m/s (72 km/h) as a reference for full jiggle effect
        float speedFactor = Mathf.Clamp01(_rb.linearVelocity.magnitude / 20f);

        // Generate smooth, random-like values using Perlin Noise
        // Time.time makes the noise value change over time
        float time = Time.time * jiggleFrequency;

        // Map Perlin noise (0 to 1) to a -1 to 1 range
        float pitchNoise = (Mathf.PerlinNoise(time, _noiseOffsetX) * 2f) - 1f;
        float rollNoise = (Mathf.PerlinNoise(time, _noiseOffsetZ) * 2f) - 1f;

        // Calculate the target rotation based on noise, amplitude, and speed
        float targetPitch = pitchNoise * jiggleAmplitude * speedFactor;
        float targetRoll = rollNoise * jiggleAmplitude * speedFactor;

        // Create the target jiggle rotation
        Quaternion targetRotation = Quaternion.Euler(targetPitch, 0, targetRoll);

        // Smoothly interpolate the visual body's local rotation towards the target jiggle rotation
        carVisualBody.localRotation = Quaternion.Slerp(carVisualBody.localRotation, targetRotation, Time.deltaTime * smoothingSpeed);

        // Ensure the visual body doesn't move from its original local position
        carVisualBody.localPosition = _initialLocalPosition;
    }
}
