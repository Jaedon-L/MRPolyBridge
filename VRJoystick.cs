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
    private Grabbable grabbableComponent;
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

        // Get or add Grabbable component
        grabbableComponent = gameObject.GetComponentInChildren<Grabbable>(true);
        if (grabbableComponent == null)
        {
            Debug.LogError("[VRJoystick] Grabbable not found!");
            grabbableComponent = gameObject.AddComponent<Grabbable>();
        }
        else
        {
            Debug.Log("[VRJoystick] Found grabbable: " + grabbableComponent.name);
        }

        // Subscribe to grab events
        if (grabbableComponent != null)
        {
            grabbableComponent.WhenPointerEventRaised += OnPointerEvent;
            Debug.Log("[VRJoystick] Successfully hooked into grab events.");
        }
        else
        {
            Debug.LogError("[VRJoystick] Could not find Grabbable component in ISDK_HandGrabInteraction.");
        }

        // Setup visual feedback
        if (directionIndicator != null)
        {
            directionIndicator.positionCount = 2;
            directionIndicator.enabled = false;
        }

        // Find current BridgeWalker
        RefreshBridgeWalkerReference();

        if (enableDebugLogs)
        {
            Debug.Log($"[VRJoystick] Initialized. Handle: {joystickHandle?.name}, " +
                     $"Grabbable: {grabbableComponent != null}, " +
                     $"BridgeWalker: {currentBridgeWalker?.name ?? "null"}");
        }
    }

    void Update()
    {
        // Refresh BridgeWalker reference if lost
        if (currentBridgeWalker == null)
        {
            RefreshBridgeWalkerReference();
        }

        if (IsBeingGrabbed())
        {
            if (!isGrabbed)
            {
                isGrabbed = true;
                if (enableDebugLogs) Debug.Log("[VRJoystick] Hand grab detected (start).");
                if (directionIndicator != null) directionIndicator.enabled = true;
            }

            UpdateJoystickPosition();
            UpdateMovementInput();
        }
        else
        {
            if (isGrabbed)
            {
                isGrabbed = false;
                if (enableDebugLogs) Debug.Log("[VRJoystick] Hand grab ended.");
                StopAllMovement();
                if (directionIndicator != null) directionIndicator.enabled = false;
            }

            if (returnToCenter)
            {
                ReturnToCenter();
            }
        }
        Debug.DrawLine(joystickBase.position, joystickHandle.position, Color.yellow);
        Debug.Log($"[DEBUG] Handle Local Pos: {joystickHandle.localPosition}, Center: {centerPosition}");

        FindFirstObjectByType<GameManager>()?.EnableUIControls(!isGrabbed);
        UpdateVisualFeedback();

        if (enableDebugLogs && isGrabbed && currentInput.magnitude > 0.1f)
        {
            Debug.DrawRay(joystickHandle.position, currentInput.normalized, Color.green);
            Debug.Log($"[VRJoystick] Heartbeat → Input: {currentInput}");
        }
    }

    private bool IsBeingGrabbed()
    {
        var grabInteractable = GetComponentInChildren<GrabInteractable>();
        if (grabInteractable == null)
            return false;

        // If any interactor is holding the object
        return grabInteractable.Interactors.Count > 0;
    }

    /// <summary>
    /// Call this when a new level is loaded to refresh the BridgeWalker reference
    /// </summary>
    public void RefreshBridgeWalkerReference()
    {
        currentBridgeWalker = FindFirstObjectByType<BridgeWalker>();
        if (enableDebugLogs)
        {
            Debug.Log($"[VRJoystick] BridgeWalker reference updated: {currentBridgeWalker?.name ?? "null"}");
        }
    }

    private void OnPointerEvent(PointerEvent pointerEvent)
    {
        if (enableDebugLogs)
        {
            Debug.Log($"[VRJoystick] Pointer event: {pointerEvent.Type}");
        }

        switch (pointerEvent.Type)
        {
            case PointerEventType.Select:
                if (enableDebugLogs) Debug.Log("[VRJoystick] Joystick grabbed!");
                isGrabbed = true;
                if (directionIndicator != null)
                    directionIndicator.enabled = true;
                break;

            case PointerEventType.Unselect:
                if (enableDebugLogs) Debug.Log("[VRJoystick] Joystick released!");
                isGrabbed = false;
                StopAllMovement();
                if (directionIndicator != null)
                    directionIndicator.enabled = false;
                break;
        }
    }

    private void UpdateJoystickPosition()
    {
        // Get the current position relative to base
        Vector3 localPos = joystickHandle.localPosition;

        // Clamp to max distance (create circular boundary)
        Vector3 flatPos = new Vector3(localPos.x, 0, localPos.z);
        if (flatPos.magnitude > maxDistance)
        {
            flatPos = flatPos.normalized * maxDistance;
            joystickHandle.localPosition = new Vector3(flatPos.x, centerPosition.y, flatPos.z);
        }

        // Calculate normalized input (-1 to 1)
        currentInput = new Vector3(
            flatPos.x / maxDistance,
            0,
            flatPos.z / maxDistance
        );
        Debug.Log($"[VRJoystick] Raw input vector: {currentInput}, magnitude: {currentInput.magnitude}, dead zone: {deadZone}");


        // Apply dead zone
        if (currentInput.magnitude < deadZone)
        {
            currentInput = Vector3.zero;
        }

        if (enableDebugLogs && currentInput.magnitude > 0)
        {
            Debug.Log($"[VRJoystick] Input: {currentInput}, Magnitude: {currentInput.magnitude}");
        }
    }

    private void UpdateMovementInput()
    {
        Debug.Log($"[VRJoystick] Calling movement update. currentInput: {currentInput}");
        if (currentBridgeWalker == null || currentBridgeWalker.Equals(null))
        {
            if (enableDebugLogs)
                Debug.LogWarning("[VRJoystick] No BridgeWalker found for movement input!");
            return;
        }

        // Determine movement directions based on joystick input
        bool shouldMoveUp = currentInput.z > deadZone;
        bool shouldMoveDown = currentInput.z < -deadZone;
        bool shouldMoveLeft = currentInput.x < -deadZone;
        bool shouldMoveRight = currentInput.x > deadZone;

        // Handle Up movement
        if (shouldMoveUp && !isMovingUp)
        {
            currentBridgeWalker.StartMoveUp();
            isMovingUp = true;
            if (enableDebugLogs) Debug.Log("[VRJoystick] Started moving UP");
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
            if (enableDebugLogs) Debug.Log("[VRJoystick] Started moving DOWN");
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
            if (enableDebugLogs) Debug.Log("[VRJoystick] Started moving LEFT");
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
            if (enableDebugLogs) Debug.Log("[VRJoystick] Started moving RIGHT");
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
        if (enableDebugLogs) Debug.Log("[VRJoystick] Force stopped all movement");
    }

    private void ReturnToCenter()
    {
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
        if (directionIndicator != null)
        {
            // Update line renderer to show direction
            Vector3 basePos = joystickBase.position;
            Vector3 handlePos = joystickHandle.position;

            directionIndicator.SetPosition(0, basePos);
            directionIndicator.SetPosition(1, handlePos);

            // Change color based on input strength
            Color currentColor = Color.Lerp(inactiveColor, activeColor, currentInput.magnitude);
            directionIndicator.material.color = currentColor;
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

    void OnDestroy()
    {
        if (grabbableComponent != null)
        {
            grabbableComponent.WhenPointerEventRaised -= OnPointerEvent;
        }
    }
}