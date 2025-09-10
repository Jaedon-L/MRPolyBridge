using UnityEngine;

[AddComponentMenu("Vehicle/BoostPad")]
public class VelocityInteraction : MonoBehaviour
{
    [Tooltip("Base boost speed (m/s).")]
    public float boostSpeed = 50f;

    [Tooltip("Extra upward lift applied when boosting.")]
    public float upwardBoost = 5f;

    [Tooltip("Optional target transform that defines launch direction.")]
    public Transform target;

    private void OnTriggerEnter(Collider collider)
    {
        // Check for the PrometeoCarController
        var car = collider.GetComponentInParent<PrometeoCarController>();
        if (car != null)
        {
            Rigidbody rb = car.GetComponent<Rigidbody>();
            if (rb != null)
            {
                // Decide direction
                Vector3 direction;
                if (target != null)
                {
                    direction = (target.position - transform.position).normalized;
                }
                else
                {
                    direction = transform.forward;
                }

                // Add upward lift
                direction = (direction + Vector3.up * upwardBoost * 0.01f).normalized;

                // Override velocity instead of just nudging
                rb.linearVelocity = direction * boostSpeed;

                Debug.Log($"BoostPad: Car boosted toward {direction} at speed {boostSpeed}");
            }
        }
    }
}
