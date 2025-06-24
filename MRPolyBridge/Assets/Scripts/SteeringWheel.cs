using UnityEngine;

public class SteeringWheel : MonoBehaviour
{
    public float resetSpeed = 2f;
    public bool useLocalZAxis = true;

    private bool isResetting = false;

    void Update()
    {
        if (isResetting)
        {
            Vector3 currentEuler = transform.localEulerAngles;
            float currentAngle = useLocalZAxis ? currentEuler.z : currentEuler.y;

            if (currentAngle > 180f) currentAngle -= 360f;

            float newAngle = Mathf.Lerp(currentAngle, 0f, Time.deltaTime * resetSpeed);
            if (Mathf.Abs(newAngle) < 0.5f)
            {
                newAngle = 0f;
                isResetting = false;
            }

            if (useLocalZAxis)
                transform.localEulerAngles = new Vector3(currentEuler.x, currentEuler.y, newAngle);
            else
                transform.localEulerAngles = new Vector3(currentEuler.x, newAngle, currentEuler.z);
        }
    }


    public void OnGrabReleased()
    {
        isResetting = true;
    }
}
