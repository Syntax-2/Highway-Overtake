using UnityEngine;
using UnityEngine.EventSystems;
using System.Collections;
using Unity.Cinemachine;
using UnityEngine.Rendering; // Required for Post-Processing Volume
using UnityEngine.Rendering.Universal; // Required for URP Post-Processing effects
using UnityEngine.UI; // Required for the new dashboard indicator
using TMPro;

public class CarController : MonoBehaviour
{
    [Header("Vehicle Physics")]
    public float maxMotorTorque = 1500f;
    public float accelerationRate = 5000f;
    public float coastingDecelerationRate = 500f;
    public float maxBrakeTorque = 3000f;
    public float maxSteerAngle = 30f;
    public float maxSpeed = 50f;
    public float centerOfMassYOffset = -0.5f;


    [Header("Wheel Colliders")]
    public WheelCollider frontLeftWheelCollider;
    public WheelCollider frontRightWheelCollider;
    public WheelCollider rearLeftWheelCollider;
    public WheelCollider rearRightWheelCollider;

    public bool ForwardDirection;

    [Header("Input")]
    public VariableJoystick variableJoystick;

    [Header("State & References")]
    public Rigidbody rb;
    public GameObject GameOverScreen;
    public GameObject driversCamera;
    public GameObject mainMeniuCamera;
    public GameObject effectsPrefab;
    public GameObject controlsUI;

    [Header("Camera Effects")]
    public CinemachineCamera playerVirtualCamera;
    public float maxFOV = 75f;
    public float fovChangeSpeed = 4f;
    [Tooltip("The sound to play during a speed boost. Assign in Inspector.")]
    public AudioClip boostSound; // NEW: Assignable sound clip

    private CinemachineImpulseSource _impulseSource;
    private float _baseFOV;


    // --- Private Variables ---
    private bool _gasPressed = false;
    private bool _brakePressed = false;
    private float _currentAppliedTorque = 0f;
    private float _currentSpeedKPH = 0f;

    // --- Speed Boost Variables ---
    private float _originalMaxMotorTorque;
    private float _originalMaxSpeed;
    private float _originalAccelerationRate;
    private bool _isSpeedBoostActive = false;
    private Coroutine _speedBoostCoroutine;

    // --- Boost Effects Variables ---
    private Volume _postProcessVolume;
    private ChromaticAberration _chromaticAberration;
    private LensDistortion _lensDistortion;
    private ParticleSystem _speedLines;
    private AudioSource _boostAudioSource;
    private Image _boostIndicatorLight;
    private float _boostEffectIntensity = 0f;


    [Header("Collision Settings")]
    public float crashSpeedThreshold = 10.0f;
    public LayerMask crashLayers;
    private float crashCooldown = 0.5f;
    private float lastCrashTime = -1f;


    // --- Upgrade System Variables ---
    private CarData _carData;
    private CarUpgrades _carUpgrades;

    [Header("UI")]
    [Tooltip("The TextMeshPro text element to display the current speed.")]
    public TextMeshProUGUI speedoText;


    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (rb == null)
        {
            Debug.LogError("Player car needs a Rigidbody component!", this);
            enabled = false;
            return;
        }

        rb.isKinematic = true;

        _impulseSource = GetComponent<CinemachineImpulseSource>();
        if (_impulseSource == null)
        {
            Debug.LogWarning("CarController: CinemachineImpulseSource not found. Camera shake will not work.", this);
        }

        if (playerVirtualCamera != null)
        {
            _baseFOV = playerVirtualCamera.Lens.FieldOfView;
        }
        else
        {
            Debug.LogWarning("CarController: Player Virtual Camera not assigned. FOV change will not work.", this);
        }

        Vector3 currentCoM = rb.centerOfMass;
        currentCoM.y += centerOfMassYOffset;
        rb.centerOfMass = currentCoM;
        rb.isKinematic = true;

        lastCrashTime = -crashCooldown;

        if (!frontLeftWheelCollider || !frontRightWheelCollider || !rearLeftWheelCollider || !rearRightWheelCollider)
        {
            Debug.LogError("One or more Wheel Colliders are not assigned!", this);
            enabled = false;
        }

        _originalMaxMotorTorque = maxMotorTorque;
        _originalMaxSpeed = maxSpeed;
        _originalAccelerationRate = accelerationRate;

        if (Object.FindFirstObjectByType<CarShopManager>() != null)
        {
            _carData = System.Array.Find(Object.FindFirstObjectByType<CarShopManager>().allCars, car => car.name == this.gameObject.name);
        }
        if (Object.FindFirstObjectByType<UpgradeManager>() != null)
        {
            if (_carData != null) _carUpgrades = System.Array.Find(Object.FindFirstObjectByType<UpgradeManager>().allCarUpgrades, upg => upg.carName == _carData.carName);
        }

        ApplyAllUpgrades();

        // Initialize the boost effects
        InitializeBoostEffects();
    }

    void Update()
    {
        HandleCameraFOV();
        HandleBoostEffectsVisuals();
    }

    void FixedUpdate()
    {
        if (rb.isKinematic) return;



        float currentSpeedMPS = rb.linearVelocity.magnitude;
        _currentSpeedKPH = currentSpeedMPS * 3.6f;

        if (speedoText != null)
        {
            // Update the text, formatting to a whole number
            speedoText.text = _currentSpeedKPH.ToString("F0") + " km/h";
        }

        float steerInput = (variableJoystick != null) ? variableJoystick.Horizontal : 0f;
        float currentSteerAngle = steerInput * maxSteerAngle;
        float targetTorque = 0f;
        float currentAccelerationRate = _isSpeedBoostActive ? accelerationRate : _originalAccelerationRate;
        if (_gasPressed && !_brakePressed)
        {
            float currentMaxMotorTorque = _isSpeedBoostActive ? maxMotorTorque : _originalMaxMotorTorque;
            targetTorque = currentMaxMotorTorque;
            _currentAppliedTorque = Mathf.MoveTowards(_currentAppliedTorque, targetTorque, currentAccelerationRate * Time.fixedDeltaTime);
        }
        else if (!_gasPressed && !_brakePressed)
        {
            targetTorque = 0f;
            _currentAppliedTorque = Mathf.MoveTowards(_currentAppliedTorque, targetTorque, coastingDecelerationRate * Time.fixedDeltaTime);
        }
        else
        {
            _currentAppliedTorque = 0f;
        }
        float currentMaxSpeed = _isSpeedBoostActive ? maxSpeed : _originalMaxSpeed;
        float speedFactor = Mathf.Clamp01(currentSpeedMPS / currentMaxSpeed);
        float speedLimitedTorque = Mathf.Lerp(_currentAppliedTorque, 0, speedFactor * speedFactor);
        if (ForwardDirection)
        {
            rearLeftWheelCollider.motorTorque = speedLimitedTorque;
            rearRightWheelCollider.motorTorque = speedLimitedTorque;
        }
        else
        {
            rearLeftWheelCollider.motorTorque = -speedLimitedTorque;
            rearRightWheelCollider.motorTorque = -speedLimitedTorque;
        }
        float currentBrakeTorque = _brakePressed ? maxBrakeTorque : 0f;
        frontLeftWheelCollider.brakeTorque = currentBrakeTorque;
        frontRightWheelCollider.brakeTorque = currentBrakeTorque;
        rearLeftWheelCollider.brakeTorque = currentBrakeTorque;
        rearRightWheelCollider.brakeTorque = currentBrakeTorque;
        frontLeftWheelCollider.steerAngle = currentSteerAngle;
        frontRightWheelCollider.steerAngle = currentSteerAngle;
    }

    private void HandleCameraFOV()
    {
        if (playerVirtualCamera == null) return;
        float speedPercentage = rb.linearVelocity.magnitude / maxSpeed;
        float targetFOV = Mathf.Lerp(_baseFOV, maxFOV, speedPercentage);
        playerVirtualCamera.Lens.FieldOfView = Mathf.Lerp(playerVirtualCamera.Lens.FieldOfView, targetFOV, Time.deltaTime * fovChangeSpeed);
    }

    public void GasPressedDown() { _gasPressed = true; }
    public void GasReleased() { _gasPressed = false; }
    public void BrakePressedDown()
    {
        _brakePressed = true;
        _currentAppliedTorque = 0f;
        rearLeftWheelCollider.motorTorque = 0f;
        rearRightWheelCollider.motorTorque = 0f;
    }
    public void BrakeReleased() { _brakePressed = false; }

    public void IsNOTKinematic()
    {
        if (rb != null && rb.isKinematic)
        {
            rb.isKinematic = false;
        }
    }

    

    public void ResetCarState()
    {
        if (rb != null)
        {
            rb.isKinematic = true;
            rb.linearVelocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }
    }

    // --- SPEED BOOST LOGIC ---
    public void ActivateSpeedBoost(float torqueMultiplier, float speedMultiplier, float accelerationMultiplier, float duration)
    {
        if (_isSpeedBoostActive && _speedBoostCoroutine != null)
        {
            StopCoroutine(_speedBoostCoroutine);
        }

        if (!_isSpeedBoostActive)
        {
            _originalMaxMotorTorque = this.maxMotorTorque;
            _originalMaxSpeed = this.maxSpeed;
            _originalAccelerationRate = this.accelerationRate;
        }

        _isSpeedBoostActive = true;
        this.maxMotorTorque = _originalMaxMotorTorque * torqueMultiplier;
        this.maxSpeed = _originalMaxSpeed * speedMultiplier;
        this.accelerationRate = _originalAccelerationRate * accelerationMultiplier;

        if (_speedLines != null) _speedLines.Play();
        if (_boostAudioSource != null) _boostAudioSource.Play();
        if (_boostIndicatorLight != null) _boostIndicatorLight.enabled = true;

        _speedBoostCoroutine = StartCoroutine(SpeedBoostDurationCoroutine(duration));
    }

    private IEnumerator SpeedBoostDurationCoroutine(float duration)
    {
        yield return new WaitForSeconds(duration);
        DeactivateSpeedBoost();
    }

    private void DeactivateSpeedBoost()
    {
        if (_isSpeedBoostActive)
        {
            maxMotorTorque = _originalMaxMotorTorque;
            maxSpeed = _originalMaxSpeed;
            accelerationRate = _originalAccelerationRate;
            _isSpeedBoostActive = false;
            _speedBoostCoroutine = null;

            if (_speedLines != null) _speedLines.Stop();
            if (_boostAudioSource != null) _boostAudioSource.Stop();
            if (_boostIndicatorLight != null) _boostIndicatorLight.enabled = false;
        }
    }


    // --- Collision Handling ---
    void OnCollisionEnter(Collision collision)
    {
        if (Time.time < lastCrashTime + crashCooldown) return;
        if (((1 << collision.gameObject.layer) & crashLayers) == 0) return;

        float impactSpeed = collision.relativeVelocity.magnitude;

        if (impactSpeed >= crashSpeedThreshold)
        {
            lastCrashTime = Time.time;
            if (_impulseSource != null) _impulseSource.GenerateImpulse();
            HandleCrash(collision);
        }
    }

    // --- Boost Effects Setup and Handling ---
    private void InitializeBoostEffects()
    {
        // Setup Post-Processing
        Camera mainCam = Camera.main;
        if (mainCam != null)
        {
            _postProcessVolume = mainCam.GetComponent<Volume>();
            if (_postProcessVolume == null) _postProcessVolume = mainCam.gameObject.AddComponent<Volume>();
            _postProcessVolume.isGlobal = true;

            VolumeProfile profile = _postProcessVolume.profile;
            if (profile == null)
            {
                profile = ScriptableObject.CreateInstance<VolumeProfile>();
                _postProcessVolume.profile = profile;
            }

            if (!profile.TryGet(out _chromaticAberration)) _chromaticAberration = profile.Add<ChromaticAberration>(true);
            if (!profile.TryGet(out _lensDistortion)) _lensDistortion = profile.Add<LensDistortion>(true);

            _chromaticAberration.intensity.value = 0f;
            _lensDistortion.intensity.value = 0f;
        }

        // Setup Speed Lines Particle System
        if (mainCam != null)
        {
            GameObject speedLinesGO = new GameObject("SpeedLinesEffect");
            speedLinesGO.transform.SetParent(mainCam.transform);
            speedLinesGO.transform.localPosition = new Vector3(0, 0, 1);
            speedLinesGO.transform.localRotation = Quaternion.identity;

            _speedLines = speedLinesGO.AddComponent<ParticleSystem>();
            var main = _speedLines.main;
            main.startLifetime = 0.5f;
            main.startSpeed = -50f;
            main.startSize = new ParticleSystem.MinMaxCurve(0.01f, 0.05f);
            main.maxParticles = 200;
            main.loop = true;
            main.playOnAwake = false;

            var emission = _speedLines.emission;
            emission.rateOverTime = 150;

            var shape = _speedLines.shape;
            shape.shapeType = ParticleSystemShapeType.Cone;
            shape.angle = 25;
            shape.radius = 5;

            var renderer = _speedLines.GetComponent<ParticleSystemRenderer>();
            renderer.renderMode = ParticleSystemRenderMode.Stretch;
            renderer.lengthScale = 2.5f;

            _speedLines.Stop();
        }

        // Setup Boost Audio Source
        _boostAudioSource = gameObject.AddComponent<AudioSource>();
        _boostAudioSource.loop = true;
        _boostAudioSource.playOnAwake = false;
        // ** MODIFIED: Assign the clip from the Inspector **
        if (boostSound != null)
        {
            _boostAudioSource.clip = boostSound;
        }
        else
        {
            Debug.LogWarning("No Boost Sound clip assigned in the Inspector for this car.", this);
        }

        // Setup UI Indicator Light
        Canvas canvas = FindFirstObjectByType<Canvas>();
        if (canvas != null)
        {
            GameObject indicatorGO = new GameObject("BoostIndicatorLight");
            indicatorGO.transform.SetParent(canvas.transform, false);
            _boostIndicatorLight = indicatorGO.AddComponent<Image>();
            _boostIndicatorLight.color = new Color(0.2f, 0.8f, 1f, 0.9f);

            RectTransform rect = indicatorGO.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0.5f, 0);
            rect.anchorMax = new Vector2(0.5f, 0);
            rect.pivot = new Vector2(0.5f, 0);
            rect.anchoredPosition = new Vector2(0, 50);
            rect.sizeDelta = new Vector2(100, 20);

            _boostIndicatorLight.enabled = false;
        }
    }

    private void HandleBoostEffectsVisuals()
    {
        if (_chromaticAberration == null || _lensDistortion == null) return;

        float targetIntensity = _isSpeedBoostActive ? 1.0f : 0.0f;
        _boostEffectIntensity = Mathf.Lerp(_boostEffectIntensity, targetIntensity, Time.deltaTime * 5f);

        _chromaticAberration.intensity.value = _boostEffectIntensity;
        _lensDistortion.intensity.value = -_boostEffectIntensity * 0.4f;
    }

    // --- Other Methods (Unchanged) ---
    void HandleCrash(Collision collisionInfo)
    {
        if (effectsPrefab) effectsPrefab.SetActive(true);
        Invoke("handlecrash2", 1f);
    }
    public void handlecrash2()
    {
        if (controlsUI) controlsUI.SetActive(false);
        if (mainMeniuCamera) mainMeniuCamera.SetActive(true);
        if (driversCamera) driversCamera.SetActive(false);
        Invoke("changeToMeniu", 2f);
    }
    private void changeToMeniu()
    {
        if (GameOverScreen) GameOverScreen.SetActive(true);
        rb.isKinematic = true;
    }
    public void ApplyAllUpgrades()
    {
        if (GameDataManager.Instance == null || _carData == null || _carUpgrades == null)
        {
            Debug.LogWarning($"Cannot apply upgrades for {gameObject.name} - managers or data not found.");
            return;
        }

        float finalMotorTorque = _originalMaxMotorTorque;
        float finalMaxSpeed = _originalMaxSpeed;
        float finalAccelerationRate = _originalAccelerationRate;
        float finalBrakeTorque = maxBrakeTorque;

        int engineLevel = GameDataManager.Instance.GetUpgradeLevel(_carData.carName, "Engine");
        for (int i = 0; i < engineLevel; i++)
        {
            if (i < _carUpgrades.engineUpgrades.Count)
            {
                finalMotorTorque += _carUpgrades.engineUpgrades[i].motorTorque_Increase;
                finalMaxSpeed += _carUpgrades.engineUpgrades[i].maxSpeed_Increase;
            }
        }

        int turboLevel = GameDataManager.Instance.GetUpgradeLevel(_carData.carName, "Turbo");
        for (int i = 0; i < turboLevel; i++)
        {
            if (i < _carUpgrades.turboUpgrades.Count)
            {
                finalAccelerationRate += _carUpgrades.turboUpgrades[i].accelerationRate_Increase;
            }
        }

        int brakeLevel = GameDataManager.Instance.GetUpgradeLevel(_carData.carName, "Brakes");
        for (int i = 0; i < brakeLevel; i++)
        {
            if (i < _carUpgrades.brakeUpgrades.Count)
            {
                finalBrakeTorque += _carUpgrades.brakeUpgrades[i].brakeTorque_Increase;
            }
        }

        this.maxMotorTorque = finalMotorTorque;
        this.maxSpeed = finalMaxSpeed;
        this.accelerationRate = finalAccelerationRate;
        this.maxBrakeTorque = finalBrakeTorque;

        _originalMaxMotorTorque = finalMotorTorque;
        _originalMaxSpeed = finalMaxSpeed;
        _originalAccelerationRate = finalAccelerationRate;

        Debug.Log($"Upgrades applied for {_carData.carName}. New Top Speed: {maxSpeed}");
    }
}
