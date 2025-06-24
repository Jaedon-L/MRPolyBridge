using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

public class JoystickController : MonoBehaviour
{
    [Header("Bridge Walker Reference")]
    [Tooltip("Reference to the BridgeWalker script to control")]
    public BridgeWalker bridgeWalker;

    [Header("Joystick Setup")]
    [Tooltip("The center/neutral position of the joystick")]
    public Transform joystickCenter;

    [Tooltip("Maximum distance the joystick can be moved from center")]
    public float joystickMaxDistance = 0.1f;

    [Tooltip("Minimum joystick input to register movement (deadzone)")]
    [Range(0f, 1f)]
    public float joystickDeadzone = 0.1f;

    [Tooltip("How fast the joystick returns to center when released")]
    public float returnSpeed = 5f;

    [Header("Movement Mapping")]
    [Tooltip("Invert forward/backward movement")]
    public bool invertForwardBack = false;

    [Tooltip("Invert left/right movement")]
    public bool invertLeftRight = false;

    [Header("Visual Feedback")]
    [Tooltip("Material to apply when joystick is grabbed")]
    public Material grabbedMaterial;

    [Tooltip("Original material to restore when released")]
    public Material originalMaterial;

    [Header("Debug")]
    [SerializeField] private bool debugJoystick = true;

    private Renderer _renderer;
    private bool _isGrabbed = false;
    private Vector3 _joystickInitialLocalPosition;
    private Vector2 _currentJoystickInput = Vector2.zero;
    private Vector2 _previousJoystickInput = Vector2.zero;

    // References to Meta Interaction SDK components
    private Grabbable _grabbable;
    private PokeInteractable _pokeInteractable;

    // Movement state tracking
    private bool _currentlyMovingForward = false;
    private bool _currentlyMovingBack = false;
    private bool _currentlyMovingLeft = false;
    private bool _currentlyMovingRight = false;

    void Awake()
    {
        _renderer = GetComponent<Renderer>();
        _grabbable = GetComponent<Grabbable>();
        _pokeInteractable = GetComponent<PokeInteractable>();

        // Store original material
        if (_renderer != null && originalMaterial == null)
        {
            originalMaterial = _renderer.material;
        }

        // Store initial local position
        _joystickInitialLocalPosition = transform.localPosition;

        // Find BridgeWalker if not assigned
        if (bridgeWalker == null)
        {
            bridgeWalker = FindObjectOfType<BridgeWalker>();
        }

        // Set joystick center to parent if not assigned
        if (joystickCenter == null && transform.parent != null)
        {
            joystickCenter = transform.parent;
        }
    }

    void Start()
    {
        // Subscribe to grab events
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised += HandleGrabEvent;
        }

        if (_pokeInteractable != null)
        {
            _pokeInteractable.WhenPointerEventRaised += HandlePokeEvent;
        }
    }

    void Update()
    {
        if (_isGrabbed)
        {
            // Constrain joystick movement
            ConstrainJoystick();

            // Update joystick input
            UpdateJoystickInput();

            // Convert joystick input to movement commands
            UpdateMovementCommands();
        }
        else
        {
            // When not grabbed, ensure no movement
            StopAllMovement();
        }
    }

    private void HandleGrabEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                OnJoystickGrabbed();
                break;
            case PointerEventType.Unselect:
                OnJoystickReleased();
                break;
        }
    }

    private void HandlePokeEvent(PointerEvent evt)
    {
        switch (evt.Type)
        {
            case PointerEventType.Select:
                OnJoystickGrabbed();
                break;
            case PointerEventType.Unselect:
                OnJoystickReleased();
                break;
        }
    }

    private void OnJoystickGrabbed()
    {
        _isGrabbed = true;

        // Visual feedback
        if (_renderer != null && grabbedMaterial != null)
        {
            _renderer.material = grabbedMaterial;
        }

        if (debugJoystick) Debug.Log("[JoystickController] Joystick grabbed");
    }

    private void OnJoystickReleased()
    {
        _isGrabbed = false;

        // Stop all movement immediately
        StopAllMovement();

        // Visual feedback
        if (_renderer != null && originalMaterial != null)
        {
            _renderer.material = originalMaterial;
        }

        // Reset joystick to center
        StartCoroutine(SmoothReturnToCenter());

        if (debugJoystick) Debug.Log("[JoystickController] Joystick released");
    }

    private void ConstrainJoystick()
    {
        if (joystickCenter == null) return;

        Vector3 offset = transform.position - joystickCenter.position;

        // Project onto XZ plane (assuming Y is up)
        offset.y = 0;

        float distance = offset.magnitude;

        if (distance > joystickMaxDistance)
        {
            // Constrain to max distance
            Vector3 constrainedOffset = offset.normalized * joystickMaxDistance;
            transform.position = joystickCenter.position + constrainedOffset;
        }
    }

    private void UpdateJoystickInput()
    {
        if (joystickCenter == null)
        {
            _currentJoystickInput = Vector2.zero;
            return;
        }

        // Calculate joystick offset from center
        Vector3 joystickOffset = transform.position - joystickCenter.position;

        // Convert 3D offset to 2D input (X-Z plane)
        Vector2 rawInput = new Vector2(joystickOffset.x, joystickOffset.z);

        // Normalize by max distance
        if (joystickMaxDistance > 0)
        {
            rawInput = rawInput / joystickMaxDistance;
        }

        // Clamp to unit circle
        if (rawInput.magnitude > 1f)
        {
            rawInput = rawInput.normalized;
        }

        // Apply deadzone
        if (rawInput.magnitude < joystickDeadzone)
        {
            _currentJoystickInput = Vector2.zero;
        }
        else
        {
            // Remap from deadzone to 1.0
            float magnitude = (rawInput.magnitude - joystickDeadzone) / (1f - joystickDeadzone);
            _currentJoystickInput = rawInput.normalized * magnitude;
        }

        // Apply inversions
        if (invertLeftRight) _currentJoystickInput.x = -_currentJoystickInput.x;
        if (invertForwardBack) _currentJoystickInput.y = -_currentJoystickInput.y;

        if (debugJoystick && _currentJoystickInput.magnitude > 0.01f)
        {
            Debug.Log($"[JoystickController] Joystick input: {_currentJoystickInput}, magnitude: {_currentJoystickInput.magnitude}");
        }
    }

    private void UpdateMovementCommands()
    {
        if (bridgeWalker == null) return;

        // Determine movement directions based on joystick input
        bool shouldMoveForward = _currentJoystickInput.y > 0.1f;  // Forward (away from user)
        bool shouldMoveBack = _currentJoystickInput.y < -0.1f;    // Back (toward user)
        bool shouldMoveLeft = _currentJoystickInput.x < -0.1f;    // Left
        bool shouldMoveRight = _currentJoystickInput.x > 0.1f;    // Right

        // Handle Forward movement
        if (shouldMoveForward && !_currentlyMovingForward)
        {
            bridgeWalker.StartMoveUp();
            _currentlyMovingForward = true;
            if (debugJoystick) Debug.Log("[JoystickController] Started forward movement");
        }
        else if (!shouldMoveForward && _currentlyMovingForward)
        {
            StopMovementDirection("forward");
            _currentlyMovingForward = false;
        }

        // Handle Back movement
        if (shouldMoveBack && !_currentlyMovingBack)
        {
            bridgeWalker.StartMoveDown();
            _currentlyMovingBack = true;
            if (debugJoystick) Debug.Log("[JoystickController] Started back movement");
        }
        else if (!shouldMoveBack && _currentlyMovingBack)
        {
            StopMovementDirection("back");
            _currentlyMovingBack = false;
        }

        // Handle Left movement
        if (shouldMoveLeft && !_currentlyMovingLeft)
        {
            bridgeWalker.StartMoveLeft();
            _currentlyMovingLeft = true;
            if (debugJoystick) Debug.Log("[JoystickController] Started left movement");
        }
        else if (!shouldMoveLeft && _currentlyMovingLeft)
        {
            StopMovementDirection("left");
            _currentlyMovingLeft = false;
        }

        // Handle Right movement
        if (shouldMoveRight && !_currentlyMovingRight)
        {
            bridgeWalker.StartMoveRight();
            _currentlyMovingRight = true;
            if (debugJoystick) Debug.Log("[JoystickController] Started right movement");
        }
        else if (!shouldMoveRight && _currentlyMovingRight)
        {
            StopMovementDirection("right");
            _currentlyMovingRight = false;
        }
    }

    private void StopMovementDirection(string direction)
    {
        if (bridgeWalker == null) return;

        // Since BridgeWalker.StopMove() stops ALL movement, we need to be careful
        // We'll only call it if no other directions are active
        bool anyMovementActive = false;

        switch (direction)
        {
            case "forward":
                anyMovementActive = _currentlyMovingBack || _currentlyMovingLeft || _currentlyMovingRight;
                break;
            case "back":
                anyMovementActive = _currentlyMovingForward || _currentlyMovingLeft || _currentlyMovingRight;
                break;
            case "left":
                anyMovementActive = _currentlyMovingForward || _currentlyMovingBack || _currentlyMovingRight;
                break;
            case "right":
                anyMovementActive = _currentlyMovingForward || _currentlyMovingBack || _currentlyMovingLeft;
                break;
        }

        if (!anyMovementActive)
        {
            bridgeWalker.StopMove();
            if (debugJoystick) Debug.Log($"[JoystickController] Stopped {direction} movement (and all movement)");
        }
        else
        {
            // Restart the movements that should still be active
            RestartActiveMovements(direction);
            if (debugJoystick) Debug.Log($"[JoystickController] Stopped {direction} movement, restarted others");
        }
    }

    private void RestartActiveMovements(string excludeDirection)
    {
        if (bridgeWalker == null) return;

        // First stop all movement
        bridgeWalker.StopMove();

        // Then restart the active ones (except the one we're stopping)
        if (_currentlyMovingForward && excludeDirection != "forward")
        {
            bridgeWalker.StartMoveUp();
        }
        if (_currentlyMovingBack && excludeDirection != "back")
        {
            bridgeWalker.StartMoveDown();
        }
        if (_currentlyMovingLeft && excludeDirection != "left")
        {
            bridgeWalker.StartMoveLeft();
        }
        if (_currentlyMovingRight && excludeDirection != "right")
        {
            bridgeWalker.StartMoveRight();
        }
    }

    private void StopAllMovement()
    {
        if (bridgeWalker == null) return;

        if (_currentlyMovingForward || _currentlyMovingBack || _currentlyMovingLeft || _currentlyMovingRight)
        {
            bridgeWalker.StopMove();
            _currentlyMovingForward = false;
            _currentlyMovingBack = false;
            _currentlyMovingLeft = false;
            _currentlyMovingRight = false;

            if (debugJoystick) Debug.Log("[JoystickController] Stopped all movement");
        }
    }

    private IEnumerator SmoothReturnToCenter()
    {
        if (joystickCenter == null) yield break;

        Vector3 startPosition = transform.position;
        Vector3 centerPosition = joystickCenter.position;

        float journey = 0f;
        while (journey <= 1f && !_isGrabbed) // Stop if grabbed again
        {
            journey += Time.deltaTime * returnSpeed;
            transform.position = Vector3.Lerp(startPosition, centerPosition, journey);
            yield return null;
        }

        // Ensure exact center position
        if (!_isGrabbed)
        {
            transform.position = joystickCenter.position;
            transform.localPosition = _joystickInitialLocalPosition;
            _currentJoystickInput = Vector2.zero;
        }
    }

    void OnDestroy()
    {
        // Unsubscribe from events
        if (_grabbable != null)
        {
            _grabbable.WhenPointerEventRaised -= HandleGrabEvent;
        }

        if (_pokeInteractable != null)
        {
            _pokeInteractable.WhenPointerEventRaised -= HandlePokeEvent;
        }
    }

    // ==== PUBLIC UTILITY METHODS ====

    public bool IsGrabbed()
    {
        return _isGrabbed;
    }

    public Vector2 GetJoystickInput()
    {
        return _currentJoystickInput;
    }

    public float GetJoystickMagnitude()
    {
        return _currentJoystickInput.magnitude;
    }

    public void ForceRelease()
    {
        if (_isGrabbed)
        {
            OnJoystickReleased();
        }
    }

    // Manual control methods (for testing)
    public void SetJoystickPosition(Vector2 input)
    {
        if (joystickCenter == null) return;

        Vector3 targetPosition = joystickCenter.position + new Vector3(input.x, 0, input.y) * joystickMaxDistance;
        transform.position = targetPosition;
        UpdateJoystickInput();
        UpdateMovementCommands();
    }
}