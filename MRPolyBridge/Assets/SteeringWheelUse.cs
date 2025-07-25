using System;
using Oculus.Interaction.HandGrab;
using UnityEngine;
using UnityEngine.Events;

public class SteeringWheelUse : MonoBehaviour, IHandGrabUseDelegate
{
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

    // runtime state
    private bool _isUsing = false;
    private float _lastStrength = 0f;

    void Awake()
    {
        // _useInteractable = GetComponent<HandGrabUseInteractable>();
        _useInteractable.InjectOptionalForwardUseDelegate(this);
    }
    void Update()
    {
        // 1) Steering: any time the wheel is grabbed
        bool grabbed = _grabInteractable.SelectingInteractors.Count > 0;
        float steer = 0f;
        // if (grabbed)
        // {
        //     // 1) Get signed angle between 0 and wheel’s Z rotation
        //     float signedZ = Mathf.DeltaAngle(0f, _wheelPivot.localEulerAngles.z);
        //     // 2) Clamp it
        //     float clampedZ = Mathf.Clamp(signedZ, -maxTiltAngle, maxTiltAngle);

        //     // 3) If it’s outside that limit, snap it back:
        //     if (!Mathf.Approximately(signedZ, clampedZ))
        //     {
        //         _wheelPivot.localRotation = Quaternion.Euler(0f, 0f, clampedZ);
        //     }

        //     // 3) Normalize into [-1..1], with a dead‑zone
        //     if (Mathf.Abs(clampedZ) >= deadZoneAngle)
        //         steer = clampedZ / maxTiltAngle;


        // }
        if (grabbed)
        {
            // At this point _wheelPivot.localEulerAngles.z is guaranteed clamped
            float z = _wheelPivot.localEulerAngles.z;
            float signedZ = z > 180f ? z - 360f : z;
            if (Mathf.Abs(signedZ) >= deadZoneAngle)
                steer = signedZ / maxTiltAngle;
        }
        // 2) Throttle: only if pinching (“use”) is in progress
        float throttle = _isUsing ? _lastStrength : 0f;

        // 3) Send combined input each frame
        _carController.HandleVRInput(new Vector2(steer, throttle));
    }
    void LateUpdate()
    {
        // 2) Enforce the clamp after transformers move the pivot
        float rawZ = _wheelPivot.localEulerAngles.z;
        float signedZ = Mathf.DeltaAngle(0f, rawZ);
        float clampedZ = Mathf.Clamp(signedZ, -maxTiltAngle, maxTiltAngle);

        if (!Mathf.Approximately(signedZ, clampedZ))
        {
            // snap it back into range
            _wheelPivot.localRotation = Quaternion.Euler(0f, 0f, clampedZ);
        }
    }
    // IHandGrabUseDelegate ↓↓↓
    public void BeginUse()
    {
        _isUsing = true;
    }

    public void EndUse()
    {
        _isUsing = false;
    }

    public float ComputeUseStrength(float strength)
    {
        _lastStrength = strength;
        // return unmodified so UseProgress tracks your real pinch
        return strength;
    }
    /// <summary>
    /// Call this any time you completely reset the car.
    /// It will zero the wheel pivot and clear any use state.
    /// </summary>
    public void ResetSteeringWheel()
    {
        // 1) Snap the visual wheel back upright
        if (_wheelPivot != null)
            _wheelPivot.localRotation = Quaternion.identity;

        // 2) Clear any “use” state so we don’t instantly throttle
        _isUsing = false;
        _lastStrength = 0f;

        // 3) (Optional) clear any grab state if you want
        // _grabInteractable.ClearSelectingInteractors(); // if you expose a method for that

        Debug.Log("[SteeringWheelUse] Wheel has been reset to center.");
    }
}
