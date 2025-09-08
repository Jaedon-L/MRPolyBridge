using System.Runtime.InteropServices;
using Oculus.Interaction;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

public class SpawnPointSelector : MonoBehaviour
{
    [SerializeField] private Transform controllerTransform; // XR controller or hand
    [SerializeField] private LineRenderer lineRenderer; // Visual ray
    [SerializeField] private LayerMask floorLayer; // Set to "Floor" layer
    [SerializeField] private GameObject boundaryIndicatorPrefab; // Visual marker prefab
    [SerializeField] private float rayLength = 10f;
    [SerializeField] private OVRHand rightHand; // Reference to the right hand
    [SerializeField] private Button spawnPointBtn;


    private Vector3 currentHitPoint;
    private GameObject boundaryIndicatorInstance;
    private bool pinchLocked = false; // Prevents re-triggering during continuous pinch
    private bool hasValidHit = false;
    private bool isLocked = false;

    void Start()
    {
        // spawnPointBtn.onClick.AddListener(ResetSpawnPoint);
        spawnPointBtn.GetComponentInChildren<InteractableUnityEventWrapper>().WhenSelect.AddListener(ResetSpawnPoint);

        lineRenderer.positionCount = 2;
        isLocked = true;
        lineRenderer.gameObject.SetActive(false);

        // Create indicator but hide initially
        if (boundaryIndicatorPrefab != null)
        {
            boundaryIndicatorInstance = Instantiate(boundaryIndicatorPrefab);
            boundaryIndicatorInstance.SetActive(false);
        }
    }

    void Update()
    {
        if (isLocked) return; // Stop updating if user has locked the spawn point

        Ray ray = new Ray(controllerTransform.position, controllerTransform.forward);
        
        // Always update ray origin
        lineRenderer.SetPosition(0, ray.origin);

        if (Physics.Raycast(ray, out RaycastHit hit, rayLength, ~0)) // ~0 = all layers
        {
            // Stop the ray at first hit
            lineRenderer.SetPosition(1, hit.point);

            if (((1 << hit.collider.gameObject.layer) & floorLayer) != 0)
            {
                // Hit is on a valid "floor" layer
                // currentHitPoint = hit.point;
                currentHitPoint = new Vector3(hit.point.x, 0f, hit.point.z);
                hasValidHit = true;
                boundaryIndicatorInstance.SetActive(true);
                boundaryIndicatorInstance.transform.position = currentHitPoint;
            }
            else
            {
                // Hit is not on floor layer
                hasValidHit = false;
                boundaryIndicatorInstance.SetActive(false);
            }
        }

        SelectSpawnPoint();
    }

    /// <summary>
    /// Handles the hand pich interaction to confirm spawn position.
    /// </summary>
    private void SelectSpawnPoint()
    {
        if (rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index))
        {
            if (!pinchLocked)
            {
                LockSpawnPoint();
                pinchLocked = true;
            }
        }
        else
        {
            pinchLocked = false;
        }
    }

    /// <summary>
    /// Locks the current hit point and stops further updates to the indicator.
    /// </summary>
    public void LockSpawnPoint()
    {
        if (!hasValidHit) return;

        isLocked = true;
        Debug.Log("Spawn Point Locked At: " + currentHitPoint);
        lineRenderer.gameObject.SetActive(false);
    }

    /// <summary>
    /// Allows the reset of the spawn point in case the user needs to change the spawn position while playing.
    /// </summary>
    public void ResetSpawnPoint()
    {
        isLocked = false;
        lineRenderer.gameObject.SetActive(true);
    }

    /// <summary>
    /// Returns the confirmed spawn point.
    /// </summary>
    public Vector3 GetLockedSpawnPoint()
    {
        return currentHitPoint;
    }
}
