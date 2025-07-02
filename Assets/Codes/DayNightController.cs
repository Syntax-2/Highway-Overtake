using UnityEngine;

public class DayNightController : MonoBehaviour
{
    [Header("Lighting & Time")]
    [Tooltip("The main Directional Light in your scene that acts as the sun.")]
    public Light sunLight;
    [Tooltip("How long a full day-night cycle takes in real-life seconds. E.g., 120 seconds = 2 minutes for a full day.")]
    [Range(10, 600)]
    public float cycleDurationSeconds = 120f;

    [Header("Color Settings")]
    [Tooltip("The color of the ambient light during the day.")]
    public Color dayAmbientColor = new Color(0.5f, 0.5f, 0.5f);
    [Tooltip("The color of the ambient light at sunset/sunrise.")]
    public Color sunsetAmbientColor = new Color(0.8f, 0.4f, 0.2f);
    [Tooltip("The color of the ambient light at night.")]
    public Color nightAmbientColor = new Color(0.1f, 0.1f, 0.25f);

    [Tooltip("The color of the fog during the day.")]
    public Color dayFogColor = new Color(0.7f, 0.8f, 0.9f);
    [Tooltip("The color of the fog at sunset/sunrise.")]
    public Color sunsetFogColor = new Color(0.9f, 0.6f, 0.4f);
    [Tooltip("The color of the fog at night.")]
    public Color nightFogColor = new Color(0.05f, 0.05f, 0.1f);

    // --- Private State ---
    private float _timeOfDay; // 0 = midnight, 0.25 = sunrise, 0.5 = noon, 0.75 = sunset, 1 = midnight

    void Start()
    {
        // --- Validation ---
        if (sunLight == null)
        {
            // Try to find the main directional light automatically
            foreach (Light light in FindObjectsByType<Light>(FindObjectsSortMode.None))
            {
                if (light.type == LightType.Directional)
                {
                    sunLight = light;
                    Debug.LogWarning("DayNightController: Sun Light was not assigned. Found a Directional Light automatically.", this);
                    break;
                }
            }
        }
        if (sunLight == null)
        {
            Debug.LogError("DayNightController: No Directional Light found or assigned. Disabling script.", this);
            enabled = false;
            return;
        }

        // Set initial time of day based on the sun's starting rotation
        float initialSunDot = Vector3.Dot(sunLight.transform.forward, Vector3.down);
        _timeOfDay = (initialSunDot + 1) / 2; // Normalize to 0-1 range
    }

    void Update()
    {
        if (sunLight == null) return;

        // --- Rotate the Sun ---
        // Rotate the light around the world's X-axis to simulate sun rising/setting
        float rotationSpeed = 360f / cycleDurationSeconds;
        sunLight.transform.Rotate(Vector3.right, rotationSpeed * Time.deltaTime);

        // --- Calculate Time of Day ---
        // Get the dot product of the light's forward direction and the world's down direction.
        // This gives -1 when sun is pointing straight down (noon), 1 when straight up (midnight), and 0 at horizon.
        float sunDot = Vector3.Dot(sunLight.transform.forward, Vector3.down);
        _timeOfDay = (sunDot + 1) / 2; // Normalize this to a 0-1 range

        // --- Update Lighting and Fog ---
        // ** MODIFIED: Pass sunDot as a parameter **
        UpdateLightingAndFog(_timeOfDay, sunDot);
    }

  
    /// <param name="time">A value from 0 (midnight) to 1 (noon) and back to 0.</param>
    /// <param name="sunDot">The dot product representing the sun's angle, from -1 (noon) to 1 (midnight).</param>
    private void UpdateLightingAndFog(float time, float sunDot) // ** MODIFIED: Added sunDot parameter **
    {
        // Interpolate between night and day colors
        Color currentAmbientColor;
        Color currentFogColor;

        if (time < 0.5f) // Night to Day (Sunrise)
        {
            float lerpFactor = time / 0.5f; // 0 at midnight, 1 at sunrise horizon
            currentAmbientColor = Color.Lerp(nightAmbientColor, sunsetAmbientColor, lerpFactor);
            currentFogColor = Color.Lerp(nightFogColor, sunsetFogColor, lerpFactor);
        }
        else // Day to Night (Sunset)
        {
            float lerpFactor = (time - 0.5f) / 0.5f; // 0 at noon, 1 at sunset horizon
            currentAmbientColor = Color.Lerp(dayAmbientColor, sunsetAmbientColor, lerpFactor);
            currentFogColor = Color.Lerp(dayFogColor, sunsetFogColor, lerpFactor);
        }

        // As it gets closer to sunset/sunrise, blend in more of that color
        float sunsetFactor = 1 - Mathf.Abs(sunDot); // 1 at horizon, 0 at noon/midnight
        RenderSettings.ambientLight = Color.Lerp(currentAmbientColor, sunsetAmbientColor, sunsetFactor);
        RenderSettings.fogColor = Color.Lerp(currentFogColor, sunsetFogColor, sunsetFactor);

        // Adjust sun intensity - full brightness during day, off at night
        sunLight.intensity = Mathf.Clamp01(sunDot);
    }
}
