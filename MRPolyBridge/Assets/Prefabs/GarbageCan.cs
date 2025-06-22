using UnityEngine;
using Oculus.Interaction; // For Grabbable

[RequireComponent(typeof(Collider))]
public class GarbageCan : MonoBehaviour
{
    private void OnTriggerEnter(Collider other)
    {
        if (other.gameObject.CompareTag("UI")) return;

        GameObject target = other.transform.root.gameObject;
        var grabbable = target.GetComponentInChildren<Grabbable>();
        var buildCost = target.GetComponent<BuildCost>();
        var snap = target.GetComponent<SnapInteractable>();
        var hinge = target.GetComponent<HingeJoint>(); // If it's a beam

        if (grabbable == null) return;

        // Prevent refund if object is marked as "broken"
        bool shouldRefund = buildCost != null && !buildCost.broken;

        // === BEAM DELETION ===
        if (hinge != null)
        {
            if (shouldRefund)
            {
                BudgetManager.Instance.Refund(buildCost.GetCostPlaced());
            }

            BridgeGraph.UnregisterBeam(target);
            Destroy(target);
            Debug.Log($"[GarbageCan] Deleted Beam: {target.name}");
            return;
        }

        // === NODE DELETION ===
        if (snap != null)
        {
            int nodeId = snap.gameObject.GetInstanceID();
            if (BridgeGraph.HasBeamsAttached(nodeId))
            {
                // Prevent deletion if beams are still attached to this node
                Debug.LogWarning($"[GarbageCan] Cannot delete node '{target.name}' — beams still attached.");
                return;
            }

            if (shouldRefund)
            {
                BudgetManager.Instance.Refund(buildCost.GetCostPlaced());
            }

            Destroy(target);
            Debug.Log($"[GarbageCan] Deleted Node: {target.name}");
            return;
        }

        // === OTHER OBJECT TYPES ===
        if (shouldRefund)
        {
            BudgetManager.Instance.Refund(buildCost.GetCostPlaced());
        }

        Destroy(target);
        Debug.Log($"[GarbageCan] Deleted Generic Object: {target.name}");
    }
}
