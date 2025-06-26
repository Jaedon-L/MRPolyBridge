using UnityEngine;

namespace Interactions
{
    public class TrainMovement : MonoBehaviour
    {
        [SerializeField] private Transform startPoint;
        [SerializeField] private Transform targetPoint;
        [SerializeField] private float acceleration = 2f;
        [SerializeField] private float maxSpeed = 50f;
        [SerializeField] private float threshold;

        private Rigidbody rb;
        private bool goingForward = true;
        private float currentSpeed = 0f;
        private Vector3 destination;

        private void Start()
        {
            rb = GetComponent<Rigidbody>();
            destination = targetPoint.position;
            transform.position = startPoint.position;
        }

        void FixedUpdate()
        {
            Vector3 direction = (destination - transform.position).normalized;
            currentSpeed = Mathf.Min(currentSpeed + acceleration * Time.fixedDeltaTime, maxSpeed); // Accelerate
            rb.linearVelocity = direction * currentSpeed;
            if (Vector3.Distance(transform.position, destination) < threshold)
            {
                goingForward = !goingForward;
                destination = goingForward ? targetPoint.position : startPoint.position;
                currentSpeed = 0f;
            }
        }

        public float pushForceMultiplier = 1.2f;

        private void OnCollisionStay(Collision collision)
        {
            var otherRb = collision.rigidbody;
            if (!otherRb) return;
            otherRb.AddForce(transform.forward * pushForceMultiplier, ForceMode.VelocityChange);
        }
    }
}