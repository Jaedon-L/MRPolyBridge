using UnityEngine;
using UnityEngine.Events;

[AddComponentMenu("Vehicle/LeverSnap")]
public class LeverSnap : MonoBehaviour
{
    [Header("Anchors (set these to empty child Transforms placed where Park/Reverse/Drive should be)")]
    public Transform parkAnchor;
    public Transform reverseAnchor;
    public Transform driveAnchor;

    [Header("Snapping")]
    [Tooltip("Distance (world units) near an anchor to trigger snapping while dragging.")]
    public float snapThreshold = 0.05f;
    [Tooltip("If true the lever will only snap when the player releases it. If false it can snap while dragging when close enough.")]
    public bool snapOnReleaseOnly = false;
    [Tooltip("How fast the lever snaps/lerps to the exact anchor (higher -> faster).")]
    public float snapLerpSpeed = 40f;
    [Tooltip("If true, when snapped we optionally disable the transformer component (if provided) to fully lock movement.")]
    public bool disableTransformerWhenSnapped = true;

    [Header("Optional: assign your transformer/grab script here so it can be disabled while snapped")]
    public MonoBehaviour transformerToDisable;

    [Header("Events (optional)")]
    public UnityEvent onSnapToPark;
    public UnityEvent onSnapToReverse;
    public UnityEvent onSnapToDrive;

    // internal states
    bool isGrabbed = false;
    bool isSnapped = false;
    Transform currentAnchor = null;

    // small optimization: distance thresholds squared
    float snapThresholdSq => snapThreshold * snapThreshold;

    void Reset()
    {
        // Try to auto-find three child anchors if they exist by name
        parkAnchor = transform.Find("Park");
        reverseAnchor = transform.Find("Reverse");
        driveAnchor = transform.Find("Drive");
    }

    void Update()
    {
        // If there's no anchors assigned we can't do anything
        if (parkAnchor == null || reverseAnchor == null || driveAnchor == null) return;

        if (isGrabbed)
        {
            // While grabbing, optionally snap when close enough
            if (!snapOnReleaseOnly)
            {
                TrySnapNearestAnchor();
            }
            else
            {
                // If snap-on-release-only but previously snapped during grab, keep it snapped
                if (isSnapped && currentAnchor != null)
                {
                    // keep lock enforced (handled in LateUpdate)
                }
            }
        }
        else
        {
            // Not grabbed: if snapOnReleaseOnly is set we ensure on release we snap to nearest anchor (handled in OnReleased)
            // If not snapped, optionally snap to nearest if within a small "rest" threshold (useful for small overshoots)
            if (!isSnapped)
            {
                // optional: softly snap to nearest if very close (makes lever settle)
                TrySoftSnapRest();
            }
        }
    }

    void LateUpdate()
    {
        // When snapped, enforce position (and optionally disable transformer)
        if (isSnapped && currentAnchor != null)
        {
            // Smoothly move to exact anchor position (use LateUpdate to override other movement scripts)
            transform.position = Vector3.Lerp(transform.position, currentAnchor.position, Time.deltaTime * snapLerpSpeed);
            transform.rotation = Quaternion.Slerp(transform.rotation, currentAnchor.rotation, Time.deltaTime * snapLerpSpeed);

            if (disableTransformerWhenSnapped && transformerToDisable != null)
            {
                if (transformerToDisable.enabled) transformerToDisable.enabled = false;
            }
        }
        else
        {
            // if not snapped, make sure transformer is enabled
            if (transformerToDisable != null && disableTransformerWhenSnapped)
            {
                if (!transformerToDisable.enabled) transformerToDisable.enabled = true;
            }
        }
    }

    // Public API: call these from your transformer / grab events
    public void OnGrabbed()
    {
        isGrabbed = true;
        // if we were snapped previously we want to allow the user to pick it up and move again, unless you want it locked
        if (isSnapped)
        {
            // If you want to allow "unsnapping" when the user grabs it, uncomment:
            Unsnapped();
            // For now we keep it snapped while grabbed until they move far enough from anchor (below).
        }
    }

    public void OnReleased()
    {
        isGrabbed = false;

        if (snapOnReleaseOnly)
        {
            // Snap to nearest anchor on release
            SnapToNearestAnchor();
        }
        else
        {
            // If currently snapped, keep snapped. If not, optionally snap if within threshold
            if (!isSnapped)
                SnapToNearestAnchorIfClose();
        }
    }

    // Tries to snap while dragging: chooses nearest anchor and snaps if inside threshold
    void TrySnapNearestAnchor()
    {
        Transform nearest = NearestAnchor();
        if (nearest == null) return;

        float dSq = (transform.position - nearest.position).sqrMagnitude;
        if (dSq <= snapThresholdSq)
        {
            SetSnapped(nearest);
        }
        else
        {
            // if user moves away enough, allow unsnap
            if (isSnapped && currentAnchor != null && (transform.position - currentAnchor.position).sqrMagnitude > snapThresholdSq * 4f)
            {
                Unsnapped();
            }
        }
    }

    // On release, snap to nearest anchor always (use with snapOnReleaseOnly = true)
    void SnapToNearestAnchor()
    {
        Transform nearest = NearestAnchor();
        if (nearest == null) return;
        SetSnapped(nearest);
        // fire event
        FireAnchorEvent(nearest);
    }

    // On release: snap only if close enough (less aggressive)
    void SnapToNearestAnchorIfClose()
    {
        Transform nearest = NearestAnchor();
        if (nearest == null) return;
        float dSq = (transform.position - nearest.position).sqrMagnitude;
        if (dSq <= snapThresholdSq * 4f) // looser threshold on release
        {
            SetSnapped(nearest);
            FireAnchorEvent(nearest);
        }
    }

    // When the lever is resting (not grabbed) softly snap to nearest if extremely close
    void TrySoftSnapRest()
    {
        Transform nearest = NearestAnchor();
        if (nearest == null) return;
        float dSq = (transform.position - nearest.position).sqrMagnitude;
        if (dSq <= snapThresholdSq * 0.25f)
        {
            SetSnapped(nearest);
        }
    }

    Transform NearestAnchor()
    {
        Transform nearest = parkAnchor;
        float best = (transform.position - parkAnchor.position).sqrMagnitude;

        float dRev = (transform.position - reverseAnchor.position).sqrMagnitude;
        if (dRev < best) { nearest = reverseAnchor; best = dRev; }

        float dDrive = (transform.position - driveAnchor.position).sqrMagnitude;
        if (dDrive < best) { nearest = driveAnchor; best = dDrive; }

        return nearest;
    }

    void SetSnapped(Transform anchor)
    {
        currentAnchor = anchor;
        isSnapped = true;
        // optionally immediately teleport to avoid jitter; comment if you want only smooth lerp
        transform.position = anchor.position;
        transform.rotation = anchor.rotation;

        // leave transformer disabled by LateUpdate if configured
        FireAnchorEvent(anchor);
    }

    void Unsnapped()
    {
        isSnapped = false;
        currentAnchor = null;
        // re-enable transformer will be handled in LateUpdate
    }

    void FireAnchorEvent(Transform anchor)
    {
        if (anchor == parkAnchor) onSnapToPark?.Invoke();
        else if (anchor == reverseAnchor) onSnapToReverse?.Invoke();
        else if (anchor == driveAnchor) onSnapToDrive?.Invoke();
    }

    // Editor helper: draw gizmos for anchors
    void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.green;
        if (parkAnchor != null) Gizmos.DrawSphere(parkAnchor.position, snapThreshold * 0.5f);
        Gizmos.color = Color.yellow;
        if (reverseAnchor != null) Gizmos.DrawSphere(reverseAnchor.position, snapThreshold * 0.5f);
        Gizmos.color = Color.red;
        if (driveAnchor != null) Gizmos.DrawSphere(driveAnchor.position, snapThreshold * 0.5f);

        Gizmos.color = Color.white;
        Gizmos.DrawLine(transform.position, transform.position + transform.up * 0.01f); // small marker at lever
    }
}
