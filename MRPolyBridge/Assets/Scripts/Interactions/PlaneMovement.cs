using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(Rigidbody))]
public class PlaneMovement : MonoBehaviour
{
    [Header("Waypoints")] public List<Transform> waypoints;

    [Header("Movement Settings")] public float speed = 10f;
    public float rotationSpeed = 5f;
    public float arrivalThreshold = 1f;

    private int currentWaypointIndex = 0;
    private Rigidbody rb;
    private Transform currentWaypoint;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.useGravity = false;
        currentWaypoint = waypoints[currentWaypointIndex];
    }

    private void FixedUpdate()
    {
        Vector3 direction = (currentWaypoint.position - rb.position).normalized;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRotation, rotationSpeed * Time.fixedDeltaTime));
        rb.linearVelocity = direction * (speed * Time.fixedDeltaTime);
        float distance = Vector3.Distance(rb.position, currentWaypoint.position);
        if (distance < arrivalThreshold)
        {
            currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
            currentWaypoint = waypoints[currentWaypointIndex];
        }
    }
}