using System.Collections;
using UnityEngine;

public class MicrogestureCarController : MonoBehaviour
{
    [Header("OVR Hand Reference")]
    [Tooltip("Assign the OVRHand component reference here.")]
    public OVRHand ovrHand;

    [Header("Car Controller Reference")]
    [Tooltip("Assign your PrometeoCarController here.")]
    public PrometeoCarController carController;

    [Header("Steering Settings")]
    [Tooltip("How long (seconds) a steering swipe holds the turn.")]
    public float steerDuration = 0.5f;

    [Header("Throttle Settings")]
    [Tooltip("Whether throttle state is toggled by swipe gestures.")]
    public bool useToggleThrottle = true;

    [Header("Debug")]
    public bool enableDebugLogs = true;

    // Internal state
    private bool isAcceleratingForward = false;
    private bool isAcceleratingReverse = false;

    private Coroutine steeringCoroutine = null;

    void Update()
    {
        if (ovrHand == null || carController == null)
            return;

        // Read current microgesture
        OVRHand.MicrogestureType microgesture = ovrHand.GetMicrogestureType();
        if (microgesture == OVRHand.MicrogestureType.Invalid || microgesture == OVRHand.MicrogestureType.NoGesture)
            return;

        // Handle gesture once per detection. Depending on OVRHand behavior, 
        // you may need to detect transition from NoGesture to a gesture to avoid repeated triggers.
        HandleGesture(microgesture);
    }

    private void HandleGesture(OVRHand.MicrogestureType gesture)
    {
        if (enableDebugLogs) Debug.Log($"[MicrogestureCarController] Detected gesture: {gesture}");

        switch (gesture)
        {
            case OVRHand.MicrogestureType.SwipeForward:
                HandleSwipeForward();
                break;
            case OVRHand.MicrogestureType.SwipeBackward:
                HandleSwipeBackward();
                break;
            case OVRHand.MicrogestureType.SwipeLeft:
                HandleSwipeLeft();
                break;
            case OVRHand.MicrogestureType.SwipeRight:
                HandleSwipeRight();
                break;
            case OVRHand.MicrogestureType.ThumbTap:
                HandleThumbTap();
                break;
            // Other gestures can be mapped if desired
            default:
                break;
        }
    }

    #region Gesture Handlers
    [ContextMenu("handleswipeforward")]
    private void HandleSwipeForward()
    {
        if (useToggleThrottle)
        {
            if (!isAcceleratingForward)
            {
                // Start forward acceleration; cancel reverse if active
                isAcceleratingForward = true;
                isAcceleratingReverse = false;
                if (enableDebugLogs) Debug.Log("[MicrogestureCarController] Started accelerating forward.");
            }
            else
            {
                // Already accelerating forward: maybe ignore or stop. Here, we stop
                isAcceleratingForward = false;
                if (enableDebugLogs) Debug.Log("[MicrogestureCarController] Stopped accelerating forward.");
            }
        }
        else
        {
            // If not toggle mode, maybe apply a momentary impulse? For simplicity, toggle mode recommended.
        }
        UpdateCarThrottleState();
    }

    private void HandleSwipeBackward()
    {
        if (useToggleThrottle)
        {
            if (!isAcceleratingReverse)
            {
                // Start reverse acceleration; cancel forward if active
                isAcceleratingReverse = true;
                isAcceleratingForward = false;
                if (enableDebugLogs) Debug.Log("[MicrogestureCarController] Started accelerating reverse.");
            }
            else
            {
                // Already reversing: stop
                isAcceleratingReverse = false;
                if (enableDebugLogs) Debug.Log("[MicrogestureCarController] Stopped accelerating reverse.");
            }
        }
        UpdateCarThrottleState();
    }

    private void HandleSwipeLeft()
    {
        // Start a steering left impulse for steerDuration
        if (steeringCoroutine != null)
        {
            StopCoroutine(steeringCoroutine);
        }
        steeringCoroutine = StartCoroutine(SteerForDuration(Vector2.left, steerDuration));
        if (enableDebugLogs) Debug.Log("[MicrogestureCarController] Steering left.");
    }

    private void HandleSwipeRight()
    {
        // Start a steering right impulse
        if (steeringCoroutine != null)
        {
            StopCoroutine(steeringCoroutine);
        }
        steeringCoroutine = StartCoroutine(SteerForDuration(Vector2.right, steerDuration));
        if (enableDebugLogs) Debug.Log("[MicrogestureCarController] Steering right.");
    }

    private void HandleThumbTap()
    {
        // Example: immediate brake / stop both throttle and steering
        // You can call carController.Brakes() for an instant brake, and reset throttle state
        if (enableDebugLogs) Debug.Log("[MicrogestureCarController] ThumbTap: Applying brake and stopping throttle.");
        // Stop throttle toggles:
        isAcceleratingForward = false;
        isAcceleratingReverse = false;
        UpdateCarThrottleState();
        // Stop steering coroutine if any
        if (steeringCoroutine != null)
        {
            StopCoroutine(steeringCoroutine);
            steeringCoroutine = null;
        }
        // Reset steering angle immediately
        carController.ResetSteeringAngle();
        // Apply brake torque briefly
        carController.Brakes();
        // Optionally, you might want to release brakes after a short moment; or let the car’s braking logic handle stopping.
    }

    #endregion

    #region Car Control Methods

    private void UpdateCarThrottleState()
    {
        // Called after toggling isAcceleratingForward/isAcceleratingReverse
        if (isAcceleratingForward)
        {
            // Use VRInput mode or direct GoForward? If using VRInput:
            carController.useVRInput = true;
            // To integrate with VRInput-based PrometeoCarController, set vrInput.y positive:
            carController.vrInput = new Vector2(carController.vrInput.x, 1f);
        }
        else if (isAcceleratingReverse)
        {
            carController.useVRInput = true;
            carController.vrInput = new Vector2(carController.vrInput.x, -1f);
        }
        else
        {
            // No throttle
            carController.useVRInput = true;
            carController.vrInput = new Vector2(carController.vrInput.x, 0f);
        }
    }

    private IEnumerator SteerForDuration(Vector2 direction, float duration)
    {
        // direction is Vector2.left or Vector2.right encoded as (-1,0) or (1,0) on x axis
        float elapsed = 0f;
        while (elapsed < duration)
        {
            // Apply steering via VRInput field (keeping current throttle)
            carController.useVRInput = true;
            Vector2 current = carController.vrInput;
            carController.vrInput = new Vector2(direction.x, current.y);
            elapsed += Time.deltaTime;
            yield return null;
        }
        // After duration, reset steering to zero while preserving throttle state
        if (enableDebugLogs) Debug.Log("[MicrogestureCarController] Steering impulse ended.");
        carController.useVRInput = true;
        carController.vrInput = new Vector2(0f, carController.vrInput.y);
        steeringCoroutine = null;
    }

    #endregion
}
