using System;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;

/// <summary>
/// SteeringWheelUse
/// - Accumulates rotational deltas to avoid Euler wrap flips when localEulerAngles.z crosses 180/360.
/// - Exposes gear API: SetPark / SetReverse / SetDrive for use by a lever's UnityEvents.
/// - Sends steering + throttle into PrometeoCarController.HandleVRInput(Vector2 steer, throttle).
/// - Optional debug logging for tuning/testing.
/// </summary>
public class SteeringWheelUse : MonoBehaviour, IHandGrabUseDelegate
{
    public enum Gear { Park, Reverse, Drive }

    [Header("Car")]
    [SerializeField] public PrometeoCarController _carController;

    [Header("Wheel Pivot")]
    [SerializeField] private Transform _wheelPivot;  // the axis you turn

    [Header("Joystick Settings (forward/back)")]
    [Tooltip("Maximum tilt applied to the wheel visual and input (degrees). Can be >180 but be mindful of UX).")]
    [SerializeField] private float maxTiltAngle = 30f;
    [Tooltip("Ignore small rotations inside this dead-zone (degrees).")]
    [SerializeField] private float deadZoneAngle = 5f;

    [Header("Interactables")]
    [SerializeField] private HandGrabUseInteractable _useInteractable;
    [SerializeField] private HandGrabInteractable _grabInteractable;  // primary grab

    [Header("Events")]
    public UnityEvent OnUseStarted;
    public UnityEvent OnUseEnded;
    public UnityEvent<float> OnForwardBackAxis;  // [-1..1]
    [Space(6)]
    public UnityEvent OnSetPark;
    public UnityEvent OnSetReverse;
    public UnityEvent OnSetDrive;

    [Header("Runtime Gear")]
    [SerializeField] private Gear _currentGear = Gear.Park;
    public Gear CurrentGear => _currentGear;

    [Header("Debug")]
    [Tooltip("Enable console logs for rawZ / delta / accumulatedAngle while testing.")]
    [SerializeField] private bool debugAngles = false;
    [Tooltip("How often (seconds) to print debug angle info when debugAngles is true.")]
    [SerializeField] private float debugInterval = 0.15f;

    // runtime state
    private bool _isUsing = false;
    private float _lastStrength = 0f;

    // accumulated-angle tracking to avoid Euler wrap jumps
    private float _accumulatedAngle = 0f;   // continuous signed angle used for steering
    private float _prevRawZ = 0f;          // last frame localEulerAngles.z (0..360)
    private bool _skipNextAccumulation = false;

    // debug timer
    private float _debugTimer = 0f;

    void Awake()
    {
        // optional injection (keeps your existing usage integration)
        try { _useInteractable?.InjectOptionalForwardUseDelegate(this); } catch { /* ignore if not set up */ }

        // initialize angle tracking from current transform if present
        if (_wheelPivot != null)
        {
            _prevRawZ = _wheelPivot.localEulerAngles.z;
            _accumulatedAngle = Mathf.DeltaAngle(0f, _prevRawZ); // signed initial
            _accumulatedAngle = Mathf.Clamp(_accumulatedAngle, -maxTiltAngle, maxTiltAngle);
            // apply clamped initial angle to visual pivot so everything is in sync
            _wheelPivot.localRotation = Quaternion.Euler(0f, 0f, _accumulatedAngle);
            // read back rawZ after we forced the transform (keeps prevRawZ correct)
            _prevRawZ = _wheelPivot.localEulerAngles.z;
        }
    }

    void Update()
    {
        // 1) Steering: only when the wheel is grabbed
        bool grabbed = _grabInteractable != null && _grabInteractable.SelectingInteractors.Count > 0;
        float steer = 0f;
        if (grabbed)
        {
            // Use accumulated signed angle (stable across 0/360 wrap)
            float signedZ = _accumulatedAngle;
            if (Mathf.Abs(signedZ) >= deadZoneAngle)
                steer = signedZ / maxTiltAngle; // normalized [-1..1]
        }

        // 2) Throttle: only while pinching ("use")
        float throttleRaw = _isUsing ? _lastStrength : 0f; // 0..1

        // 3) Convert throttle according to gear:
        float throttleSigned = 0f;
        switch (_currentGear)
        {
            case Gear.Park:
                throttleSigned = 0f;
                break;
            case Gear.Drive:
                throttleSigned = throttleRaw; // positive forward
                break;
            case Gear.Reverse:
                throttleSigned = -throttleRaw; // negative reverse
                break;
        }

        // 4) Send combined input to car controller
        if (_carController != null)
            _carController.HandleVRInput(new Vector2(steer, throttleSigned));

        // 5) Optional frame events
        OnForwardBackAxis?.Invoke(throttleSigned);

        // 6) Debug logging (throttled by debugInterval)
        if (debugAngles)
        {
            _debugTimer += Time.deltaTime;
            if (_debugTimer >= debugInterval)
            {
                _debugTimer = 0f;
                // compute current rawZ for logging (0..360)
                float rawZ = _wheelPivot != null ? _wheelPivot.localEulerAngles.z : 0f;
                // compute delta between prevRawZ and rawZ (this is what LateUpdate accumulates next)
                float delta = Mathf.DeltaAngle(_prevRawZ, rawZ);
                Debug.LogFormat("[SteeringWheelUse] rawZ={0:F2}°, delta={1:F3}°, accumulated={2:F3}° (gear={3})", rawZ, delta, _accumulatedAngle, _currentGear);
            }
        }
    }

    void LateUpdate()
    {
        // keep the pivot visually clamped and update accumulated angle safely using delta integration
        if (_wheelPivot == null) return;

        // read raw (0..360) local Z reported by transform
        float rawZ = _wheelPivot.localEulerAngles.z;
        // If we're skipping this frame (fresh reset/snap), simply sync prevRawZ and resume next frame
        if (_skipNextAccumulation)
        {
            _prevRawZ = rawZ;
            _skipNextAccumulation = false;
            // also ensure visuals match cleared accumulator
            _wheelPivot.localRotation = Quaternion.Euler(0f, 0f, _accumulatedAngle);
            return;
        }

        // compute signed shortest-difference from previous rawZ to this rawZ
        float delta = Mathf.DeltaAngle(_prevRawZ, rawZ);

        // accumulate and clamp to allowed range
        _accumulatedAngle += delta;
        _accumulatedAngle = Mathf.Clamp(_accumulatedAngle, -maxTiltAngle, maxTiltAngle);

        // write the clamped accumulated angle back to the transform (keeps visuals stable)
        _wheelPivot.localRotation = Quaternion.Euler(0f, 0f, _accumulatedAngle);

        // store new rawZ (read back from transform after we set it)
        _prevRawZ = _wheelPivot.localEulerAngles.z;
    }

    // IHandGrabUseDelegate implementation
    public void BeginUse()
    {
        _isUsing = true;
        OnUseStarted?.Invoke();
    }

    public void EndUse()
    {
        _isUsing = false;
        OnUseEnded?.Invoke();
    }

    public float ComputeUseStrength(float strength)
    {
        _lastStrength = strength;
        return strength; // don't alter use progress
    }

    // -----------------------
    // Gear API (wire these to your lever events)
    // -----------------------
    public void SetPark()
    {
        _currentGear = Gear.Park;
        Debug.Log("[SteeringWheelUse] Gear -> PARK");
        if (_carController != null)
        {
            _carController.ThrottleOff();
            // Handbrake locks traction and provides a strong "park" behavior; if you want a softer park, use Brakes() instead.
            _carController.Handbrake();
        }
        // clear any use state so we don't immediately throttle
        _isUsing = false;
        _lastStrength = 0f;
        OnSetPark?.Invoke();
    }

    public void SetDrive()
    {
        _currentGear = Gear.Drive;
        Debug.Log("[SteeringWheelUse] Gear -> DRIVE");
        if (_carController != null)
        {
            _carController.RecoverTraction();
            _carController.ThrottleOff();
        }
        OnSetDrive?.Invoke();
    }

    public void SetReverse()
    {
        _currentGear = Gear.Reverse;
        Debug.Log("[SteeringWheelUse] Gear -> REVERSE");
        if (_carController != null)
        {
            _carController.RecoverTraction();
            _carController.ThrottleOff();
        }
        OnSetReverse?.Invoke();
    }

    /// <summary>
    /// Sync the internal accumulator with the transform's current rotation.
    /// If preserveClosestToPrevious==true we choose the angle congruent to the transform that is
    /// closest to the current _accumulatedAngle to avoid 360-degree jumps.
    /// </summary>
    public void SyncAccumulatedAngleToTransform(bool preserveClosestToPrevious = true)
    {
        if (_wheelPivot == null) return;

        // rawZ is 0..360 reported by transform
        float rawZ = _wheelPivot.localEulerAngles.z;

        // baseSigned is in [-180,180], one congruent representative of the transform rotation
        float baseSigned = Mathf.DeltaAngle(0f, rawZ);

        if (preserveClosestToPrevious)
        {
            // choose the congruent representative closest to previous accumulated angle
            // k = round((prev - base) / 360)
            float k = Mathf.Round((_accumulatedAngle - baseSigned) / 360f);
            baseSigned += 360f * k;
        }

        // clamp to configured limits and apply to visuals
        _accumulatedAngle = Mathf.Clamp(baseSigned, -maxTiltAngle, maxTiltAngle);
        _wheelPivot.localRotation = Quaternion.Euler(0f, 0f, _accumulatedAngle);

        // read back rawZ after applying the rotation so _prevRawZ lines up with the transform representation
        _prevRawZ = _wheelPivot.localEulerAngles.z;

        Debug.LogFormat("[SteeringWheelUse] SyncAccumulatedAngleToTransform -> rawZ={0:F2}, accumulated={1:F2}", rawZ, _accumulatedAngle);
    }

    /// <summary>
    /// Reset the visual wheel to center and deterministically zero internal accumulators.
    /// Use this when you want a true reset (no preserved 360 offsets).
    /// </summary>
    /// 
    [ContextMenu("resetSteeringwheel")]
    public void ResetSteeringWheel()
    {
        if (_wheelPivot != null)
            _wheelPivot.localRotation = Quaternion.identity;

        _isUsing = false;
        _lastStrength = 0f;

        // deterministically zero the accumulator and set prevRawZ to the transform's reported value
        _accumulatedAngle = 0f;
        _prevRawZ = _wheelPivot != null ? _wheelPivot.localEulerAngles.z : 0f;
        _skipNextAccumulation = true;
        // Optionally apply the zero back to the transform (should already be identity but keep in sync)
        if (_wheelPivot != null)
            _wheelPivot.localRotation = Quaternion.Euler(0f, 0f, _accumulatedAngle);

        Debug.LogFormat("[SteeringWheelUse] Wheel has been reset to center (accumulator cleared). prevRawZ={0:F2}", _prevRawZ);
    }

    public void SetCarController(PrometeoCarController car)
    {
        _carController = car;
        // keep things in sync when wiring a new car
        SyncAccumulatedAngleToTransform(); // optional but helpful
        Debug.LogFormat("[SteeringWheelUse] Car assigned: {0}", car != null ? car.name : "null");
    }
    // Helper: friendly getter for UI
    public string GetCurrentGearName()
    {
        return _currentGear.ToString();
    }
    /// <summary>
    /// Returns true if no interactors (hands) are currently grabbing the wheel.
    /// Useful for resuming steering wheel position updates only when fully released.
    /// </summary>
    public bool NoInteractors()
    {
        return _grabInteractable == null || _grabInteractable.SelectingInteractors.Count == 0;
    }

}
