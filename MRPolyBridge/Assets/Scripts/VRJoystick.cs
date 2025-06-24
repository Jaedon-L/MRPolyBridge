using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;
using Oculus.Interaction.HandGrab;
using UnityEngine.Animations;
using static Oculus.Interaction.TransformerUtils;

public class VRJoystick : MonoBehaviour
{
    [Header("Joystick Settings")]
    [SerializeField] private Transform joystickHandle;
    [SerializeField] private Transform joystickBase;
    [SerializeField] private float maxDistance = 1f;
    [SerializeField] private float deadZone = 0.1f;
    [SerializeField] private bool returnToCenter = true;
    [SerializeField] private float returnSpeed = 5f;

    [Header("Visual Feedback")]
    [SerializeField] private LineRenderer directionIndicator;
    [SerializeField] private Color activeColor = Color.green;
    [SerializeField] private Color inactiveColor = Color.gray;

    [Header("Debug")]
    [SerializeField] private bool enableDebugLogs = true;

    private Vector3 centerPosition;
    private Vector3 currentInput = Vector3.zero;
    private bool isGrabbed = false;
    private BridgeWalker currentBridgeWalker;

    // Current movement state
    private bool isMovingUp = false;
    private bool isMovingDown = false;
    private bool isMovingLeft = false;
    private bool isMovingRight = false;

    // Debug counters
    private int debugCounter = 0;

    void Start()
    {
        Initialize();
    }

    private void Awake()
    {
        if (joystickHandle != null)
        {
            centerPosition = joystickHandle.localPosition;
            Debug.Log($"[VRJoystick] Awake: Center position set to {centerPosition}");
        }
        else
        {
            Debug.LogError("[VRJoystick] joystickHandle is null in Awake!");
        }
    }

    private void Initialize()
    {
        Debug.Log("[VRJoystick] Initialize() called.");

        // Check all required components
        if (joystickHandle == null)
        {
            Debug.LogError("[VRJoystick] joystickHandle is not assigned!");
            return;
        }

        if (joystickBase == null)
        {
            Debug.LogError("[VRJoystick] joystickBase is not assigned!");
            return;
        }

        // Setup visual feedback
        if (directionIndicator != null)
        {
            directionIndicator.positionCount = 2;
            directionIndicator.enabled = false;
            Debug.Log("[VRJoystick] Direction indicator configured");
        }
        else
        {
            Debug.LogWarning("[VRJoystick] No direction indicator assigned");
        }

        // Find current BridgeWalker
        RefreshBridgeWalkerReference();

        Debug.Log($"[VRJoystick] Initialized. Handle: {joystickHandle?.name}, " +
                 $"Base: {joystickBase?.name}, " +
                 $"BridgeWalker: {currentBridgeWalker?.name ?? "null"}");
    }

    void Update()
    {
        // Debug every 60 frames (once per second at 60fps)
        debugCounter++;
        if (debugCounter >= 60)
        {
            debugCounter = 0;
            if (enableDebugLogs)
            {
                Debug.Log($"[VRJoystick] Status - Grabbed: {isGrabbed}, " +
                         $"Input: {currentInput}, " +
                         $"BridgeWalker: {currentBridgeWalker?.name ?? "NULL"}, " +
                         $"Handle Pos: {joystickHandle?.localPosition ?? Vector3.zero}");
            }
        }

        // Refresh BridgeWalker reference if lost
        if (currentBridgeWalker == null)
        {
            RefreshBridgeWalkerReference();
        }

        // Check if being grabbed using multiple methods
        bool wasGrabbed = isGrabbed;
        isGrabbed = IsBeingGrabbed();

        if (isGrabbed != wasGrabbed)
        {
            Debug.Log($"[VRJoystick] Grab state changed: {wasGrabbed} -> {isGrabbed}");
            if (isGrabbed)
            {
                if (directionIndicator != null) directionIndicator.enabled = true;
            }
            else
            {
                StopAllMovement();
                if (directionIndicator != null) directionIndicator.enabled = false;
            }
        }

        if (isGrabbed)
        {
            UpdateJoystickPosition();
            UpdateMovementInput();
        }
        else if (returnToCenter)
        {
            ReturnToCenter();
        }

        // Always update visual feedback
        UpdateVisualFeedback();

        // Enable/disable UI controls based on joystick state
        var gameManager = FindFirstObjectByType<GameManager>();
        if (gameManager != null)
        {
            gameManager.EnableUIControls(!isGrabbed);
        }

        // Debug drawing
        if (joystickBase != null && joystickHandle != null)
        {
            Debug.DrawLine(joystickBase.position, joystickHandle.position, Color.yellow);
        }

        // Log input when active
        if (enableDebugLogs && isGrabbed && currentInput.magnitude > 0.1f)
        {
            Debug.DrawRay(joystickHandle.position, currentInput.normalized, Color.green);
        }
    }

    private bool IsBeingGrabbed()
    {
        // Method 1: Check HandGrabInteractable
        var handGrabInteractable = GetComponentInChildren<HandGrabInteractable>();
        if (handGrabInteractable != null && handGrabInteractable.Interactors.Count > 0)
        {
            if (enableDebugLogs && debugCounter == 0)
                Debug.Log("[VRJoystick] Grabbed via HandGrabInteractable");
            return true;
        }

        // Method 2: Check GrabInteractable
        var grabInteractable = GetComponentInChildren<GrabInteractable>();
        if (grabInteractable != null && grabInteractable.Interactors.Count > 0)
        {
            if (enableDebugLogs && debugCounter == 0)
                Debug.Log("[VRJoystick] Grabbed via GrabInteractable");
            return true;
        }

        // Method 3: Check for any Grabbable component
        var grabbable = GetComponentInChildren<Grabbable>();
        if (grabbable != null)
        {
            // Try to access selection state if available
            if (enableDebugLogs && debugCounter == 0)
                Debug.Log("[VRJoystick] Found Grabbable component");
        }

        return false;
    }

    /// <summary>
    /// Call this when a new level is loaded to refresh the BridgeWalker reference
    /// </summary>
    public void RefreshBridgeWalkerReference()
    {
        var oldWalker = currentBridgeWalker;
        currentBridgeWalker = FindFirstObjectByType<BridgeWalker>();

        if (currentBridgeWalker != oldWalker)
        {
            Debug.Log($"[VRJoystick] BridgeWalker reference updated: {oldWalker?.name ?? "null"} -> {currentBridgeWalker?.name ?? "null"}");
        }

        if (currentBridgeWalker == null)
        {
            Debug.LogWarning("[VRJoystick] No BridgeWalker found in scene!");
        }
    }

    private void UpdateJoystickPosition()
    {
        if (joystickHandle == null)
        {
            Debug.LogError("[VRJoystick] joystickHandle is null in UpdateJoystickPosition!");
            return;
        }

        // Get the current position relative to base
        Vector3 localPos = joystickHandle.localPosition;
        Vector3 originalPos = localPos;

        // Clamp to max distance (create circular boundary)
        Vector3 flatPos = new Vector3(localPos.x, 0, localPos.z);
        if (flatPos.magnitude > maxDistance)
        {
            flatPos = flatPos.normalized * maxDistance;
            joystickHandle.localPosition = new Vector3(flatPos.x, centerPosition.y, flatPos.z);
            localPos = joystickHandle.localPosition;
        }

        // Calculate normalized input (-1 to 1)
        Vector3 deltaFromCenter = localPos - centerPosition;
        currentInput = new Vector3(
            deltaFromCenter.x / maxDistance,
            0,
            deltaFromCenter.z / maxDistance
        );

        if (enableDebugLogs && debugCounter == 0)
        {
            Debug.Log($"[VRJoystick] Position update - Original: {originalPos}, " +
                     $"Final: {localPos}, Center: {centerPosition}, " +
                     $"Delta: {deltaFromCenter}, Input: {currentInput}, " +
                     $"Magnitude: {currentInput.magnitude}");
        }

        // Apply dead zone
        if (currentInput.magnitude < deadZone)
        {
            currentInput = Vector3.zero;
        }
    }

    private void UpdateMovementInput()
    {
        if (currentBridgeWalker == null)
        {
            if (enableDebugLogs && debugCounter == 0)
                Debug.LogWarning("[VRJoystick] No BridgeWalker found for movement input!");
            return;
        }

        // Determine movement directions based on joystick input
        bool shouldMoveUp = currentInput.z > deadZone;
        bool shouldMoveDown = currentInput.z < -deadZone;
        bool shouldMoveLeft = currentInput.x < -deadZone;
        bool shouldMoveRight = currentInput.x > deadZone;

        if (enableDebugLogs && debugCounter == 0)
        {
            Debug.Log($"[VRJoystick] Movement check - Input: {currentInput}, " +
                     $"Should move - Up: {shouldMoveUp}, Down: {shouldMoveDown}, " +
                     $"Left: {shouldMoveLeft}, Right: {shouldMoveRight}");
        }

        // Handle Up movement
        if (shouldMoveUp && !isMovingUp)
        {
            currentBridgeWalker.StartMoveUp();
            isMovingUp = true;
            Debug.Log("[VRJoystick] Started moving UP");
        }
        else if (!shouldMoveUp && isMovingUp)
        {
            isMovingUp = false;
            CheckStopMovement();
        }

        // Handle Down movement
        if (shouldMoveDown && !isMovingDown)
        {
            currentBridgeWalker.StartMoveDown();
            isMovingDown = true;
            Debug.Log("[VRJoystick] Started moving DOWN");
        }
        else if (!shouldMoveDown && isMovingDown)
        {
            isMovingDown = false;
            CheckStopMovement();
        }

        // Handle Left movement
        if (shouldMoveLeft && !isMovingLeft)
        {
            currentBridgeWalker.StartMoveLeft();
            isMovingLeft = true;
            Debug.Log("[VRJoystick] Started moving LEFT");
        }
        else if (!shouldMoveLeft && isMovingLeft)
        {
            isMovingLeft = false;
            CheckStopMovement();
        }

        // Handle Right movement
        if (shouldMoveRight && !isMovingRight)
        {
            currentBridgeWalker.StartMoveRight();
            isMovingRight = true;
            Debug.Log("[VRJoystick] Started moving RIGHT");
        }
        else if (!shouldMoveRight && isMovingRight)
        {
            isMovingRight = false;
            CheckStopMovement();
        }
    }

    private void CheckStopMovement()
    {
        // Only stop movement if no directions are active
        if (!isMovingUp && !isMovingDown && !isMovingLeft && !isMovingRight)
        {
            if (currentBridgeWalker != null)
            {
                currentBridgeWalker.StopMove();
                if (enableDebugLogs) Debug.Log("[VRJoystick] Stopped all movement");
            }
        }
    }

    private void StopAllMovement()
    {
        isMovingUp = false;
        isMovingDown = false;
        isMovingLeft = false;
        isMovingRight = false;
        if (currentBridgeWalker != null)
        {
            currentBridgeWalker.StopMove();
        }
        Debug.Log("[VRJoystick] Force stopped all movement");
    }

    private void ReturnToCenter()
    {
        if (joystickHandle == null) return;

        // Smoothly return joystick to center when not grabbed
        joystickHandle.localPosition = Vector3.Lerp(
            joystickHandle.localPosition,
            centerPosition,
            returnSpeed * Time.deltaTime
        );

        // Stop movement when close to center
        if (Vector3.Distance(joystickHandle.localPosition, centerPosition) < 0.01f)
        {
            joystickHandle.localPosition = centerPosition;
            currentInput = Vector3.zero;
        }
    }

    private void UpdateVisualFeedback()
    {
        if (directionIndicator != null && joystickBase != null && joystickHandle != null)
        {
            // Update line renderer to show direction
            Vector3 basePos = joystickBase.position;
            Vector3 handlePos = joystickHandle.position;

            directionIndicator.SetPosition(0, basePos);
            directionIndicator.SetPosition(1, handlePos);

            // Change color based on input strength
            Color currentColor = Color.Lerp(inactiveColor, activeColor, currentInput.magnitude);
            if (directionIndicator.material != null)
            {
                directionIndicator.material.color = currentColor;
            }
        }
    }

    // Public methods for debugging or external control
    public Vector3 GetJoystickInput()
    {
        return currentInput;
    }

    public bool IsActive()
    {
        return currentInput.magnitude > deadZone;
    }

    public bool HasValidBridgeWalker()
    {
        return currentBridgeWalker != null;
    }

    // Manual testing methods - call these from inspector or other scripts
    [ContextMenu("Test Move Up")]
    public void TestMoveUp()
    {
        Debug.Log("[VRJoystick] Manual test: Move Up");
        if (currentBridgeWalker != null)
        {
            currentBridgeWalker.StartMoveUp();
            StartCoroutine(StopAfterDelay(1f));
        }
        else
        {
            Debug.LogError("[VRJoystick] No BridgeWalker for test!");
        }
    }

    [ContextMenu("Test Move Down")]
    public void TestMoveDown()
    {
        Debug.Log("[VRJoystick] Manual test: Move Down");
        if (currentBridgeWalker != null)
        {
            currentBridgeWalker.StartMoveDown();
            StartCoroutine(StopAfterDelay(1f));
        }
    }

    [ContextMenu("Test Move Left")]
    public void TestMoveLeft()
    {
        Debug.Log("[VRJoystick] Manual test: Move Left");
        if (currentBridgeWalker != null)
        {
            currentBridgeWalker.StartMoveLeft();
            StartCoroutine(StopAfterDelay(1f));
        }
    }

    [ContextMenu("Test Move Right")]
    public void TestMoveRight()
    {
        Debug.Log("[VRJoystick] Manual test: Move Right");
        if (currentBridgeWalker != null)
        {
            currentBridgeWalker.StartMoveRight();
            StartCoroutine(StopAfterDelay(1f));
        }
    }

    private IEnumerator StopAfterDelay(float delay)
    {
        yield return new WaitForSeconds(delay);
        if (currentBridgeWalker != null)
        {
            currentBridgeWalker.StopMove();
            Debug.Log("[VRJoystick] Test movement stopped");
        }
    }

    // Force grab simulation for testing
    [ContextMenu("Simulate Grab")]
    public void SimulateGrab()
    {
        isGrabbed = true;
        Debug.Log("[VRJoystick] Simulating grab for testing");
    }

    [ContextMenu("Simulate Release")]
    public void SimulateRelease()
    {
        isGrabbed = false;
        StopAllMovement();
        Debug.Log("[VRJoystick] Simulating release for testing");
    }
}