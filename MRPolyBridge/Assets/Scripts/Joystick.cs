using UnityEngine;
using Oculus.Interaction;

public class Joystick : MonoBehaviour
{
    [Header("Joystick Transforms")]
    [Tooltip("Pivot transform around which the joystick handle tilts.")]
    [SerializeField] private Transform joystickPivot;

    [Header("Car Controller Reference")]
    [Tooltip("Assign your PrometeoCarController here.")]
    [SerializeField] private PrometeoCarController carController;

    [Header("Tilt Settings")]
    [Tooltip("Maximum tilt angle in degrees recognized for input.")]
    [SerializeField] private float maxTiltAngle = 30f;
    [Tooltip("Dead zone angle in degrees: small tilts within this range produce zero input.")]
    [SerializeField] private float deadZoneAngle = 5f;
    [Tooltip("Speed at which joystick returns upright when released.")]
    [SerializeField] private float returnSpeed = 5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private bool isGrabbed = false;
    private Vector2 joystickInput = Vector2.zero;

    void Awake()
    {
        if (joystickPivot == null)
            Debug.LogError("[Joystick] joystickPivot is not assigned!");
        if (carController == null)
            Debug.LogError("[Joystick] carController is not assigned!");
    }

    /// <summary>
    /// Hook this to your WhenSelect event.
    /// </summary>
    public void OnGrabStarted()
    {
        isGrabbed = true;
        if (enableDebugLogs)
            Debug.Log("[Joystick] OnGrabStarted");
    }

    /// <summary>
    /// Hook this to your WhenUnselect event.
    /// </summary>
    public void OnGrabEnded()
    {
        isGrabbed = false;
        joystickInput = Vector2.zero;
        if (carController != null)
            carController.HandleVRInput(Vector2.zero);
        if (enableDebugLogs)
            Debug.Log("[Joystick] OnGrabEnded");
    }

    void Update()
    {
        if (joystickPivot == null) return;

        if (enableDebugLogs)
            Debug.Log("[Joystick] Update called; isGrabbed=" + isGrabbed);

        if (isGrabbed)
        {
            ReadPivotRotationAsInput();
        }
        else
        {
            // Return to upright
            if (joystickPivot.localRotation != Quaternion.identity)
            {
                joystickPivot.localRotation = Quaternion.Slerp(
                    joystickPivot.localRotation,
                    Quaternion.identity,
                    returnSpeed * Time.deltaTime
                );
            }
            joystickInput = Vector2.zero;
        }

        // Send to car
        if (carController != null)
        {
            if (enableDebugLogs)
                Debug.Log($"[Joystick] Applying to car: input=({joystickInput.x:F2},{joystickInput.y:F2})");
            carController.HandleVRInput(joystickInput);
        }
    }

    /// <summary>
    /// Reads pivot.localRotation and maps to joystickInput.
    /// Adjust mapping logic (signs/axes) based on your observed pivot Euler behaviour.
    /// </summary>
private void ReadPivotRotationAsInput()
{
    Vector3 euler = joystickPivot.localRotation.eulerAngles;
    float angleX = euler.x > 180f ? euler.x - 360f : euler.x;
    float angleZ = euler.z > 180f ? euler.z - 360f : euler.z;

    if (enableDebugLogs)
        Debug.Log($"[Joystick] Pivot signed Euler: X={angleX:F1}, Z={angleZ:F1}");

    float clampedX = Mathf.Clamp(angleX, -maxTiltAngle, maxTiltAngle);
    float clampedZ = Mathf.Clamp(angleZ, -maxTiltAngle, maxTiltAngle);

    float inputY = 0f;
    float inputX = 0f;

    // Forward/back remains as before:
    if (Mathf.Abs(clampedZ) >= deadZoneAngle)
        inputY = -clampedZ / maxTiltAngle;

    // Reverse left/right: negate clampedX
    if (Mathf.Abs(clampedX) >= deadZoneAngle)
        inputX = -clampedX / maxTiltAngle;

    joystickInput = new Vector2(inputX, inputY);

    if (enableDebugLogs)
        Debug.Log($"[Joystick] Mapped joystickInput=({inputX:F2},{inputY:F2})");
}

}
