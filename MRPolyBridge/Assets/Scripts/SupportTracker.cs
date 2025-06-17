using UnityEngine;
using Oculus.Interaction;

public class SupportTracker : MonoBehaviour
{
    private SnapInteractable _nodeA;
    private SnapInteractable _nodeB;

    public void Initialize(SnapInteractable nodeA, SnapInteractable nodeB)
    {
        _nodeA = nodeA;
        _nodeB = nodeB;
    }

    void OnDestroy()
    {
        if (_nodeA != null)
            BridgeGraph.UnmarkNodeSupported(_nodeA);

        if (_nodeB != null)
            BridgeGraph.UnmarkNodeSupported(_nodeB);
    }
}
