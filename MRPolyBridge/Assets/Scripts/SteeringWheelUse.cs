using System;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;

public class SteeringWheelUse : MonoBehaviour, IHandGrabUseDelegate
{
    public enum Gear { Park, Reverse, Drive }

    [Header("Car")]
    [SerializeField] private PrometeoCarController _carController;

    [Header("Wheel Pivot")]
    [SerializeField] private Transform _wheelPivot;  // the axis you turn

    [Header("Joystick Settings (forward/back)")]
    [SerializeField] private float maxTiltAngle = 30f;
    [SerializeField] private float deadZoneAngle = 5f;

    [Header("Events")]
    public UnityEvent OnUseStarted;              // fires when BeginUse is called
    public UnityEvent OnUseEnded;                // fires when EndUse is called
    public UnityEvent<float> OnForwardBackAxis;  // fires each frame with value [-1..1]

    [SerializeField] private HandGrabUseInteractable _useInteractable;
    [SerializeField] private HandGrabInteractable _grabInteractable;  // primary grab

    [Header("Gear (runtime / inspector)")]
    [SerializeField] private Gear _currentGear = Gear.Park;
    public Gear CurrentGear => _currentGear;
    public UnityEvent OnSetPark;
    public UnityEvent OnSetReverse;
    public UnityEvent OnSetDrive;

    // runtime state
    private bool _isUsing = false;
    private float _lastStrength = 0f;

    void Awake()
    {
        _useInteractable.InjectOptionalForwardUseDelegate(this);
    }

    void Update()
    {
        // 1) Steering: any time the wheel is grabbed
        bool grabbed = _grabInteractable != null && _grabInteractable.SelectingInteractors.Count > 0;
        float steer = 0f;
        if (grabbed)
        {
            float z = _wheelPivot.localEulerAngles.z;
            float signedZ = z > 180f ? z - 360f : z;
            if (Mathf.Abs(signedZ) >= deadZoneAngle)
                steer = signedZ / maxTiltAngle;
        }

        // 2) Throttle: only if pinching (“use”) is in progress
        float throttleRaw = _isUsing ? _lastStrength : 0f; // 0..1

        // 3) Convert throttle according to gear:
        float throttleSigned = 0f;
        switch (_currentGear)
        {
            case Gear.Park:
                throttleSigned = 0f;
                break;
            case Gear.Drive:
                throttleSigned = throttleRaw; // positive
                break;
            case Gear.Reverse:
                throttleSigned = -throttleRaw; // negative
                break;
        }

        // 4) Send combined input each frame
        // Note: HandleVRInput sets useVRInput = true inside the car controller.
        if (_carController != null)
        {
            _carController.HandleVRInput(new Vector2(steer, throttleSigned));
        }

        // 5) Optional per-frame callback for UI/haptics
        OnForwardBackAxis?.Invoke(throttleSigned);
    }

    void LateUpdate()
    {
        // Enforce wheel pivot clamp after transformers move the pivot
        float rawZ = _wheelPivot.localEulerAngles.z;
        float signedZ = Mathf.DeltaAngle(0f, rawZ);
        float clampedZ = Mathf.Clamp(signedZ, -maxTiltAngle, maxTiltAngle);

        if (!Mathf.Approximately(signedZ, clampedZ))
        {
            _wheelPivot.localRotation = Quaternion.Euler(0f, 0f, clampedZ);
        }
    }

    // IHandGrabUseDelegate ↓↓↓
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
        // return unmodified so UseProgress tracks your real pinch
        return strength;
    }

    /// <summary>
    /// Public gear API - wire these into your lever's UnityEvents
    /// e.g. lever.onSnapToPark -> SetPark()
    /// </summary>
    public void SetPark()
    {
        _currentGear = Gear.Park;
        Debug.Log("[SteeringWheelUse] Gear -> PARK");
        // Force throttle off and apply handbrake so vehicle stays put
        if (_carController != null)
        {
            _carController.ThrottleOff();
            _carController.Handbrake();       // locks traction & emits skid state
        }
        // clear any use so the wheel won't send throttle
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
            // release handbrake so car can move
            _carController.RecoverTraction();
            _carController.ThrottleOff(); // zero throttle on gear change
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
    /// Call this any time you completely reset the car.
    /// It will zero the wheel pivot and clear any use state.
    /// </summary>
    public void ResetSteeringWheel()
    {
        if (_wheelPivot != null)
            _wheelPivot.localRotation = Quaternion.identity;

        _isUsing = false;
        _lastStrength = 0f;

        Debug.Log("[SteeringWheelUse] Wheel has been reset to center.");
    }

    // Helper: useful for debugging or UI
    public string GetCurrentGearName()
    {
        return _currentGear.ToString();
    }
}
