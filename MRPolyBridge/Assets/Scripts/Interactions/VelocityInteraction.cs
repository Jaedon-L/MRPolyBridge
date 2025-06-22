using UnityEngine;

public class VelocityInteraction : MonoBehaviour
{
    public float speedBoost = 5f;

    private void OnTriggerEnter(Collider collider)
    {
        var rb = collider.attachedRigidbody;
        if (rb != null)
        {
            rb.AddForce(rb.transform.forward * speedBoost, ForceMode.Impulse);
        }
    }
}