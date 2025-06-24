using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Random = UnityEngine.Random;

namespace Interactions
{
    public class SharkTankController : MonoBehaviour
    {
        [SerializeField] private GameObject sharkPrefab;
        [SerializeField] private int numberOfSharks = 5;
        [SerializeField] private float tankRadius = 5f;
        [SerializeField] private float sharkSpeed = 2f;
        [SerializeField] private float jumpInterval = 2f;
        [SerializeField] private float jumpArcInterval = 2f;
        [SerializeField] private float jumpHeight = 2f;
        [SerializeField] private float jumpArcHeight = 2f;
        [SerializeField] private Transform initialJumpPosition;
        [SerializeField] private Transform jumpTargetPosition;
        [SerializeField] private int amountOfWaypoints = 5;

        private Rigidbody[] sharks;
        private Rigidbody sharkArc;
        public List<Transform> waypoints;

        private float jumpTimer;
        private float jumpArcTimer;


        private void Start()
        {
            sharks = new Rigidbody[numberOfSharks];
            waypoints = new List<Transform>();
            for (int i = 0; i < amountOfWaypoints; i++)
            {
                Vector3 position = Random.insideUnitSphere * tankRadius;
                position.y = 0f;
                position += transform.position;
                GameObject waypoint = new GameObject($"Waypoint_{i + 1}");
                waypoint.transform.SetParent(transform);
                waypoint.transform.position = position;
                waypoints.Add(waypoint.transform);
            }

            for (int i = 0; i < numberOfSharks; i++)
            {
                Vector3 position = Random.insideUnitSphere * tankRadius;
                position.y = 0f;
                sharks[i] = Instantiate(sharkPrefab, transform, false).GetComponent<Rigidbody>();
                sharks[i].gameObject.transform.localPosition = position;
                sharks[i].GetComponent<SharkMovement>().Initialize(waypoints);
            }

            sharkArc = sharks[^1];
            sharkArc.position = initialJumpPosition.position;
            sharkArc.GetComponent<SharkMovement>().enabled = false;
        }

        private void FixedUpdate()
        {
            float time = Time.fixedDeltaTime;
            jumpTimer += time;
            jumpArcTimer += time;

            if (jumpTimer >= jumpInterval)
            {
                jumpTimer = 0f;
                JumpSharks();
            }

            if (jumpArcTimer >= jumpArcInterval)
            {
                jumpArcTimer = 0f;
                JumpSharkArc();
            }
        }


        private void JumpSharks()
        {
            for (int i = 0; i < numberOfSharks - 1; i++)
            {
                sharks[i].GetComponent<SharkMovement>().SetMoving(false);
                sharks[i].MoveRotation(Quaternion.Euler(-90f, 0f, 0f));
                StartCoroutine(SimulateVerticalJump(sharks[i], jumpHeight, sharkSpeed));
            }
        }

        private IEnumerator SimulateVerticalJump(Rigidbody shark, float height, float duration)
        {
            float timePassed = 0f;
            Vector3 startPosition = shark.position;

            while (timePassed < duration)
            {
                float t = timePassed / duration;
                float currentHeight = 4 * height * t * (1 - t);
                Vector3 newPosition = startPosition;
                newPosition.y = startPosition.y + currentHeight;
                shark.MovePosition(newPosition);
                timePassed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            shark.MovePosition(startPosition);
            shark.rotation = Quaternion.identity;
            shark.GetComponent<SharkMovement>().SetMoving(true);
        }

        private void JumpSharkArc()
        {
            sharkArc.GetComponent<SharkMovement>().SetMoving(false);
            Vector3 startPosition = initialJumpPosition.position;
            Vector3 targetPosition = jumpTargetPosition.position;
            Vector3 midPoint = (startPosition + targetPosition) / 2f;
            midPoint.y += jumpArcHeight;
            sharkArc.MovePosition(startPosition);
            StartCoroutine(SimulateJumpArc(sharkArc, startPosition, midPoint, targetPosition, sharkSpeed));
        }

        private IEnumerator SimulateJumpArc(Rigidbody sharkArc, Vector3 startPosition, Vector3 midPoint,
            Vector3 targetPosition, float duration)
        {
            float timePassed = 0f;

            while (timePassed < duration)
            {
                float t = timePassed / duration;

                Vector3 position = Mathf.Pow(1 - t, 2) * startPosition +
                                   2 * (1 - t) * t * midPoint +
                                   Mathf.Pow(t, 2) * targetPosition;
                sharkArc.MovePosition(position);
                Vector3 direction = 2 * (1 - t) * (midPoint - startPosition) +
                                    2 * t * (targetPosition - midPoint);
                direction.Normalize();
                sharkArc.MoveRotation(Quaternion.LookRotation(direction));

                timePassed += Time.fixedDeltaTime;
                yield return new WaitForFixedUpdate();
            }

            sharkArc.MovePosition(targetPosition);
            this.sharkArc.rotation = Quaternion.identity;
            sharkArc.GetComponent<SharkMovement>().SetMoving(true);
        }
    }
}