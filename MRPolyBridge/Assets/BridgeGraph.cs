using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction;

[System.Serializable]
public struct BridgePhysicsConfig
{
    public float baseBreakForce;
    public float baseBreakTorque;
    public float supportBonusForce;
    public float supportBonusTorque;
}

/// <summary>
/// A simple adjacency registry: each node (SnapInteractable) maps to a set of
/// all beams (GameObjects) currently hinged to it.  Also tracks which nodes are
/// currently “supported” by a support beam (via SupportTracker).
/// 
/// Now updated so that each newly supported node **adds** its bonus onto each
/// beam’s existing breakForce, instead of replacing it wholesale.
/// </summary>
public static class BridgeGraph
{
    private static BridgePhysicsConfig _config = new()
    {
        baseBreakForce = 15f,
        baseBreakTorque = 6f,
        supportBonusForce = 3f,
        supportBonusTorque = 2f
    };

    public static BridgePhysicsConfig GetCurrentConfig() => _config;

    // For each node (by its instance ID), which hinge‐beam GameObjects are attached?
    private static Dictionary<int, HashSet<GameObject>> _nodeToBeams = new();
    // For each beam (by its instance ID), store the two node IDs (so we can unregister).
    private static Dictionary<int, (int nodeAId, int nodeBId)> _beamToNodes = new();

    // Which nodes are currently supported? (A support beam directly under them)
    private static HashSet<int> _supportedNodeIds = new();

    // Keep track of which supported nodes have already applied their bonus to each beam.
    // Key = beamInstanceID, Value = set of nodeIDs whose bonus has been applied to that beam.
    private static Dictionary<int, HashSet<int>> _beamBonusApplied = new();

    /// <summary>
    /// Call this when a new beam GameObject is created and its hinges are attached
    /// to nodeA and nodeB.  We store both directions in the registry.  We also
    /// initialize its current breakForce to baseBreakForce.
    /// </summary>
    public static void RegisterBeam(GameObject beamGO, SnapInteractable nodeA, SnapInteractable nodeB)
    {
        int beamId = beamGO.GetInstanceID();
        int idA = nodeA.gameObject.GetInstanceID();
        int idB = nodeB.gameObject.GetInstanceID();

        Debug.Log($"[BridgeGraph] RegisterBeam: beam '{beamGO.name}' ({beamId}) between NodeA='{nodeA.name}' ({idA}) and NodeB='{nodeB.name}' ({idB})");

        _beamToNodes[beamId] = (idA, idB);

        if (!_nodeToBeams.TryGetValue(idA, out var beamsAtA))
        {
            beamsAtA = new HashSet<GameObject>();
            _nodeToBeams[idA] = beamsAtA;
        }
        beamsAtA.Add(beamGO);

        if (!_nodeToBeams.TryGetValue(idB, out var beamsAtB))
        {
            beamsAtB = new HashSet<GameObject>();
            _nodeToBeams[idB] = beamsAtB;
        }
        beamsAtB.Add(beamGO);

        // Initialize this beam’s bonus‐tracking set:
        _beamBonusApplied[beamId] = new HashSet<int>();

        // Finally: set each hinge’s breakForce to baseBreakForce (first time)
        var hinges = beamGO.GetComponents<HingeJoint>();
        foreach (var hinge in hinges)
        {
            hinge.breakForce = _config.baseBreakForce;
            hinge.breakTorque = _config.baseBreakTorque;
        }
        // ——————————————————————————————————————————————————————————
        // (2) ***NEW:*** If either endpoint node is already supported, immediately apply its bonus:
        if (_supportedNodeIds.Contains(idA))
        {
            ApplySupportBonusFromNode(idA);
        }
        if (_supportedNodeIds.Contains(idB))
        {
            ApplySupportBonusFromNode(idB);
        }
        // Now this new beam’s hinges will get the same +supportBonus that older beams did.
        // ——————————————————————————————————————————————————————————
    }

    /// <summary>
    /// Call this when a beam GameObject is destroyed or removed.
    /// </summary>
    public static void UnregisterBeam(GameObject beamGO)
    {
        int beamId = beamGO.GetInstanceID();
        if (!_beamToNodes.TryGetValue(beamId, out var nodes)) return;

        int idA = nodes.nodeAId;
        int idB = nodes.nodeBId;

        if (_nodeToBeams.TryGetValue(idA, out var beamsAtA))
        {
            beamsAtA.Remove(beamGO);
            if (beamsAtA.Count == 0) _nodeToBeams.Remove(idA);
        }
        if (_nodeToBeams.TryGetValue(idB, out var beamsAtB))
        {
            beamsAtB.Remove(beamGO);
            if (beamsAtB.Count == 0) _nodeToBeams.Remove(idB);
        }
        _beamToNodes.Remove(beamId);
        _beamBonusApplied.Remove(beamId);
    }

    /// <summary>
    /// Mark this node as “supported.”  When a support beam appears under a node,
    /// call this to add the node’s ID to our supported set.  We then ONLY add that
    /// node’s bonus once to each beam in the cluster.
    /// </summary>
    public static void MarkNodeSupported(SnapInteractable node)
    {
        int nodeId = node.gameObject.GetInstanceID();
        if (_supportedNodeIds.Add(nodeId))
        {
            Debug.Log($"[BridgeGraph] MarkNodeSupported: {node.name} (ID {nodeId})");
            ApplySupportBonusFromNode(nodeId);
        }
    }

    /// <summary>
    /// Mark this node as “no longer supported.”  When a support is removed,
    /// call this to remove from our set.  We will then subtract that node’s
    /// bonus from each beam that previously received it.
    /// </summary>
    public static void UnmarkNodeSupported(SnapInteractable node)
    {
        int nodeId = node.gameObject.GetInstanceID();
        if (_supportedNodeIds.Remove(nodeId))
        {
            Debug.Log($"[BridgeGraph] UnmarkNodeSupported: {node.name} (ID {nodeId})");
            RemoveSupportBonusFromNode(nodeId);
        }
    }

    /// <summary>
    /// When a node becomes supported, find all beams in its connected cluster,
    /// and for each beam that has *not yet* received this node’s bonus, do:
    ///    hinge.breakForce  += supportBonusForce
    ///    hinge.breakTorque += supportBonusTorque
    /// Then mark that beam as having received nodeId’s bonus in _beamBonusApplied.
    /// </summary>
    private static void ApplySupportBonusFromNode(int startNodeId)
    {
        if (!_nodeToBeams.ContainsKey(startNodeId))
        {
            // no beams attached to this node → nothing to update
            Debug.Log($"[BridgeGraph] ApplySupportBonus: node {startNodeId} has no beams.");
            return;
        }

        // BFS over nodes and beams to collect every beam in the cluster:
        var visitedNodes = new HashSet<int>();
        var visitedBeams = new HashSet<int>();
        var queue = new Queue<int>();

        queue.Enqueue(startNodeId);
        visitedNodes.Add(startNodeId);

        while (queue.Count > 0)
        {
            int nodeId = queue.Dequeue();
            if (!_nodeToBeams.TryGetValue(nodeId, out var beams)) continue;

            foreach (var beamGO in beams)
            {
                int beamId = beamGO.GetInstanceID();
                if (visitedBeams.Add(beamId))
                {
                    // Enqueue endpoint nodes
                    if (_beamToNodes.TryGetValue(beamId, out var nodePair))
                    {
                        if (!visitedNodes.Contains(nodePair.nodeAId))
                        {
                            visitedNodes.Add(nodePair.nodeAId);
                            queue.Enqueue(nodePair.nodeAId);
                        }
                        if (!visitedNodes.Contains(nodePair.nodeBId))
                        {
                            visitedNodes.Add(nodePair.nodeBId);
                            queue.Enqueue(nodePair.nodeBId);
                        }
                    }
                }
            }
        }

        // Now visitedBeams holds every beam in the cluster. For each beam that
        // hasn’t yet received *this node’s* bonus, add it:
        foreach (int beamId in visitedBeams)
        {
            if (!_beamBonusApplied.TryGetValue(beamId, out var appliedSet))
                continue;

            // If we have *not* yet applied this node’s bonus to that beam:
            if (!appliedSet.Contains(startNodeId))
            {
                // Find the beam GameObject & its hinges:
                GameObject beamGO = FindBeamByID(beamId);
                if (beamGO == null)
                    continue;

                var hinges = beamGO.GetComponents<HingeJoint>();
                foreach (var hinge in hinges)
                {
                    hinge.breakForce += _config.supportBonusForce;
                    hinge.breakTorque += _config.supportBonusTorque;
                }
                // Now lock this beam’s hinges so it cannot pivot at all:
                LockBeamHinges(beamId);

                Debug.Log($"[BridgeGraph]   +Applied bonus from node {startNodeId} to beam '{beamGO.name}' (ID {beamId}): +{_config.supportBonusForce}/{_config.supportBonusTorque}");
                // Mark that this beam has now received nodeId’s bonus:
                appliedSet.Add(startNodeId);
            }
        }
    }

    /// <summary>
    /// When a node’s support is removed, subtract that node’s bonus from every beam
    /// in its cluster that had previously gotten that bonus.  Then remove nodeId from
    /// each beam’s _beamBonusApplied set.
    /// </summary>
    private static void RemoveSupportBonusFromNode(int startNodeId)
    {
        if (!_nodeToBeams.ContainsKey(startNodeId))
        {
            Debug.Log($"[BridgeGraph] RemoveSupportBonus: node {startNodeId} has no beams.");
            return;
        }

        // BFS again to collect every beam in the cluster:
        var visitedNodes = new HashSet<int>();
        var visitedBeams = new HashSet<int>();
        var queue = new Queue<int>();

        queue.Enqueue(startNodeId);
        visitedNodes.Add(startNodeId);

        while (queue.Count > 0)
        {
            int nodeId = queue.Dequeue();
            if (!_nodeToBeams.TryGetValue(nodeId, out var beams)) continue;

            foreach (var beamGO in beams)
            {
                int beamId = beamGO.GetInstanceID();
                if (visitedBeams.Add(beamId))
                {
                    if (_beamToNodes.TryGetValue(beamId, out var nodePair))
                    {
                        if (!visitedNodes.Contains(nodePair.nodeAId))
                        {
                            visitedNodes.Add(nodePair.nodeAId);
                            queue.Enqueue(nodePair.nodeAId);
                        }
                        if (!visitedNodes.Contains(nodePair.nodeBId))
                        {
                            visitedNodes.Add(nodePair.nodeBId);
                            queue.Enqueue(nodePair.nodeBId);
                        }
                    }
                }
            }
        }

        // For each beam that had previously gotten startNodeId’s bonus, subtract it now:
        foreach (int beamId in visitedBeams)
        {
            if (!_beamBonusApplied.TryGetValue(beamId, out var appliedSet))
                continue;

            if (appliedSet.Contains(startNodeId))
            {
                GameObject beamGO = FindBeamByID(beamId);
                if (beamGO == null)
                    continue;

                var hinges = beamGO.GetComponents<HingeJoint>();
                foreach (var hinge in hinges)
                {
                    hinge.breakForce -= _config.supportBonusForce;
                    hinge.breakTorque -= _config.supportBonusTorque;
                }
                // If _after_ removing, this beam has NO more supporting nodes, restore its hinge limits:
                if (appliedSet.Count == 0)
                {
                    UnlockBeamHinges(beamId);
                }


                Debug.Log($"[BridgeGraph]   −Removed bonus from node {startNodeId} on beam '{beamGO.name}' (ID {beamId}): −{_config.supportBonusForce}/{_config.supportBonusTorque}");

                // Clear the record:
                appliedSet.Remove(startNodeId);
            }
        }
    }

    /// <summary>
    /// Utility to fetch a beam GameObject from its instance ID.
    /// (Assumes the beam still exists in the scene.)
    /// </summary>
    private static GameObject FindBeamByID(int beamId)
    {
        var allHinges = Object.FindObjectsByType<HingeJoint>(FindObjectsSortMode.None);
        foreach (var hinge in allHinges)
        {
            if (hinge.gameObject.GetInstanceID() == beamId)
                return hinge.gameObject;
        }
        return null;
    }
    private static void LockBeamHinges(int beamId)
    {
        GameObject beamGO = FindBeamByID(beamId);
        if (beamGO == null) return;

        var hinges = beamGO.GetComponents<HingeJoint>();
        foreach (var hinge in hinges)
        {
            hinge.useLimits = false;
            var jl = hinge.limits;
            jl.min = 0f;
            jl.max = 0f;
            hinge.limits = jl;
        }
    }

    private static void UnlockBeamHinges(int beamId)
    {
        GameObject beamGO = FindBeamByID(beamId);
        if (beamGO == null) return;

        var hinges = beamGO.GetComponents<HingeJoint>();
        foreach (var hinge in hinges)
        {
            hinge.useLimits = true;
            var jl = hinge.limits;
            jl.min = -1f;
            jl.max = +1f;
            hinge.limits = jl;
        }

    }

}
