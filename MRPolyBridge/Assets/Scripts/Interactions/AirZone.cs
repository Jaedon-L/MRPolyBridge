using UnityEngine;

public class AirZone : MonoBehaviour
{
    [Header("Air Force Settings")] public Vector3 airDirection = Vector3.up;
    public float airStrength = 10f;

    private void OnTriggerStay(Collider collider)
    {
        var rb = collider.attachedRigidbody;
        if (rb != null)
        {
            rb.AddForce(airDirection.normalized * airStrength, ForceMode.Force);
        }
    }
}