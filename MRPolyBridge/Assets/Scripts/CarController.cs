using UnityEngine;
using TMPro;
using UnityEngine.UI;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.SceneManagement;

public class CarController : MonoBehaviour
{
    [Header("Car Movement")]
    public GameObject car;
    public float speed = 5f;
    public float turnSpeed = 2f;

    [Header("Steering")]
    public Transform steeringWheel;
    public float maxSteeringAngle = 45f;
    public bool useLocalZAxis = true;

    [Header("UI")]
    public TextMeshProUGUI speedText;

    private bool isMoving = false;
    private bool movementAllowed = false;
    private bool isReversing = false;

    [Header("Life System")]
    public int totalLives = 3;
    public Image[] lifeImages;
    public GameObject gameOverPanel;
    public AudioSource collisionSound;
    private int currentLives;

    private bool canTakeDamage = true;
    public float damageCooldown = 3f;

    [Header("Audio")]
    public AudioSource engineSound;

    [Header("UI Controls")]
    public GameObject restartObject;

    void Start()
    {
        currentLives = totalLives;
        if (gameOverPanel != null)
            gameOverPanel.SetActive(false);

        if (engineSound != null)
            engineSound.Stop();

        if (restartObject != null)
            restartObject.SetActive(false);
    }

    void Update()
    {

        if (car != null && car.transform.position.y < -10f)
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        if (!movementAllowed || !isMoving || car == null || steeringWheel == null)
        {
            UpdateSpeedUI(0f);
            if (engineSound != null && engineSound.isPlaying) engineSound.Stop();
            return;
        }

        if (engineSound != null && !engineSound.isPlaying)
            engineSound.Play();

        Vector3 direction = isReversing ? Vector3.back : Vector3.forward;
        car.transform.Translate(direction * speed * Time.deltaTime);

        float steeringInput = GetSteeringInput();
        float targetYRotation = steeringInput * maxSteeringAngle;
        if (isReversing) targetYRotation = -targetYRotation;

        Quaternion targetRotation = Quaternion.Euler(0f, car.transform.eulerAngles.y + targetYRotation * Time.deltaTime, 0f);
        car.transform.rotation = Quaternion.Lerp(car.transform.rotation, targetRotation, Time.deltaTime * turnSpeed);

        UpdateSpeedUI(speed);
    }

    float GetSteeringInput()
    {
        float angle = useLocalZAxis ? steeringWheel.localEulerAngles.z : steeringWheel.localEulerAngles.y;
        if (angle > 180f) angle -= 360f;
        return -Mathf.Clamp(angle / maxSteeringAngle, -1f, 1f);
    }

    void UpdateSpeedUI(float currentSpeed)
    {
        if (speedText != null)
        {
            float displaySpeed = (isMoving && movementAllowed) ? currentSpeed : 0f;
            string direction = isReversing ? " (R)" : "";
            speedText.text = $"{displaySpeed:F1}{direction}";
        }
    }

    public void ToggleCarMovement()
    {
        movementAllowed = !movementAllowed;
        isMoving = movementAllowed && speed > 0f;

        if (!isMoving && engineSound != null)
            engineSound.Stop();

        Debug.Log("Toggle: movementAllowed = " + movementAllowed + ", isMoving = " + isMoving);
    }

    public void IncreaseSpeedFromPose(float increaseAmount)
    {
        speed = Mathf.Clamp(speed + increaseAmount, 0f, 100f);
        isMoving = speed > 0f && movementAllowed;

        if (!isMoving && engineSound != null)
            engineSound.Stop();

        Debug.Log("Speed increased: " + speed + ", isMoving = " + isMoving);
    }

    public void DecreaseSpeedFromPose(float decreaseAmount)
    {
        speed = Mathf.Clamp(speed - decreaseAmount, 0f, 100f);
        isMoving = speed > 0f && movementAllowed;

        if (!isMoving && engineSound != null)
            engineSound.Stop();

        Debug.Log(speed == 0f ? "Car stopped." : "Speed decreased: " + speed);
    }

    public void DriveInReverse(bool reverse)
    {
        isReversing = reverse;
        isMoving = movementAllowed && speed > 0f;
        Debug.Log("Reverse mode: " + isReversing + ", isMoving = " + isMoving);
    }

    public void DriveForward()
    {
        isReversing = false;
        isMoving = movementAllowed && speed > 0f;
        Debug.Log("Forward mode: isReversing = " + isReversing + ", isMoving = " + isMoving);
    }

    void OnCollisionEnter(Collision collision)
    {
        if (collision.gameObject.CompareTag("Obstacle") && canTakeDamage)
        {
            if (collisionSound != null)
                collisionSound.Play();

            DecreaseLife();
            StartCoroutine(DamageCooldown());
        }
    }

    void DecreaseLife()
    {
        if (currentLives <= 0) return;

        currentLives--;

        if (currentLives >= 0 && currentLives < lifeImages.Length)
            lifeImages[currentLives].enabled = false;

        Debug.Log("Life lost! Remaining: " + currentLives);

        if (currentLives <= 0)
        {
            StopCar();
            if (gameOverPanel != null)
                gameOverPanel.SetActive(true);

            if (restartObject != null)
                restartObject.SetActive(true);

            Debug.Log("Game Over!");
        }
    }

    void StopCar()
    {
        isMoving = false;
        movementAllowed = false;
        if (engineSound != null && engineSound.isPlaying)
            engineSound.Stop();
        UpdateSpeedUI(0f);
    }

    IEnumerator DamageCooldown()
    {
        canTakeDamage = false;
        yield return new WaitForSeconds(damageCooldown);
        canTakeDamage = true;
    }
}
