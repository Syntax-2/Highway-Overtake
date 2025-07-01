using UnityEngine;

public class AmbientParticleController : MonoBehaviour
{
    // Enum to define the different particle themes available
    public enum ParticleTheme
    {
        None,
        Dust,
        Snow,
        Rain
    }

    [Header("Setup")]
    [Tooltip("The theme of the particles to display in this scene.")]
    public ParticleTheme theme = ParticleTheme.None;

    // REMOVED: public Transform playerTransform;

    [Header("Particle Area")]
    [Tooltip("The size of the box around the player where particles will be emitted.")]
    public Vector3 emissionBoxSize = new Vector3(20, 20, 50); // X, Y, Z size

    // --- Private Variables ---
    private ParticleSystem _particleSystem;
    private Transform _activePlayerTransform; // Internal reference to the current player car

    // OnEnable is called when the object becomes enabled and active.
    private void OnEnable()
    {
        // Attempt to find the PlayerCarManager and subscribe to its event
        if (PlayerCarManager.Instance != null)
        {
            // Subscribe the HandlePlayerCarChanged method to the event
            PlayerCarManager.Instance.OnPlayerCarChanged += HandlePlayerCarChanged;

            // Immediately get the currently active car if one already exists
            if (PlayerCarManager.Instance.CurrentPlayerCarGameObject != null)
            {
                HandlePlayerCarChanged(PlayerCarManager.Instance.CurrentPlayerCarGameObject);
            }
        }
        else
        {
            Debug.LogError("AmbientParticleController: PlayerCarManager.Instance not found! This script cannot function without it. Ensure it is in your scene and its execution order is set correctly.", this);
            enabled = false; // Disable this script if the manager is missing
        }
    }

    // OnDisable is called when the object becomes disabled or inactive.
    private void OnDisable()
    {
        // Unsubscribe from the event to prevent errors when this object is destroyed
        if (PlayerCarManager.Instance != null)
        {
            PlayerCarManager.Instance.OnPlayerCarChanged -= HandlePlayerCarChanged;
        }
    }

    void Start()
    {
        // --- Create and Configure Particles ---
        if (theme != ParticleTheme.None)
        {
            CreateParticleSystem();
            ConfigureParticlesForTheme(theme);
        }
    }

    // LateUpdate is called after all Update functions have been called.
    // This is a good place to update camera or particle positions.
    void LateUpdate()
    {
        // Make the particle system follow the player's position
        if (_activePlayerTransform != null && _particleSystem != null)
        {
            _particleSystem.transform.position = _activePlayerTransform.position;
        }
    }

    /// <summary>
    /// This method is called by the PlayerCarManager's event whenever the active car changes.
    /// </summary>
    /// <param name="newPlayerCar">The new active player car GameObject.</param>
    private void HandlePlayerCarChanged(GameObject newPlayerCar)
    {
        if (newPlayerCar != null)
        {
            _activePlayerTransform = newPlayerCar.transform;
            Debug.Log($"[AmbientParticleController] Player transform updated to '{newPlayerCar.name}'.");
        }
        else
        {
            _activePlayerTransform = null;
            Debug.LogWarning("[AmbientParticleController] Player transform set to null.");
        }
    }

    /// <summary>
    /// Creates the GameObject and ParticleSystem component.
    /// </summary>
    private void CreateParticleSystem()
    {
        GameObject psObject = new GameObject(theme.ToString() + "Particles");
        psObject.transform.SetParent(this.transform); // Keep it organized
        _particleSystem = psObject.AddComponent<ParticleSystem>();

        // --- FIX: Create and assign a material to prevent pink particles ---
        ParticleSystemRenderer psRenderer = psObject.GetComponent<ParticleSystemRenderer>();
        if (psRenderer != null)
        {
            // Create a new simple material that works with URP
            Material particleMaterial = new Material(Shader.Find("Universal Render Pipeline/Particles/Unlit"));
            // Set the material's main color property. We can use white and let the particle system's color module handle the tinting.
            particleMaterial.color = Color.white;
            psRenderer.material = particleMaterial;
        }
        // --- END FIX ---
    }

    /// <summary>
    /// Main configuration function that calls the specific setup method based on the theme.
    /// </summary>
    private void ConfigureParticlesForTheme(ParticleTheme selectedTheme)
    {
        if (_particleSystem == null) return;

        // --- Common Shape Settings ---
        var shape = _particleSystem.shape;
        shape.shapeType = ParticleSystemShapeType.Box;
        shape.scale = emissionBoxSize;

        // --- Theme-Specific Settings ---
        switch (selectedTheme)
        {
            case ParticleTheme.Dust:
                SetupDustEffect();
                break;
            case ParticleTheme.Snow:
                SetupSnowEffect();
                break;
            case ParticleTheme.Rain:
                SetupRainEffect();
                break;
        }
    }

    private void SetupDustEffect()
    {
        var main = _particleSystem.main;
        main.startLifetime = 4f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(0.1f, 0.5f);
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.15f);
        main.startColor = new Color(0.8f, 0.7f, 0.6f, 0.05f); // Semi-transparent brown/yellow
        main.maxParticles = 300;

        var emission = _particleSystem.emission;
        emission.rateOverTime = 50;

        var rotation = _particleSystem.rotationOverLifetime;
        rotation.enabled = true;
        rotation.z = new ParticleSystem.MinMaxCurve(-90f, 90f);
    }

    private void SetupSnowEffect()
    {
        var main = _particleSystem.main;
        main.startLifetime = 5f;
        main.startSpeed = new ParticleSystem.MinMaxCurve(1f, 3f); // Snow falls faster than dust floats
        main.startSize = new ParticleSystem.MinMaxCurve(0.05f, 0.2f);
        main.startColor = new Color(1f, 1f, 1f, 0.7f); // Mostly opaque white
        main.maxParticles = 500;
        main.gravityModifier = 0.1f; // Add a little gravity

        var emission = _particleSystem.emission;
        emission.rateOverTime = 100;

        var noise = _particleSystem.noise; // Add noise for swirling motion
        noise.enabled = true;
        noise.strength = 0.5f;
        noise.frequency = 0.2f;
    }

    private void SetupRainEffect()
    {
        var main = _particleSystem.main;
        main.startLifetime = 1.5f;
        main.startSpeed = 15f; // Rain falls fast
        main.startSize = new ParticleSystem.MinMaxCurve(0.02f, 0.04f);
        main.startColor = new Color(0.8f, 0.9f, 1f, 0.4f); // Semi-transparent light blue
        main.maxParticles = 1000;
        main.gravityModifier = 1f;

        var emission = _particleSystem.emission;
        emission.rateOverTime = 400;

        // Use stretched particles to simulate rain streaks
        var renderer = _particleSystem.GetComponent<ParticleSystemRenderer>();
        renderer.renderMode = ParticleSystemRenderMode.Stretch;
        renderer.lengthScale = 5f; // Adjust to control streak length
        renderer.velocityScale = 0.1f;
    }
}
