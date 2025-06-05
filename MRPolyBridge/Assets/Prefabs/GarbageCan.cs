using UnityEngine;
using Oculus.Interaction; // for Grabbable

[RequireComponent(typeof(Collider))]
public class GarbageCan : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("UI")) return; 
        // Look for a Grabbable on the hit object or any of its parents
        var grabbable = other.GetComponentInChildren<Grabbable>();
        if (grabbable != null)
        {
            // Destroy the entire GameObject that holds the Grabbable.
            Destroy(grabbable.transform.root.gameObject);
            Debug.Log($"[GarbageCan] Destroyed '{grabbable.gameObject.name}'.");
        }
    }
}
