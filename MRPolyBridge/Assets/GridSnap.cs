using UnityEngine;
using Oculus.Interaction.Grab;
using Oculus.Interaction; // for GrabInteractable

public class GridSnap : MonoBehaviour
{
    [Tooltip("Size of each grid cell.  Transform will snap to multiples of this.")]
    [SerializeField] private float gridSize = 0.5f;

    private Grabbable _grabInteractable;

    void Awake()
    {
        _grabInteractable = GetComponentInChildren<Grabbable>();
    }

    void LateUpdate()
    {
        // If it exists and is NOT currently selected (i.e. not being held), snap to grid:
        if (_grabInteractable != null && _grabInteractable.GrabPoints.Count == 0)
        {
            Vector3 raw = transform.position;
            float x = Mathf.Round(raw.x / gridSize) * gridSize;
            float y = Mathf.Round(raw.y / gridSize) * gridSize;
            float z = Mathf.Round(raw.z / gridSize) * gridSize;
            transform.position = new Vector3(x, y, z);
        }
    }
}
