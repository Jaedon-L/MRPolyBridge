using System.Collections;
using System.Collections.Generic;
using Oculus.Interaction;
using Unity.Mathematics;
using UnityEngine;

public class HandPinchDetection : MonoBehaviour
{
    [Header("Node (Snap) Settings")]
    [SerializeField] private Rigidbody snapAreaRb;
    private bool _isIndexFingerPinching;

    [Header("Prefab References")]
    [SerializeField] private GameObject snapInteractablePrefab; // node prefab
    [SerializeField] private GameObject woodBeamPrefab;         // main beam prefab
    [SerializeField] private GameObject supportBeamPrefab;      // support‐mode prefab
    [SerializeField] private GameObject edgePreviewPrefab;

    [Header("OVRHand data")]
    [SerializeField] private OVRSkeleton leftSkeleton;
    [SerializeField] private OVRHand lefthand;
    [SerializeField] private OVRSkeleton rightSkeleton;
    [SerializeField] private OVRHand rightHand;

    [Header("Bridge Settings")]
    [SerializeField] private float breakForceThreshold = 15f;
    [SerializeField] private float breakTorqueThreshold = 8f;

    [Header("Support Settings")]
    [SerializeField] private float supportBonusForce = 2f;
    [SerializeField] private float supportBonusTorque = 1f;

    [Header("Snap Settings")]
    [Tooltip("Size of each grid cell. Nodes will land on the nearest multiple of this in X, Y, and Z.")]
    [SerializeField] private float gridSize = 0.5f;

    // Internal state / mode flags:
    private bool buildingModeEnabled = false;  // “Build” on/off
    private bool supportModeEnabled = false;   // whether right pinch places support
    private bool leftPinchEnabled = false;     // allow left‐hand node spawning?
    private bool rightPinchEnabled = false;    // allow right‐hand beam placement?
    private bool rightPinchActive = false;

    // Runtime variables:
    private SnapInteractable firstNode;
    private Transform firstNodeTransform;

    private GameObject currentPreviewLine;
    private LineRenderer currentLineRenderer;

    void Update()
    {
        if (!buildingModeEnabled) return;

        if (leftPinchEnabled)
        {
            HandleLeftPinch();
        }

        if (rightPinchEnabled)
        {
            HandleRightPinch();
        }
    }

    #region Public Toggle Methods (UI Buttons)

    public void ToggleBuildingMode()
    {
        buildingModeEnabled = !buildingModeEnabled;
        Debug.Log($"[ToggleBuildingMode] Building Mode is now {(buildingModeEnabled ? "ON" : "OFF")}");

        if (buildingModeEnabled)
        {
            leftPinchEnabled = true;
            rightPinchEnabled = true;
        }
        else
        {
            if (currentPreviewLine != null)
            {
                Destroy(currentPreviewLine);
                currentPreviewLine = null;
                currentLineRenderer = null;
            }
            firstNode = null;
            firstNodeTransform = null;
            Debug.Log("[ToggleBuildingMode] Cleared selection and preview.");
        }
    }

    public void ToggleSupportMode()
    {
        supportModeEnabled = !supportModeEnabled;
        Debug.Log($"[ToggleSupportMode] Support Mode is now {(supportModeEnabled ? "ON" : "OFF")}");
    }

    public void ToggleLeftPinchEnabled()
    {
        leftPinchEnabled = !leftPinchEnabled;
        Debug.Log($"[ToggleLeftPinchEnabled] Left Pinch (node spawn) is now {(leftPinchEnabled ? "ENABLED" : "DISABLED")}");
    }

    public void ToggleRightPinchEnabled()
    {
        rightPinchEnabled = !rightPinchEnabled;
        Debug.Log($"[ToggleRightPinchEnabled] Right Pinch (beam/place) is now {(rightPinchEnabled ? "ENABLED" : "DISABLED")}");
    }

    #endregion

    #region Left‐Hand: Spawn Nodes

    private void HandleLeftPinch()
    {
        bool isPinching = lefthand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        var confidence = lefthand.GetFingerConfidence(OVRHand.HandFinger.Index);

        if (!_isIndexFingerPinching && isPinching && confidence == OVRHand.TrackingConfidence.High)
        {
            _isIndexFingerPinching = true;
            Debug.Log("[HandleLeftPinch] Left pinch start.");
            SpawnSnapNodeAtLeftIndexTip();
        }
        else if (_isIndexFingerPinching && !isPinching)
        {
            _isIndexFingerPinching = false;
            Debug.Log("[HandleLeftPinch] Left pinch release.");
        }
    }

    private void SpawnSnapNodeAtLeftIndexTip()
    {
        Transform tip = FindIndexTip(leftSkeleton);
        if (tip == null)
        {
            Debug.LogWarning("[SpawnSnapNode] Could not find left index tip.");
            return;
        }

        // Snap tip position to nearest grid cell in X, Y, and Z:
        Vector3 raw = tip.position;
        float snappedX = Mathf.Round(raw.x / gridSize) * gridSize;
        float snappedY = Mathf.Round(raw.y / gridSize) * gridSize; // ← now snapping Y as well
        float snappedZ = Mathf.Round(raw.z / gridSize) * gridSize;

        Vector3 spawnPos = new Vector3(snappedX, snappedY, snappedZ);
        Debug.Log($"[SpawnSnapNode] raw tip={raw:F3} → snapped to {spawnPos:F3}");

        GameObject go = Instantiate(snapInteractablePrefab, spawnPos, Quaternion.identity);
        var si = go.GetComponentInChildren<SnapInteractable>();
        si.InjectRigidbody(snapAreaRb);
        go.tag = "BridgeNode";
        Debug.Log($"[SpawnSnapNode] Spawned node at {spawnPos}");
        
    }

    #endregion

    #region Right‐Hand: Create Edge/Beam (Main vs. Support)

    private void HandleRightPinch()
    {
        bool isPinching = rightHand.GetFingerIsPinching(OVRHand.HandFinger.Index);
        var confidence = rightHand.GetFingerConfidence(OVRHand.HandFinger.Index);

        if (!rightPinchActive && isPinching && confidence == OVRHand.TrackingConfidence.High)
        {
            rightPinchActive = true;
            Debug.Log("[HandleRightPinch] Right pinch start.");
            StartSelectingFirstNode();
        }
        else if (rightPinchActive && isPinching && confidence == OVRHand.TrackingConfidence.High)
        {
            UpdateSelectingEdgePreview();
        }
        else if (rightPinchActive && (!isPinching || confidence != OVRHand.TrackingConfidence.High))
        {
            rightPinchActive = false;
            Debug.Log("[HandleRightPinch] Right pinch release.");
            EndSelectingSecondNodeAndPlaceBeam();
            EndConnectionPreview();
        }
    }

    private void StartSelectingFirstNode()
    {
        if (rightSkeleton == null || !rightSkeleton.IsDataValid || !rightSkeleton.IsDataHighConfidence)
        {
            Debug.LogWarning("[StartSelectingFirstNode] Right skeleton not ready.");
            return;
        }

        firstNode = PickClosestNodeToIndexTip(rightSkeleton);
        if (firstNode != null)
        {
            firstNodeTransform = firstNode.transform;
            Debug.Log($"[StartSelectingFirstNode] Selected first node at {firstNodeTransform.position}");
            BeginConnectionPreview(firstNodeTransform);
        }
        else
        {
            Debug.Log("[StartSelectingFirstNode] No node near right index tip.");
        }
    }

    private void UpdateSelectingEdgePreview()
    {
        if (firstNodeTransform == null) return;
        Transform tip = FindIndexTip(rightSkeleton);
        if (tip == null) return;

        if (currentLineRenderer != null)
        {
            currentLineRenderer.SetPosition(0, firstNodeTransform.position);
            currentLineRenderer.SetPosition(1, tip.position);
            // Debug.Log($"[UpdateSelectingEdgePreview] Preview from {firstNodeTransform.position} to {tip.position}");
        }
    }

    private void EndSelectingSecondNodeAndPlaceBeam()
    {
        if (firstNodeTransform == null) return;
        if (rightSkeleton == null || !rightSkeleton.IsDataValid || !rightSkeleton.IsDataHighConfidence)
        {
            Debug.LogWarning("[EndSelectingSecondNode] Right skeleton lost tracking.");
            firstNode = null;
            firstNodeTransform = null;
            return;
        }

        SnapInteractable secondNode = PickClosestNodeToIndexTip(rightSkeleton);
        if (secondNode != null && secondNode != firstNode)
        {
            Debug.Log($"[EndSelectingSecondNode] Placing beam between {firstNodeTransform.position} and {secondNode.transform.position}");
            if (supportModeEnabled)
                PlaceSupportBetween(firstNodeTransform, secondNode.transform);
            else
                PlaceMainBeamBetween(firstNodeTransform, secondNode.transform);
        }
        else
        {
            Debug.Log("[EndSelectingSecondNode] No valid second node.");
        }

        firstNode = null;
        firstNodeTransform = null;
    }

    private void BeginConnectionPreview(Transform startPoint)
    {
        currentPreviewLine = Instantiate(edgePreviewPrefab);
        currentLineRenderer = currentPreviewLine.GetComponent<LineRenderer>();
        currentLineRenderer.positionCount = 2;
        currentLineRenderer.SetPosition(0, startPoint.position);
        currentLineRenderer.SetPosition(1, startPoint.position);
        Debug.Log($"[BeginConnectionPreview] Started preview at {startPoint.position}");
    }

    private void EndConnectionPreview()
    {
        if (currentPreviewLine != null)
        {
            Destroy(currentPreviewLine);
            Debug.Log("[EndConnectionPreview] Destroyed preview line.");
        }
        currentPreviewLine = null;
        currentLineRenderer = null;
    }

    #endregion

    #region Bridge Beam & Support Placement

    private void PlaceMainBeamBetween(Transform A, Transform B)
    {
        Debug.Log("[PlaceMainBeam] Spawning main wood beam.");
        SpawnBeam(A, B, woodBeamPrefab, isSupport: false);
    }

    private void PlaceSupportBetween(Transform A, Transform B)
    {
        Debug.Log("[PlaceSupportBeam] Spawning support beam.");
        SpawnBeam(A, B, supportBeamPrefab, isSupport: true);
    }

    private void SpawnBeam(Transform A, Transform B, GameObject beamPrefab, bool isSupport)
    {
        Vector3 pA = A.position;
        Vector3 pB = B.position;
        float dist = Vector3.Distance(pA, pB);
        Vector3 mid = (pA + pB) * 0.5f;

        Vector3 rawDir = (pB - pA).normalized;
        Quaternion rawRot;
        if (isSupport)
        {
            // full 3D rotation for support beams
            rawRot = Quaternion.FromToRotation(Vector3.right, rawDir);

        }
        else
        {
            // flatten X‐axis rotation for main beams
            rawRot = Quaternion.FromToRotation(Vector3.right, rawDir);
            Vector3 e = rawRot.eulerAngles;
            e.x = 0f;
            rawRot = Quaternion.Euler(e);
        }

        Debug.Log($"[SpawnBeam] mid={mid}, dist={dist}, finalRot={rawRot.eulerAngles}");

        GameObject beam = Instantiate(beamPrefab, mid, rawRot);
        beam.transform.localScale = new Vector3(dist,
                                                beam.transform.localScale.y,
                                                beam.transform.localScale.z);

        Rigidbody beamRb = beam.GetComponent<Rigidbody>();
        if (beamRb == null)
        {
            beamRb = beam.AddComponent<Rigidbody>();
            Debug.Log("[SpawnBeam] Added Rigidbody to beam.");
        }

        if (isSupport)
        {
            beamRb.isKinematic = true;
            Debug.Log("[SpawnBeam] Support beam remains kinematic.");
            // beam.AddComponent<SupportTracker>().enabled = true;
        }
        else
        {
            beamRb.isKinematic = false; // main beams are dynamic
            Debug.Log("[SpawnBeam] Main beam is dynamic (isKinematic=false).");
        }

        // Attach hinge joints
        AttachHingeJoints(beam, A.GetComponent<Rigidbody>(), B.GetComponent<Rigidbody>());
        // 2) Register this beam in BridgeGraph
        var nodeA = A.GetComponent<SnapInteractable>();
        var nodeB = B.GetComponent<SnapInteractable>();
        BridgeGraph.RegisterBeam(beam, nodeA, nodeB);

        // 3) If this is a SUPPORT beam, immediately mark its two endpoint nodes:
        if (isSupport)
        {
            BridgeGraph.MarkNodeSupported(nodeA);
            BridgeGraph.MarkNodeSupported(nodeB);

            // 4) Add SupportCleanup so we unmark nodes when this support is destroyed
            var cleanup = beam.AddComponent<SupportTracker>();
            cleanup.Initialize(nodeA, nodeB);
        }

    }

    private void AttachHingeJoints(GameObject beam, Rigidbody rbA, Rigidbody rbB)
    {
        float halfLen = 0.5f;

        // Hinge Joint A
        var hingeA = beam.AddComponent<HingeJoint>();
        hingeA.connectedBody = rbA;
        hingeA.autoConfigureConnectedAnchor = false;
        hingeA.anchor = new Vector3(-halfLen, 0f, 0f);
        hingeA.connectedAnchor = rbA.transform.InverseTransformPoint(beam.transform.TransformPoint(new Vector3(-halfLen, 0f, 0f)));
        hingeA.axis = Vector3.up;
        hingeA.useSpring = false;
        hingeA.useLimits = true;
        hingeA.spring = new JointSpring
        {
            spring = 20f,
            damper = 10f,
            targetPosition = 0f
        };
        hingeA.limits = new JointLimits
        {
            min = -1f,
            max = 1f,
            bounciness = 0f
        };
        hingeA.breakForce = breakForceThreshold;
        hingeA.breakTorque = breakTorqueThreshold;
        Debug.Log($"[AttachHingeJoints] HingeA attached with breakForce={breakForceThreshold}, breakTorque={breakTorqueThreshold}");

        // Hinge Joint B
        var hingeB = beam.AddComponent<HingeJoint>();
        hingeB.connectedBody = rbB;
        hingeB.autoConfigureConnectedAnchor = false;
        hingeB.anchor = new Vector3(+halfLen, 0f, 0f);
        hingeB.connectedAnchor = rbB.transform.InverseTransformPoint(beam.transform.TransformPoint(new Vector3(+halfLen, 0f, 0f)));
        hingeB.axis = Vector3.up;
        hingeB.useSpring = false;
        hingeB.useLimits = true;
        hingeB.spring = new JointSpring
        {
            spring = 20f,
            damper = 10f,
            targetPosition = 0f
        };
        hingeB.limits = new JointLimits
        {
            min = -1f,
            max = 1f,
            bounciness = 0f
        };
        hingeB.breakForce = breakForceThreshold;
        hingeB.breakTorque = breakTorqueThreshold;
        Debug.Log($"[AttachHingeJoints] HingeB attached with breakForce={breakForceThreshold}, breakTorque={breakTorqueThreshold}");

        // Add manual break monitor
        beam.AddComponent<BridgeBeamManualBreak>().currentForceThreshold = breakForceThreshold;
    }

    private void ApplySupportToConnectedBeams(Transform nodeA, Transform nodeB)
    {
        // Find all hinge joints in the scene
        HingeJoint[] allHinges = Object.FindObjectsByType<HingeJoint>(FindObjectsSortMode.None);
        Debug.Log($"[ApplySupport] Found {allHinges.Length} hinge joints in scene.");

        // Increase break thresholds on any hinge attached to either nodeA or nodeB
        foreach (var hinge in allHinges)
        {
            if (hinge.connectedBody == null) continue;
            Transform connectedNode = hinge.connectedBody.transform;
            if (Vector3.Distance(connectedNode.position, nodeA.position) < 0.001f ||
                Vector3.Distance(connectedNode.position, nodeB.position) < 0.001f)
            {
                hinge.breakForce += supportBonusForce;
                hinge.breakTorque += supportBonusTorque;
                Debug.Log($"[ApplySupport] Increased break thresholds on hinge ({hinge.gameObject.name}) connected to " +
                          $"{connectedNode.name}: new breakForce={hinge.breakForce}, breakTorque={hinge.breakTorque}");
            }
        }
    }

    #endregion

    #region Utility Methods

    private Transform FindIndexTip(OVRSkeleton skeleton)
    {
        if (skeleton == null || !skeleton.IsDataValid || !skeleton.IsDataHighConfidence)
            return null;

        foreach (var b in skeleton.Bones)
        {
            if (b.Transform.name.EndsWith("IndexTip"))
                return b.Transform;
        }

        Debug.LogWarning($"[FindIndexTip] Couldn't find a bone containing 'IndexTip' under {skeleton.name}");
        return null;
    }

    private SnapInteractable PickClosestNodeToIndexTip(OVRSkeleton skeleton)
    {
        Transform tip = FindIndexTip(skeleton);
        if (tip == null) return null;

        float bestDist = float.MaxValue;
        SnapInteractable bestNode = null;

        foreach (var node in Object.FindObjectsByType<SnapInteractable>(FindObjectsSortMode.None))
        {
            float d = Vector3.Distance(node.transform.position, tip.position);
            if (d < bestDist && d < 0.2f)
            {
                bestDist = d;
                bestNode = node;
            }
        }

        if (bestNode != null)
            Debug.Log($"[PickClosestNode] Selected node at {bestNode.transform.position} (dist {bestDist:F3})");
        else
            Debug.Log("[PickClosestNode] No node within threshold.");

        return bestNode;
    }

    #endregion
}

public class BridgeBeamManualBreak : MonoBehaviour
{
    [HideInInspector] public float currentForceThreshold = 15f;

    void FixedUpdate()
    {
        var hinges = GetComponents<HingeJoint>();
        foreach (var hinge in hinges)
        {
            // Read the **current** breakForce & breakTorque directly from the hinge:
            currentForceThreshold  = hinge.breakForce;
            float currentTorqueThreshold = hinge.breakTorque;
            Vector3 reaction = hinge.currentForce;
            float mag = reaction.magnitude;
            if (mag > currentForceThreshold)
            {
                Debug.Log($"[BridgeBeamManualBreak] Breaking hinge on {gameObject.name} at force {mag:F1} N (threshold {currentForceThreshold})");
                BridgeGraph.UnregisterBeam(gameObject);
                Destroy(gameObject);
                return;
            }
        }
    }
    void OnDestroy()
    {
        // In case something else destroys this object, still unregister
        BridgeGraph.UnregisterBeam(gameObject);
    }
}
