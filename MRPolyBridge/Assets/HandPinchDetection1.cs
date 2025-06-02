using System.Collections;
using Oculus.Interaction;
using Unity.Mathematics;
using UnityEngine;

public class HandPinchDetection1 : MonoBehaviour
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

    [Header("Bridge/Support Settings")]
    [SerializeField] private float breakForceThreshold = 4000f; // N
    [SerializeField] private LayerMask supportLayerMask;        // for support‐beam checks
    [SerializeField] private float supportRayLength = 0.1f;      // ray length for “support”

    // Internal state / mode flags:
    private bool buildingModeEnabled = false;  // “Build” on/off
    private bool supportModeEnabled = false;   // “Support” prefab or “main” prefab?
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
        // Only run pinch logic if building mode is on:
        if (!buildingModeEnabled) return;

        // Left‐hand: spawn nodes (only if leftPinchEnabled)
        if (leftPinchEnabled)
        {
            HandleLeftPinch();
        }

        // Right‐hand: place beams/supports (if rightPinchEnabled)
        if (rightPinchEnabled)
        {
            HandleRightPinch();
        }
    }

    #region Public Toggle Methods (hook these to your UI Buttons)

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
            Debug.Log("[ToggleBuildingMode] Cleared firstNode and preview line.");
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
            Debug.Log("[HandleLeftPinch] Detected left pinch start.");
            SpawnSnapNodeAtLeftIndexTip();
        }
        else if (_isIndexFingerPinching && !isPinching)
        {
            _isIndexFingerPinching = false;
            Debug.Log("[HandleLeftPinch] Left pinch released.");
        }
    }

    private void SpawnSnapNodeAtLeftIndexTip()
    {
        Transform tip = FindIndexTip(leftSkeleton);
        if (tip == null)
        {
            Debug.LogWarning("[SpawnSnapNodeAtLeftIndexTip] Couldn't find index tip.");
            return;
        }

        GameObject go = Instantiate(snapInteractablePrefab, tip.position, Quaternion.identity);
        var si = go.GetComponentInChildren<SnapInteractable>();
        si.InjectRigidbody(snapAreaRb);

        go.tag = "BridgeNode";
        Debug.Log($"[SpawnSnapNode] Spawned SnapNode at {tip.position}, tagged as BridgeNode.");
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
            Debug.Log("[HandleRightPinch] Detected right pinch start.");
            StartSelectingFirstNode();
        }
        else if (rightPinchActive && isPinching && confidence == OVRHand.TrackingConfidence.High)
        {
            UpdateSelectingEdgePreview();
        }
        else if (rightPinchActive && (!isPinching || confidence != OVRHand.TrackingConfidence.High))
        {
            rightPinchActive = false;
            Debug.Log("[HandleRightPinch] Right pinch released.");
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
            Debug.Log("[StartSelectingFirstNode] No node found near right index tip.");
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
            Debug.Log($"[UpdateEdgePreview] Updating preview from {firstNodeTransform.position} to {tip.position}");
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
            {
                PlaceSupportBetween(firstNodeTransform, secondNode.transform);
            }
            else
            {
                PlaceMainBeamBetween(firstNodeTransform, secondNode.transform);
            }
        }
        else
        {
            Debug.Log("[EndSelectingSecondNode] No valid second node found or second node is same as first.");
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

    #region Bridge‐Beam & Support‐Beam Placement

    private void PlaceMainBeamBetween(Transform A, Transform B)
    {
        Debug.Log("[PlaceMainBeamBetween] Spawning main beam.");
        SpawnBeamWithSupportLogic(A, B, woodBeamPrefab);
    }

    private void PlaceSupportBetween(Transform A, Transform B)
    {
        Debug.Log("[PlaceSupportBetween] Spawning support beam.");
        SpawnBeamWithSupportLogic(A, B, supportBeamPrefab);
    }

    private void SpawnBeamWithSupportLogic(Transform A, Transform B, GameObject beamPrefab)
    {
        Vector3 pA = A.position;
        Vector3 pB = B.position;
        float dist = Vector3.Distance(pA, pB);
        Vector3 mid = (pA + pB) * 0.5f;

        Vector3 rawDir = (pB - pA).normalized;
        Quaternion rawRot = Quaternion.FromToRotation(Vector3.right, rawDir);
        Vector3 e = rawRot.eulerAngles;
        e.x = 0f;
        Quaternion finalRot = Quaternion.Euler(e);

        Debug.Log($"[SpawnBeamWithSupportLogic] mid={mid}, dist={dist}, finalRot={finalRot.eulerAngles}");

        GameObject beam = Instantiate(beamPrefab, mid, finalRot);
        beam.transform.localScale = new Vector3(dist,
                                                beam.transform.localScale.y,
                                                beam.transform.localScale.z);

        Rigidbody beamRb = beam.GetComponent<Rigidbody>();
        if (beamRb == null)
        {
            beamRb = beam.AddComponent<Rigidbody>();
            Debug.Log("[SpawnBeamWithSupportLogic] Added Rigidbody to beam.");
        }
        beamRb.isKinematic = true;
        Debug.Log("[SpawnBeamWithSupportLogic] Beam is kinematic until supported.");

        var supportWatcher = beam.AddComponent<BridgeBeamWithSupport>();
        supportWatcher.Initialize(
            A.GetComponent<Rigidbody>(),
            B.GetComponent<Rigidbody>(),
            dist,
            breakForceThreshold,
            supportLayerMask,
            supportRayLength
        );
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
            Debug.Log($"[PickClosestNode] Found node at {bestNode.transform.position}, distance {bestDist}");
        else
            Debug.Log("[PickClosestNode] No node within threshold.");

        return bestNode;
    }

    #endregion
}

public class BridgeBeamWithSupport : MonoBehaviour
{
    private Rigidbody _beamRb;
    private Rigidbody _nodeA;
    private Rigidbody _nodeB;
    private float _restLength;
    private float _breakForce;
    private LayerMask _supportMask;
    private float _rayLen;

    private bool _activated = false;

    public void Initialize(Rigidbody nodeA,
                           Rigidbody nodeB,
                           float restLength,
                           float breakForceThreshold,
                           LayerMask supportLayerMask,
                           float supportRayLength)
    {
        _beamRb = GetComponent<Rigidbody>();
        _nodeA = nodeA;
        _nodeB = nodeB;
        _restLength = restLength;
        _breakForce = breakForceThreshold;
        _supportMask = supportLayerMask;
        _rayLen = supportRayLength;

        gameObject.layer = LayerMask.NameToLayer("BridgeSupport");
        Debug.Log("[BridgeBeamWithSupport] Initialized with restLength=" + restLength);
    }

    void FixedUpdate()
    {
        if (_activated) return;

        Vector3 leftEnd = transform.TransformPoint(new Vector3(-_restLength * 0.5f, 0f, 0f));
        Vector3 rightEnd = transform.TransformPoint(new Vector3(+_restLength * 0.5f, 0f, 0f));

        bool leftSupported = Physics.Raycast(leftEnd, Vector3.down, out RaycastHit hitLeft, _rayLen, _supportMask);
        bool rightSupported = Physics.Raycast(rightEnd, Vector3.down, out RaycastHit hitRight, _rayLen, _supportMask);

        Debug.Log($"[BridgeBeamWithSupport] Raycast left at {leftEnd} hit {(leftSupported ? hitLeft.collider.name : "nothing")}, " +
                  $"right at {rightEnd} hit {(rightSupported ? hitRight.collider.name : "nothing")}");

        if (leftSupported && rightSupported)
        {
            Debug.Log("[BridgeBeamWithSupport] Both ends supported, activating beam.");
            ActivateBeam();
        }
    }

    private void ActivateBeam()
    {
        if (_activated) return;
        _activated = true;

        _beamRb.isKinematic = false;
        Debug.Log("[BridgeBeamWithSupport] Beam set to dynamic.");

        if (_nodeA != null)
        {
            _nodeA.isKinematic = true;
            Debug.Log("[BridgeBeamWithSupport] Node A made kinematic.");
        }
        if (_nodeB != null)
        {
            _nodeB.isKinematic = true;
            Debug.Log("[BridgeBeamWithSupport] Node B made kinematic.");
        }

        float halfLen = 0.5f;

        Vector3 leftWorldPos = transform.TransformPoint(new Vector3(-halfLen, 0f, 0f));
        Vector3 rightWorldPos = transform.TransformPoint(new Vector3(+halfLen, 0f, 0f));

        var fjA = gameObject.AddComponent<FixedJoint>();
        fjA.connectedBody = _nodeA;
        fjA.autoConfigureConnectedAnchor = false;
        fjA.anchor = new Vector3(-halfLen, 0f, 0f);
        fjA.connectedAnchor = _nodeA.transform.InverseTransformPoint(leftWorldPos);
        fjA.breakForce = _breakForce;
        fjA.breakTorque = Mathf.Infinity;
        Debug.Log($"[ActivateBeam] Attached FixedJoint A at {leftWorldPos} with breakForce={_breakForce}");

        var fjB = gameObject.AddComponent<FixedJoint>();
        fjB.connectedBody = _nodeB;
        fjB.autoConfigureConnectedAnchor = false;
        fjB.anchor = new Vector3(+halfLen, 0f, 0f);
        fjB.connectedAnchor = _nodeB.transform.InverseTransformPoint(rightWorldPos);
        fjB.breakForce = _breakForce;
        fjB.breakTorque = Mathf.Infinity;
        Debug.Log($"[ActivateBeam] Attached FixedJoint B at {rightWorldPos} with breakForce={_breakForce}");

        var helper = gameObject.AddComponent<BridgeBeamFixedBreak>();
        helper.breakForceThreshold = _breakForce;
        Debug.Log("[ActivateBeam] BridgeBeamFixedBreak helper added.");
    }
}

public class BridgeBeamFixedBreak : MonoBehaviour
{
    [HideInInspector] public float breakForceThreshold = 4000f;

    void OnJointBreak(float breakForce)
    {
        Debug.Log($"[BridgeBeamFixedBreak] Joint broke at {breakForce:F1} N (threshold {breakForceThreshold}). Destroying {name}.");
        Destroy(gameObject);
    }
}
