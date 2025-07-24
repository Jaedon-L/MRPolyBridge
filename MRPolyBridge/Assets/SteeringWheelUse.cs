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

    void Awake()
    {
        // _useInteractable = GetComponent<HandGrabUseInteractable>();
        _useInteractable.InjectOptionalForwardUseDelegate(this);
    }

    // IHandGrabUseDelegate ▶ called once when you start pinching/gripping
    [ContextMenu("Forward")]
    public void BeginUse()
    {
        OnUseStarted?.Invoke();
        Debug.Log("Began use");
    }

    // IHandGrabUseDelegate ▶ called once when you release pinch/grip
    public void EndUse()
    {
        OnUseEnded?.Invoke();
        // zero out your forward/back
        _carController.HandleVRInput(Vector2.zero);
        Debug.Log("Ended use");
    }

    // IHandGrabUseDelegate ▶ each frame while pinching/gripping
    public float ComputeUseStrength(float strength)
    {
        // 1) Map wheel Z‑rotation → forward/back
        //    mirror your joystick’s Z‑axis logic:
        var euler = _wheelPivot.localEulerAngles;
        float angleZ = euler.z > 180f ? euler.z - 360f : euler.z;
        float clampedZ = Mathf.Clamp(angleZ, -maxTiltAngle, maxTiltAngle);

        float forwardBack = 0f;
        if (Mathf.Abs(clampedZ) >= deadZoneAngle)
            forwardBack = -clampedZ / maxTiltAngle;

        // 2) Fire your event so other systems can listen
        OnForwardBackAxis?.Invoke(forwardBack);

        // 3) Send only forward/back into your car (no steering)
        _carController.HandleVRInput(new Vector2(0f, forwardBack));

        // 4) Return strength unchanged so UseProgress still tracks finger curl
        Debug.Log($"Wheel UseStrength: {strength:F2}");
        return strength;
    }
}
