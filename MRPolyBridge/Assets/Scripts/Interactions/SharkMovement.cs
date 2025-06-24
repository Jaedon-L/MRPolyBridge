using System.Collections.Generic;
using UnityEngine;

namespace Interactions
{
    [RequireComponent(typeof(Rigidbody))]
    public class SharkMovement : MonoBehaviour
    {
        [Header("Movement Settings")] public float speed = .3f;
        public float rotationSpeed = 2f;
        public float arrivalThreshold = .1f;

        private int currentWaypointIndex;
        private Rigidbody rb;
        private Transform currentWaypoint;
        public List<Transform> waypoints;
        private bool isMoving;


        public void Initialize(List<Transform> waypoints)
        {
            rb = GetComponent<Rigidbody>();
            this.waypoints = waypoints;
            currentWaypointIndex = Random.Range(0, waypoints.Count);
            currentWaypoint = waypoints[currentWaypointIndex];
            isMoving = true;
        }

        private void FixedUpdate()
        {
            if (!isMoving) return;
            Vector3 newPosition =
                Vector3.MoveTowards(rb.position, currentWaypoint.position, speed * Time.fixedDeltaTime);
            Quaternion lookRotation = Quaternion.LookRotation(currentWaypoint.position - rb.position);
            rb.MoveRotation(Quaternion.Slerp(rb.rotation, lookRotation, rotationSpeed * Time.fixedDeltaTime));
            rb.MovePosition(newPosition);


            if (Vector3.Distance(rb.position, currentWaypoint.position) < arrivalThreshold)
            {
                currentWaypointIndex = (currentWaypointIndex + 1) % waypoints.Count;
                currentWaypoint = waypoints[currentWaypointIndex];
            }
        }

        public void SetMoving(bool moving)
        {
            isMoving = moving;
        }
    }
}