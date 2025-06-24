using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction; // for SnapInteractable

[RequireComponent(typeof(Rigidbody))]
public class BridgeWalker : MonoBehaviour
{
    [Header("Movement Settings")]
    [Tooltip("How fast (m/s) the cube moves while a button is held.")]
    public float moveSpeed = 2f;

    [Header("Movement State")]
    [SerializeField] private bool debugMovement = true;

    private Rigidbody _rb;
    private Vector3 _moveDir = Vector3.zero;

    // Track individual direction states for diagonal movement
    private bool _movingForward = false;
    private bool _movingBack = false;
    private bool _movingLeft = false;
    private bool _movingRight = false;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // // (Optionally) lock rotations so the cube doesn’t tumble
        // _rb.constraints = RigidbodyConstraints.FreezeRotationX 
        //                 | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        // Calculate combined movement direction
        UpdateMovementDirection();

        if (_moveDir != Vector3.zero)
        {
            // Normalize diagonal movement so it doesn't go faster
            Vector3 normalizedDir = _moveDir.normalized;

            // MovePosition ensures smooth interpolation under physics
            Vector3 newPos = _rb.position + _moveDir * (moveSpeed * Time.fixedDeltaTime);
            _rb.MovePosition(newPos);

            if (debugMovement)
            {
                Debug.Log($"Moving: {normalizedDir} at speed {moveSpeed}");
                Debug.Log($"Moving from {_rb.position} to {newPos}");
            }
        }
    }

    private void UpdateMovementDirection()
    {
        _moveDir = Vector3.zero;

        if (_movingForward) _moveDir += Vector3.forward;
        if (_movingBack) _moveDir += Vector3.back;
        if (_movingLeft) _moveDir += Vector3.left;
        if (_movingRight) _moveDir += Vector3.right;

        // Debug to see what's happening
        if (debugMovement && _moveDir != Vector3.zero)
        {
            Debug.Log($"Movement direction: {_moveDir}, Normalized: {_moveDir.normalized}");
        }
    }

    // Called by Btn_Up → EventTrigger/PointerDown
    public void StartMoveUp()
    {
        // +Z is “up” in world‐space; adjust if your forward axis is different
        //_moveDir = Vector3.forward;
        _movingForward = true;
        if (debugMovement) Debug.Log("Started moving forward");
    }

    // Called by Btn_Down → EventTrigger/PointerDown
    public void StartMoveDown()
    {
        //_moveDir = Vector3.back;
        _movingBack = true;
        if (debugMovement) Debug.Log("Started moving back");
    }

    // Called by Btn_Left → EventTrigger/PointerDown
    public void StartMoveLeft()
    {
        //_moveDir = Vector3.left;
        _movingLeft = true;
        if (debugMovement) Debug.Log("Started moving left");
    }

    // Called by Btn_Right → EventTrigger/PointerDown
    public void StartMoveRight()
    {
        //_moveDir = Vector3.right;
        _movingRight = true;
        if (debugMovement) Debug.Log("Started moving right");
    }

    // Called by any button’s PointerUp event
    public void StopMove()
    {
        _movingForward = false;
        _movingBack = false;
        _movingLeft = false;
        _movingRight = false;
        _moveDir = Vector3.zero;

        if (debugMovement) Debug.Log("Stopped all movement");
    }

    // Utility methods
    public bool IsMoving()
    {
        return _moveDir != Vector3.zero;
    }

    public Vector3 GetMovementDirection()
    {
        return _moveDir.normalized;
    }
}
