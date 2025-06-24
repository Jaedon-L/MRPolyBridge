using UnityEngine;

public class SwingingInteraction : MonoBehaviour
{
    [SerializeField] private Transform spawnPosition;

    [SerializeField] private float speed = 5f;

    [SerializeField] private float swingLength = 5f;

    [SerializeField] private float strengthForce = 5f;

    [SerializeField] private float rotationExe = 90f;
    [SerializeField] private float initialAngle;
    [SerializeField] private bool isMovingPosition;
    [SerializeField] private AudioSource collisionSound;

    private Vector3 _fromPosition;
    private Vector3 _toPosition;
    private Quaternion _fromRotation;
    private Quaternion _toRotation;
    private Rigidbody rb;


    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        _fromPosition = transform.position - Vector3.forward * swingLength / 2f;
        _toPosition = transform.position + Vector3.forward * swingLength / 2f;

        _fromRotation = Quaternion.Euler(0f, rotationExe, -swingLength / 2f);
        _toRotation = Quaternion.Euler(0f, rotationExe, swingLength / 2f);
    }

    private void FixedUpdate()
    {
        var point = Mathf.PingPong((Time.time + initialAngle) * speed, 1f);

        if (isMovingPosition)
        {
            rb.MovePosition(Vector3.Slerp(_fromPosition, _toPosition, point));
        }
        else
        {
            rb.MoveRotation(Quaternion.Lerp(_fromRotation, _toRotation, point));
        }
    }

    private void OnCollisionEnter(Collision other)
    {
        other.rigidbody.AddForce(-other.contacts[0].point * strengthForce, ForceMode.Impulse);
        collisionSound.Play();
    }
}