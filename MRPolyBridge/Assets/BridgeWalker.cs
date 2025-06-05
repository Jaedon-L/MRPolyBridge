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

    private Rigidbody _rb;
    private Vector3 _moveDir = Vector3.zero;

    void Awake()
    {
        _rb = GetComponent<Rigidbody>();
        // // (Optionally) lock rotations so the cube doesn’t tumble
        // _rb.constraints = RigidbodyConstraints.FreezeRotationX 
        //                 | RigidbodyConstraints.FreezeRotationZ;
    }

    void FixedUpdate()
    {
        if (_moveDir != Vector3.zero)
        {
            // MovePosition ensures smooth interpolation under physics
            Vector3 newPos = _rb.position + _moveDir * (moveSpeed * Time.fixedDeltaTime);
            _rb.MovePosition(newPos);
        }
    }

    // Called by Btn_Up → EventTrigger/PointerDown
    public void StartMoveUp()
    {
        // +Z is “up” in world‐space; adjust if your forward axis is different
        _moveDir = Vector3.forward;
    }

    // Called by Btn_Down → EventTrigger/PointerDown
    public void StartMoveDown()
    {
        _moveDir = Vector3.back;
    }

    // Called by Btn_Left → EventTrigger/PointerDown
    public void StartMoveLeft()
    {
        _moveDir = Vector3.left;
    }

    // Called by Btn_Right → EventTrigger/PointerDown
    public void StartMoveRight()
    {
        _moveDir = Vector3.right;
    }

    // Called by any button’s PointerUp event
    public void StopMove()
    {
        _moveDir = Vector3.zero;
    }
}
