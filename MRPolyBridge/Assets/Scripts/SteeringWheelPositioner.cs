using UnityEngine;

public class SteeringWheelPositioner : MonoBehaviour
{
    [Header("Follow Settings")]
    public float smoothFactor = 2f;
    public Transform target; // Player camera
    public Vector3 offset = new Vector3(0f, 0.8f, 0.5f);
    public Vector3 eulerOffset = new Vector3(0f, 0f, 0f);

    [Header("Control")]
    public bool freezeRotation = false;
    private SteeringWheelUse steeringWheelUse;

    private void Start()
    {
        if (target == null)
            target = Camera.main.transform;
        // Find the SteeringWheelUse on same object
        steeringWheelUse = GetComponentInChildren<SteeringWheelUse>();
        transform.position = GetTargetPos();
        transform.rotation = GetTargetRot();
    }

    private void Update()
    {
        Vector3 targetPos = GetTargetPos();
        transform.position = Vector3.Lerp(transform.position, targetPos, smoothFactor * Time.deltaTime);

        if (!freezeRotation && (steeringWheelUse == null || steeringWheelUse.NoInteractors())) // Only rotate if not frozen
        {
            Quaternion targetRot = GetTargetRot();
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRot, smoothFactor * Time.deltaTime);
        }
    }

    private Vector3 GetTargetPos()
    {
        // Position slightly in front of camera, following offset
        Vector3 forward = Vector3.ProjectOnPlane(target.forward, Vector3.up);
        Vector3 targetPos = target.position + forward * offset.z;
        targetPos.y = target.position.y + offset.y;
        targetPos.x += offset.x;
        return targetPos;
    }

    private Quaternion GetTargetRot()
    {
        // Rotate with player's Yaw + offset
        return Quaternion.Euler(
            eulerOffset.x,
            target.eulerAngles.y + eulerOffset.y,
            eulerOffset.z
        );
    }

    // Helper methods for UnityEvent
    public void PauseRotation(bool pause)
    {
        freezeRotation = pause;
    }
}
