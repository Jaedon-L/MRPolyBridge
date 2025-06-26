using UnityEngine;
using Oculus.Interaction;

public class Joystick : MonoBehaviour
{
    [Header("Joystick Transforms")]
    [SerializeField] private Transform joystickPivot;

    [Header("Car Controller Reference")]
    [SerializeField] private PrometeoCarController carController;

    [Header("Tilt Settings")]
    [SerializeField] private float maxTiltAngle    = 30f;
    [SerializeField] private float deadZoneAngle   =  5f;
    [SerializeField] private float returnSpeed     =  5f;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = false;

    private bool   isGrabbed     = false;
    private Vector2 joystickInput = Vector2.zero;

    void Awake()
    {
        if (joystickPivot == null)
            Debug.LogError("[Joystick] joystickPivot is not assigned!");
        if (carController == null)
            Debug.LogError("[Joystick] carController is not assigned!");
    }

    public void OnGrabStarted()
    {
        isGrabbed = true;
        if (enableDebugLogs) Debug.Log("[Joystick] OnGrabStarted");
    }

    public void OnGrabEnded()
    {
        isGrabbed = false;
        joystickInput = Vector2.zero;
        carController?.HandleVRInput(Vector2.zero);
        if (enableDebugLogs) Debug.Log("[Joystick] OnGrabEnded");
    }

    void Update()
    {
        if (joystickPivot == null) return;

        if (isGrabbed)
        {
            ReadPivotRotationAsInput();
        }
        else
        {
            // Smoothly return to upright when released
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

        // Send input to car
        if (carController != null)
        {
            // if (enableDebugLogs)
            //     Debug.Log($"[Joystick] Applying to car: input=({joystickInput.x:F2}, {joystickInput.y:F2})");
            carController.HandleVRInput(joystickInput);
        }
    }

    void LateUpdate()
    {
        // Always clamp the pivot rotation, whether grabbed or not,
        // to enforce the ±45° physical limit.
        if (joystickPivot == null) return;

        Vector3 euler = joystickPivot.localRotation.eulerAngles;
        float angleX = (euler.x > 180f ? euler.x - 360f : euler.x);
        float angleZ = (euler.z > 180f ? euler.z - 360f : euler.z);

        angleX = Mathf.Clamp(angleX, -45f, 45f);
        angleZ = Mathf.Clamp(angleZ, -45f, 45f);

        joystickPivot.localRotation = Quaternion.Euler(angleX, 0f, angleZ);
    }

    /// <summary>
    /// Converts the (now-clamped) pivot.localRotation into normalized joystick input.
    /// </summary>
    private void ReadPivotRotationAsInput()
    {
        Vector3 euler = joystickPivot.localRotation.eulerAngles;
        float angleX = (euler.x > 180f ? euler.x - 360f : euler.x);
        float angleZ = (euler.z > 180f ? euler.z - 360f : euler.z);

        if (enableDebugLogs)
            Debug.Log($"[Joystick] Pivot Euler: X={angleX:F1}, Z={angleZ:F1}");

        // Further clamp to the maxTiltAngle for input mapping
        float clampedX = Mathf.Clamp(angleX, -maxTiltAngle, maxTiltAngle);
        float clampedZ = Mathf.Clamp(angleZ, -maxTiltAngle, maxTiltAngle);

        float inputX = 0f, inputY = 0f;

        if (Mathf.Abs(clampedZ) >= deadZoneAngle)
            inputY = -clampedZ / maxTiltAngle;  // forward/back

        if (Mathf.Abs(clampedX) >= deadZoneAngle)
            inputX = -clampedX / maxTiltAngle;  // left/right

        joystickInput = new Vector2(inputX, inputY);

        if (enableDebugLogs)
            Debug.Log($"[Joystick] Mapped Input: ({inputX:F2}, {inputY:F2})");
    }
}
