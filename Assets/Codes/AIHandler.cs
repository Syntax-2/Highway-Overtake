using UnityEngine;
using System.Collections; // Required for Coroutines if used later

[RequireComponent(typeof(Rigidbody))]
public class LaneSwitchingAICar : MonoBehaviour
{
    [Header("Movement")]
    [Tooltip("Minimum speed assigned at start.")]
    public float minSpeed = 8f;
    [Tooltip("Maximum speed assigned at start.")]
    public float maxSpeed = 15f;
    [Tooltip("How strongly the car corrects its position towards the target lane X. Higher = faster correction.")]
    public float correctionStrength = 5f;
    [Tooltip("Maximum sideways speed used for correction/lane changing.")]
    public float maxCorrectionSpeed = 3f;
    [Tooltip("How quickly the car changes its target X position when switching lanes.")]
    public float laneChangeSharpness = 2.0f; // Higher = faster transition

    [Header("Lane Definitions")]
    [Tooltip("X coordinate for the center of the right lane.")]
    public float rightLaneX = 0f;
    [Tooltip("X coordinate for the center of the left lane.")]
    public float leftLaneX = 3f; // Adjust if your left lane is -3

    [Header("Detection & Overtaking")]
    [Tooltip("How far ahead the car looks for obstacles in its current lane.")]
    public float detectionDistance = 25f;
    [Tooltip("How far ahead the car checks if the *other* lane is clear before starting an overtake.")]
    public float overtakeClearanceCheckDistance = 40f; // Needs to be longer than detectionDistance
    // [Tooltip("The minimum distance to keep behind another car before initiating overtake.")]
    // public float followDistance = 5f; // Note: Not currently used actively
    [Tooltip("Time in seconds the car stays in the other lane after overtaking before trying to return.")]
    public float overtakeHoldTime = 1.5f;
    [Tooltip("The layer mask containing cars (AI and Player).")]
    public LayerMask carLayerMask; // IMPORTANT: Set this in the inspector!

    [Tooltip("How close (world units) a detected car's X position needs to be to the target lane's center X to be considered 'in the lane'.")]
    public float laneCheckTolerance = 1.2f; // Default value, adjust in Inspector


    [Header("Debugging")]
    public bool showGizmos = true;
    public bool detailedLogs = true; // Toggle for logging

    [Tooltip("Adjusts the vertical position (Y-offset) where detection gizmos are drawn in the Scene view.")]
    public float gizmoVerticalOffset = 0.2f; // Default value, adjust in Inspector

    [Tooltip("Adjusts the horizontal position (X-offset, relative to target lane center) where the lane clearance check gizmo is drawn.")]
    public float gizmoHorizontalOffset = 0.0f; // Default value (centered on lane)

    // Private state variables
    private Rigidbody rb;
    private float _currentSpeed;
    private float _targetLaneX; // The X coordinate the car ultimately wants to be in
    private float _currentMovingTargetX; // The X coordinate used for immediate steering (smooth transition)
    private float _originalLaneX; // To remember where to return to
    private CarState _currentState = CarState.DrivingStraight;
    private float _overtakeTimer = 0f;
    private Transform _carToOvertake = null; // Reference to the car being overtaken
    private Vector3 _boxCastHalfExtents = new Vector3(0.5f, 0.5f, 0.1f); // Width, Height, tiny Depth
    private float DetectedCarSpeed;

    private bool isInTargetLane;        

    private enum CarState { DrivingStraight, NeedsToOvertake, ChangingToOvertakeLane, Overtaking, CheckingToReturn, ReturningToOriginalLane }

    public AudioSource hornsfx;

    void Start()
    {
        // ... (Start function remains mostly the same) ...
        rb = GetComponent<Rigidbody>();
        rb.linearDamping = 0.5f;
        rb.angularDamping = 1.0f;
        rb.constraints = RigidbodyConstraints.FreezeRotationX | RigidbodyConstraints.FreezeRotationZ;
        _currentSpeed = Random.Range(minSpeed, maxSpeed);
        _targetLaneX = rightLaneX;
        _originalLaneX = rightLaneX;
        _currentMovingTargetX = _targetLaneX;
        transform.rotation = Quaternion.LookRotation(Vector3.back);
        if (carLayerMask == 0) { Debug.LogWarning($"CarLayerMask is not set on {gameObject.name}.", this); }

        // Ensure tolerance is reasonable
        laneCheckTolerance = Mathf.Max(0.1f, laneCheckTolerance);
    }

    void FixedUpdate()
    {
        // State Machine Logic
        switch (_currentState)
        {
            case CarState.DrivingStraight: UpdateDrivingStraight(); break;
            case CarState.NeedsToOvertake: UpdateNeedsToOvertake(); break;
            case CarState.ChangingToOvertakeLane: UpdateChangingLane(); break;
            case CarState.Overtaking: UpdateOvertaking(); break;
            case CarState.CheckingToReturn: UpdateCheckingToReturn(); break;
            case CarState.ReturningToOriginalLane: UpdateReturningLane(); break;
        }
        ApplyMovement();
        ForceForwardRotation();
    }

    // --- State Update Functions ---

    void UpdateDrivingStraight()
    {
        _currentMovingTargetX = Mathf.MoveTowards(_currentMovingTargetX, _targetLaneX, laneChangeSharpness * Time.fixedDeltaTime);
        Transform detectedCarTransform = CheckAhead(detectionDistance);

        if (detailedLogs && detectedCarTransform != null)
        {
            //Debug.Log($"[{Time.time:F1}] {name}: Detected '{detectedCarTransform.name}' ahead.");
        }

        if (detectedCarTransform != null)
        {
            Rigidbody detectedRb = detectedCarTransform.GetComponent<Rigidbody>();
            if (detectedRb != null)
            {
                float detectedVelocityZ = detectedRb.linearVelocity.z;
                DetectedCarSpeed = detectedVelocityZ;
                float myTargetVelocityZ = -this._currentSpeed;
                bool isDetectedCarSlower = detectedVelocityZ > myTargetVelocityZ;

                // --- Use the IsInLane check (which now uses the public variable) ---
                isInTargetLane = IsInLane(detectedCarTransform.position, _targetLaneX);
                // ---

                if (detailedLogs)
                {
                    // --- Updated Log to show tolerance used ---
                    //Debug.Log($"[{Time.time:F1}] {name}: Checking '{detectedCarTransform.name}': " +
                    //          $"DetectedVelZ={detectedVelocityZ:F2}, MyTargetVelZ={myTargetVelocityZ:F2}, IsSlower={isDetectedCarSlower}, " +
                    //          $"DetectedPosX={detectedCarTransform.position.x:F2}, TargetLaneX={_targetLaneX:F1}, Tolerance={laneCheckTolerance:F1}, IsInLane={isInTargetLane}"); // Added Tolerance
                                                                                                                                                                                  // ---
                }

                if (isDetectedCarSlower && isInTargetLane)
                {
                    if (detailedLogs) //Debug.Log($"[{Time.time:F1}] {name} >>> Overtake Triggered (Slower & In Lane): {detectedCarTransform.name} <<<", this);
                    _carToOvertake = detectedCarTransform;
                    _currentState = CarState.NeedsToOvertake;
                }
                else
                {
                    if (detailedLogs)
                    {
                        string reason = "";
                        if (!isDetectedCarSlower) reason += "Detected car not slower. ";
                        // --- Updated Log reason for lane check ---
                        if (!isInTargetLane) reason += $"Detected car not within tolerance ({laneCheckTolerance:F1}) of target lane X ({_targetLaneX:F1}). ";
                        // ---
                       // Debug.Log($"[{Time.time:F1}] {name} >>> Overtake NOT Triggered: {detectedCarTransform.name}. Reason: {reason} <<<");
                    }
                    if (_carToOvertake == detectedCarTransform) _carToOvertake = null;

                    if (!isInTargetLane)
                    {
                        _currentSpeed = -(DetectedCarSpeed);
                    }
                }
            }
            else
            {
                if (detailedLogs) //Debug.LogWarning($"[{Time.time:F1}] {name}: Detected '{detectedCarTransform.name}' but it has no Rigidbody component!", detectedCarTransform);
                if (_carToOvertake == detectedCarTransform) _carToOvertake = null;
            }
        }
        else
        {
            if (_carToOvertake != null) { _carToOvertake = null; }
        }
    }

    // ... (Rest of the functions: UpdateNeedsToOvertake, UpdateChangingLane, etc. are unchanged) ...
    void UpdateNeedsToOvertake()
    {
        float overtakeLaneX = (_targetLaneX == rightLaneX) ? leftLaneX : rightLaneX;
        if (CheckLaneClear(overtakeLaneX, overtakeClearanceCheckDistance))
        { // Returns TRUE if BLOCKED
            if (detailedLogs) //Debug.Log($"[{Time.time:F1}] {name} wants to overtake {_carToOvertake?.name}, but lane X={overtakeLaneX} is blocked. Waiting.", this);
            _currentState = CarState.DrivingStraight;
            _currentSpeed = -(DetectedCarSpeed);
            return;
        }
        if (detailedLogs) //Debug.Log($"[{Time.time:F1}] {name} initiating overtake of {_carToOvertake?.name}. Switching to lane X={overtakeLaneX}.", this);
        _targetLaneX = overtakeLaneX; _currentState = CarState.ChangingToOvertakeLane;
    }
    void UpdateChangingLane()
    {
        _currentMovingTargetX = Mathf.MoveTowards(_currentMovingTargetX, _targetLaneX, laneChangeSharpness * Time.fixedDeltaTime);
        if (Mathf.Abs(_currentMovingTargetX - _targetLaneX) < 0.1f)
        {
            _currentMovingTargetX = _targetLaneX;
            if (_targetLaneX != _originalLaneX)
            { // Changing to overtake
                hornsfx.Play();
                if (detailedLogs) //Debug.Log($"[{Time.time:F1}] {name} completed change to overtake lane X={_targetLaneX}. Now overtaking.", this);
                _currentState = CarState.Overtaking; _overtakeTimer = 0f;
            }
            else
            { // Changing back to original
                if (detailedLogs) //Debug.Log($"[{Time.time:F1}] {name} completed return to original lane X={_targetLaneX}.", this);
                _currentState = CarState.DrivingStraight; _carToOvertake = null;
            }
        }
    }
    void UpdateOvertaking()
    {
        _currentMovingTargetX = _targetLaneX; _overtakeTimer += Time.fixedDeltaTime;
        bool passedTarget = false;
        if (_carToOvertake != null)
        {
            float behindBuffer = 2.0f;
            if (_carToOvertake.position.z > this.transform.position.z + behindBuffer) { passedTarget = true; }
        }
        else { passedTarget = _overtakeTimer >= overtakeHoldTime; }
        if (passedTarget || _overtakeTimer >= overtakeHoldTime * 1.5f)
        {
            if (detailedLogs) //Debug.Log($"[{Time.time:F1}] {name} finished overtaking (Passed={passedTarget}, Timer={_overtakeTimer:F1}). Checking to return to lane X={_originalLaneX}.", this);
            _currentState = CarState.CheckingToReturn;
        }
    }
    void UpdateCheckingToReturn()
    {
        if (CheckLaneClear(_originalLaneX, detectionDistance - 5f))
        { // Returns TRUE if BLOCKED
            if (detailedLogs) //Debug.Log($"[{Time.time:F1}] {name} wants to return to lane X={_originalLaneX}, but it's blocked ahead. Staying.", this);
            _currentState = CarState.Overtaking; _overtakeTimer = 0f; return;
        }
        if (detailedLogs) //Debug.Log($"[{Time.time:F1}] {name} returning to original lane X={_originalLaneX}.", this);
        _targetLaneX = _originalLaneX; _currentState = CarState.ReturningToOriginalLane;
    }
    void UpdateReturningLane() { UpdateChangingLane(); }
    void ApplyMovement()
    {
        Vector3 currentPosition = rb.position; Vector3 currentVelocity = rb.linearVelocity; // Use velocity
        float xError = currentPosition.x - _currentMovingTargetX;
        float correctionVelocityX = -xError * correctionStrength;
        correctionVelocityX = Mathf.Clamp(correctionVelocityX, -maxCorrectionSpeed, maxCorrectionSpeed);
        float targetVelocityZ = -_currentSpeed;
        rb.linearVelocity = new Vector3(correctionVelocityX, currentVelocity.y, targetVelocityZ); // Use velocity
    }
    void ForceForwardRotation()
    {
        Quaternion targetRotation = Quaternion.LookRotation(Vector3.back);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, targetRotation, Time.fixedDeltaTime * 10f));
    }
    Transform CheckAhead(float distance)
    {
        Vector3 boxOrigin = transform.position + transform.forward * (_boxCastHalfExtents.z + 1f) + transform.up * 0.1f; // Note: Increased forward offset here? Was 0.1f before.

        // --- Create a temporary wider size vector ---
        Vector3 checkAheadHalfExtents = _boxCastHalfExtents; // Start with the default size
        checkAheadHalfExtents.x = 10f; // Set the desired half-width (e.g., 1.5f)
        // You could also multiply: checkAheadHalfExtents.x *= 1.5f; // Make it 50% wider than default
        // ---

        RaycastHit hit;
        // --- Use the temporary wider size in this specific BoxCast ---
        if (Physics.BoxCast(boxOrigin, checkAheadHalfExtents, transform.forward, out hit, transform.rotation, distance, carLayerMask))
        {
            if (hit.collider.attachedRigidbody != rb) return hit.transform;
        }
        return null;
    }
    bool CheckLaneClear(float laneX, float distance)
    {
        Vector3 checkOriginBase = transform.position + transform.forward * (_boxCastHalfExtents.z + 0.2f) + transform.up * 0.1f;
        Vector3 checkOrigin = new Vector3(laneX, checkOriginBase.y, checkOriginBase.z);
        RaycastHit hit;

        bool didHit = Physics.BoxCast(checkOrigin, _boxCastHalfExtents, transform.forward, out hit, transform.rotation, distance, carLayerMask);

        if (didHit && hit.collider.attachedRigidbody != rb)
        {
            // --- ADDED LOG ---
            if (detailedLogs) //Debug.LogWarning($"[{Time.time:F1}] {name}: CheckLaneClear BLOCKED lane X={laneX:F1}. Hit '{hit.collider.name}' (Layer: {LayerMask.LayerToName(hit.collider.gameObject.layer)}, Tag: {hit.collider.tag}) at distance {hit.distance:F1}", hit.collider.gameObject);
            // --- END LOG ---
            return true; // Blocked
        }

        // --- Optional log for clarity ---
        // if(detailedLogs && !didHit) Debug.Log($"[{Time.time:F1}] {name}: CheckLaneClear found lane X={laneX:F1} CLEAR.");
        // ---

        return false; // Clear
    }
    bool IsInLane(Vector3 position, float laneXCenter)
    {
        // Use the public variable directly
        // float tolerance = 1.0f; // Old hardcoded value
        return Mathf.Abs(position.x - laneXCenter) < laneCheckTolerance;
    }


    void OnDrawGizmos()
    {
        if (!showGizmos || !enabled || rb == null) return;
        Vector3 forwardDir = Application.isPlaying ? transform.forward : Vector3.back;

        // --- Target Position Marker ---
        Gizmos.color = Color.cyan;
        Vector3 currentTargetWorldPos = new Vector3(_currentMovingTargetX, transform.position.y, transform.position.z);
        Gizmos.DrawWireSphere(currentTargetWorldPos + transform.up * gizmoVerticalOffset, 0.5f);
        Gizmos.DrawLine(transform.position + transform.up * gizmoVerticalOffset, currentTargetWorldPos + transform.up * gizmoVerticalOffset);

        // --- Forward Detection Box ---
        Vector3 boxOrigin = transform.position + forwardDir * (_boxCastHalfExtents.z + 0.1f) + transform.up * gizmoVerticalOffset;
        Color detectionColor = Color.yellow;
#if UNITY_EDITOR
        if (Application.isPlaying) { Transform detected = CheckAhead(detectionDistance); if (detected != null) { detectionColor = Color.red; } }
#endif
        // Draw forward gizmo (uses its own origin calculation)
        DrawWireCubeGizmo(boxOrigin, forwardDir, detectionDistance, _boxCastHalfExtents * 2, transform.rotation, detectionColor);


        // --- Overtake Lane Check Visualization ---
        if (Application.isPlaying && (_currentState == CarState.NeedsToOvertake || _currentState == CarState.CheckingToReturn))
        {
            float checkLaneX = 0; float checkDistance = 0; bool isChecking = false;
            if (_currentState == CarState.NeedsToOvertake && _targetLaneX == _originalLaneX)
            {
                checkLaneX = (_originalLaneX == rightLaneX) ? leftLaneX : rightLaneX;
                checkDistance = overtakeClearanceCheckDistance; isChecking = true;
            }
            else if (_currentState == CarState.CheckingToReturn && _targetLaneX != _originalLaneX)
            {
                checkLaneX = _originalLaneX; checkDistance = detectionDistance + 5f; isChecking = true;
            }

            if (isChecking)
            {
                // 1. Calculate the ACTUAL physics check origin (no visual offsets yet)
                Vector3 actualCheckOriginBase = transform.position + forwardDir * (_boxCastHalfExtents.z + 0.5f) + transform.up * gizmoVerticalOffset; // Base Y/Z pos using vertical offset
                Vector3 actualCheckOrigin = new Vector3(checkLaneX, actualCheckOriginBase.y, actualCheckOriginBase.z); // Center on the target lane X

                // 2. Calculate the CENTER of the ACTUAL check path
                Vector3 actualGizmoCenter = actualCheckOrigin + forwardDir * (checkDistance / 2.0f);

                // --- 3. Apply VISUAL Horizontal Offset Directly to the Center X ---
                Vector3 visualGizmoCenter = actualGizmoCenter;
                visualGizmoCenter.x += gizmoHorizontalOffset; // Add offset here
                                                              // ---

                // Debugging Log (Optional)
                // if (detailedLogs) Debug.Log($"Drawing LaneCheck Gizmo: ActualCenter.X={actualGizmoCenter.x:F2}, VisualCenter.X={visualGizmoCenter.x:F2} (Offset={gizmoHorizontalOffset:F2})");

                Color clearanceColor = Color.green;
#if UNITY_EDITOR
                // Base color on the ACTUAL check results
                if (CheckLaneClear(checkLaneX, checkDistance)) { clearanceColor = Color.magenta; }
#endif

                // --- 4. Draw the gizmo using the VISUALLY offset center ---
                DrawWireCubeGizmoFromCenter(visualGizmoCenter, forwardDir, checkDistance, _boxCastHalfExtents * 2, transform.rotation, clearanceColor);
            }
        }
    }

    // Modified Gizmo Drawing Function - Takes CENTER instead of ORIGIN
    void DrawWireCubeGizmoFromCenter(Vector3 center, Vector3 direction, float distance, Vector3 size, Quaternion orientation, Color color)
    {
        // Note: 'direction' and 'distance' aren't strictly needed if 'center' is already calculated,
        // but we keep them for potential future use or clarity. Size and orientation are key.
        Gizmos.color = color;
        // Center is already calculated and passed in
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(center, orientation, size); // Use center directly
        Gizmos.matrix = rotationMatrix;
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one);
        Gizmos.matrix = Matrix4x4.identity;
    }


    void DrawWireCubeGizmo(Vector3 origin, Vector3 direction, float distance, Vector3 size, Quaternion orientation, Color color)
    {
        Gizmos.color = color;
        Vector3 center = origin + direction * (distance / 2.0f); // Calculate center of the cast path
        Matrix4x4 rotationMatrix = Matrix4x4.TRS(center, orientation, size); // Create matrix for position, rotation, and scaling to size
        Gizmos.matrix = rotationMatrix; // Apply the matrix
        Gizmos.DrawWireCube(Vector3.zero, Vector3.one); // Draw a unit cube; the matrix scales and positions it
        Gizmos.matrix = Matrix4x4.identity; // Reset the Gizmos matrix to avoid affecting other gizmos
    }
}