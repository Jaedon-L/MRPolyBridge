using Oculus.Interaction;
using Unity.Mathematics;
using UnityEngine;


public class HandPinchDetection : MonoBehaviour
{
    [SerializeField] private Rigidbody snapAreaRb;
    private bool _hasPinched;
    private bool _isIndexFingerPinching;
    private float _pinchStrength;

    [Header("Prefabs")]
    [SerializeField] private GameObject snapInteractablePrefab;
    [SerializeField] private GameObject woodBeamPrefab;
    [SerializeField] private GameObject edgePreviewPrefab;

    [Header("OVRHand data")]
    [SerializeField] private OVRSkeleton leftSkeleton;
    [SerializeField] private OVRHand lefthand;
    [SerializeField] private OVRSkeleton rightSkeleton;
    [SerializeField] private OVRHand rightHand;
    private OVRHand.TrackingConfidence _confidence;

    [Header("Bridge Settings")]
    [SerializeField] private float breakForceThreshold = 1000f;

    // Internal state:
    private bool leftPinchGrabbed;      // for spawning nodes
    private bool rightPinchActive;      // for making edges
    private SnapInteractable firstNode;
    private Transform firstNodeTransform;

    private GameObject currentPreviewLine;
    private LineRenderer currentLineRenderer;

    void Update()
    {
        HandleLeftPinch();
        HandleRightPinch();
    }

    #region Left‐Hand: Spawn Nodes
    void HandleLeftPinch()
    {
        bool isPinching = lefthand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        var confidence = lefthand.GetFingerConfidence(OVRHand.HandFinger.Index);

        if (!_isIndexFingerPinching && isPinching && confidence == OVRHand.TrackingConfidence.High)
        {
            _isIndexFingerPinching = true;
            SpawnSnapNodeAtLeftIndexTip();
        }
        else if (_isIndexFingerPinching && !isPinching)
        {
            _isIndexFingerPinching = false;
        }
    }

    void SpawnSnapNodeAtLeftIndexTip()
    {
        // Find the OVRSkeleton bone for left index tip:
        Transform tip = FindIndexTip(leftSkeleton);
        if (tip == null) return;

        var go = Instantiate(snapInteractablePrefab, tip.position, Quaternion.identity);
        var si = go.GetComponentInChildren<SnapInteractable>();
        si.InjectRigidbody(snapAreaRb);
        // Optionally parent or tag it as a “BridgeNode”
    }
    #endregion

    #region Right‐Hand: Create Edge/Beam
    void HandleRightPinch()
    {
        bool isPinching = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        var confidence = rightHand.GetFingerConfidence(OVRHand.HandFinger.Index);

        if (!rightPinchActive && isPinching && confidence == OVRHand.TrackingConfidence.High)
        {
            rightPinchActive = true;
            StartSelectingFirstNode();
        }
        else if (rightPinchActive && isPinching && confidence == OVRHand.TrackingConfidence.High)
        {
            UpdateSelectingEdgePreview();
        }
        else if (rightPinchActive && (!isPinching || confidence != OVRHand.TrackingConfidence.High))
        {
            rightPinchActive = false;
            EndSelectingSecondNodeAndPlaceBeam();
            EndConnectionPreview();
        }
    }

    void StartSelectingFirstNode()
    {
        // Make sure the skeleton data is ready before picking a node
        if (rightSkeleton == null || !rightSkeleton.IsDataValid || !rightSkeleton.IsDataHighConfidence)
        {
            Debug.LogWarning("[HandPinchDetection] Right skeleton not ready. Try again next frame.");
            return;
        }
        // Raycast or “closest‐node” logic to pick firstNode:
        firstNode = PickClosestNodeToIndexTip(rightSkeleton);
        if (firstNode != null)
        {
            firstNodeTransform = firstNode.transform;
            // Optionally highlight it (e.g., change material color) to show “selected.”
            BeginConnectionPreview(firstNodeTransform);
        }
    }

    void UpdateSelectingEdgePreview()
    {
        if (firstNodeTransform == null) return;
        Transform tip = FindIndexTip(rightSkeleton);
        if (tip == null) return;

        if (currentLineRenderer != null)
        {
            currentLineRenderer.SetPosition(0, firstNodeTransform.position);
            currentLineRenderer.SetPosition(1, tip.position);
        }
    }

    void EndSelectingSecondNodeAndPlaceBeam()
    {
        if (firstNodeTransform == null) return;
        if (rightSkeleton == null || !rightSkeleton.IsDataValid || !rightSkeleton.IsDataHighConfidence)
        {
            Debug.LogWarning("[HandPinchDetection] Right skeleton lost tracking; canceling edge.");
            firstNode = null;
            firstNodeTransform = null;
            return;
        }

        // Find whichever node is now closest under the finger as secondNode:
        SnapInteractable secondNode = PickClosestNodeToIndexTip(rightSkeleton);

        if (secondNode != null && secondNode != firstNode)
        {
            PlaceWoodBetween(firstNodeTransform, secondNode.transform);
        }

        // Clear selection state
        firstNode = null;
        firstNodeTransform = null;
    }

    void BeginConnectionPreview(Transform startPoint)
    {
        currentPreviewLine = Instantiate(edgePreviewPrefab);
        currentLineRenderer = currentPreviewLine.GetComponent<LineRenderer>();
        currentLineRenderer.positionCount = 2;
        currentLineRenderer.SetPosition(0, startPoint.position);
        currentLineRenderer.SetPosition(1, startPoint.position);
    }

    void EndConnectionPreview()
    {
        if (currentPreviewLine != null)
            Destroy(currentPreviewLine);
        currentPreviewLine = null;
        currentLineRenderer = null;
    }

    void PlaceWoodBetween(Transform A, Transform B)
    {
        Vector3 pA = A.position;
        Vector3 pB = B.position;
        Vector3 dir = (pB - pA).normalized;
        float dist = Vector3.Distance(pA, pB);
        Vector3 mid = (pA + pB) * 0.5f;

        // Rotate so that local +X points from A→B
        Quaternion rot = Quaternion.FromToRotation(Vector3.right, dir);

        // Instantiate & scale the beam so its local‐X length = dist:
        GameObject beam = Instantiate(woodBeamPrefab, mid, rot);
        Vector3 sc = beam.transform.localScale;
        beam.transform.localScale = new Vector3(dist, sc.y, sc.z);

        // If you want physics now, ensure the woodBeamPrefab has a Rigidbody.
        Rigidbody beamRb = beam.GetComponent<Rigidbody>();
        if (beamRb != null)
        {
            // ---- NEW: compute half‐length in world units (beam’s local X goes from –halfLen to +halfLen)
            float halfLen = 0.5f;

            // ---- Create Joint A at the beam’s LEFT end (−halfLen in local space), connected to A
            {
                // Add manual‐break behavior so we can later check for excessive loads
                var manualBreak = beam.AddComponent<BridgeBeamManualBreak>();
                manualBreak.breakForceThreshold = breakForceThreshold;


                var jointA = beam.AddComponent<ConfigurableJoint>();
                jointA.connectedBody = A.GetComponent<Rigidbody>();
                jointA.autoConfigureConnectedAnchor = false;
                // jointA.axis = Vector3.up;

                // Place the joint’s anchor at the beam’s local (−halfLen, 0, 0):
                jointA.anchor = new Vector3(-halfLen, 0f, 0f);

                // Connect to Node A’s pivot (assumes Node A’s Rigidbody pivot is exactly at A.position):
                jointA.connectedAnchor = Vector3.zero;

                // Lock all but X motion:
                jointA.xMotion = ConfigurableJointMotion.Locked;
                jointA.yMotion = ConfigurableJointMotion.Limited;
                jointA.zMotion = ConfigurableJointMotion.Locked;
                jointA.angularXMotion = ConfigurableJointMotion.Locked;
                jointA.angularYMotion = ConfigurableJointMotion.Locked;
                jointA.angularZMotion = ConfigurableJointMotion.Locked;

                // Set a limit = halfLen, so rest length is 2×halfLen = dist
                SoftJointLimit limitA = new SoftJointLimit { limit = 0.025f };
                jointA.linearLimit = limitA;

                // Tweak spring/damper for stability
                SoftJointLimitSpring springA = new SoftJointLimitSpring { spring = 2000f, damper = 1000f };
                jointA.linearLimitSpring = springA;
            }

            // ---- Create Joint B at the beam’s RIGHT end (+halfLen in local space), connected to B
            {
                var jointB = beam.AddComponent<ConfigurableJoint>();
                jointB.connectedBody = B.GetComponent<Rigidbody>();
                jointB.autoConfigureConnectedAnchor = false;

                // Place the joint’s anchor at the beam’s local (+halfLen, 0, 0):
                jointB.anchor = new Vector3(+halfLen, 0f, 0f);

                // Connect to Node B’s pivot:
                jointB.connectedAnchor = Vector3.zero;
                // jointB.axis = Vector3.up;

                // Lock all but X motion:
                jointB.xMotion = ConfigurableJointMotion.Locked;
                jointB.yMotion = ConfigurableJointMotion.Limited;
                jointB.zMotion = ConfigurableJointMotion.Locked;
                jointB.angularXMotion = ConfigurableJointMotion.Locked;
                jointB.angularYMotion = ConfigurableJointMotion.Locked;
                jointB.angularZMotion = ConfigurableJointMotion.Locked;

                SoftJointLimit limitB = new SoftJointLimit { limit = 0.025f };
                jointB.linearLimit = limitB;

                SoftJointLimitSpring springB = new SoftJointLimitSpring { spring = 2000f, damper = 1000f };
                jointB.linearLimitSpring = springB;
            }
        }
    }
    #endregion

    #region Utility Methods
    Transform FindIndexTip(OVRSkeleton skeleton)
    {
        if (skeleton == null || !skeleton.IsDataValid || !skeleton.IsDataHighConfidence)
            return null;

        foreach (var b in skeleton.Bones)
        {
            // Match any GameObject name ending in "IndexTip"
            if (b.Transform.name.EndsWith("IndexTip"))
                return b.Transform;
        }

        Debug.LogWarning($"[HandPinchDetection] Couldn't find a bone containing 'IndexTip' under {skeleton.name}");
        return null;
    }

    SnapInteractable PickClosestNodeToIndexTip(OVRSkeleton skeleton)
    {
        Transform tip = FindIndexTip(skeleton);
        if (tip == null) return null;

        float bestDist = float.MaxValue;
        SnapInteractable bestNode = null;

        // Use the new FindObjectsByType API (non‐sorting) to find all SnapInteractables:
        foreach (var node in Object.FindObjectsByType<SnapInteractable>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(node.transform.position, tip.position);
            if (d < bestDist && d < 0.2f) // nodes within 20 cm
            {
                bestDist = d;
                bestNode = node;
            }
        }
        return bestNode;
    }
    #endregion
}

/// <summary>
/// Attached to each beam; checks all ConfigurableJoints’ reaction forces each FixedUpdate.
/// If any exceed breakForceThreshold, that joint is destroyed (simulating a “break under too much load”).
/// </summary>
public class BridgeBeamManualBreak : MonoBehaviour
{
    [HideInInspector] public float breakForceThreshold = 4000f;

    void FixedUpdate()
    {
        // Look at each ConfigurableJoint on this beam:
        var joints = GetComponents<ConfigurableJoint>();
        foreach (var j in joints)
        {
            Vector3 reaction = j.currentForce;
            float mag = reaction.magnitude;

            if (mag > breakForceThreshold)
            {
                Debug.Log($"[BridgeBeamManualBreak] Breaking joint on {gameObject.name} at force {mag:F1} N");
                // 

                // If you want the entire beam to vanish when one joint breaks, uncomment:Destroy(j);
                // Destroy(gameObject);

                // Don’t check any further joints this frame
                return;
            }
        }
    }
}