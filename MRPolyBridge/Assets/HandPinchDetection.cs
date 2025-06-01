using Oculus.Interaction;
using Unity.Mathematics;
using UnityEngine;

public class HandPinchDetection : MonoBehaviour
{

    [SerializeField] Rigidbody snapAreaRb;
    private bool _hasPinched;
    private bool _isIndexFingerPinching;
    private float _pinchStrength;

    [SerializeField] GameObject snapInteractablePrefab;

    [Header("OVRHand data")]
    private Transform handIndexTipTransform;
    [SerializeField] OVRSkeleton skeleton;
    [SerializeField] OVRHand lefthand;
    private OVRHand.TrackingConfidence _confidence;
    void Update() => CheckPinch(lefthand);

    void CheckPinch(OVRHand hand)
    {
        _pinchStrength = hand.GetFingerPinchStrength(OVRHand.HandFinger.Index); //0-1 strength of pinch
        _isIndexFingerPinching = hand.GetFingerIsPinching(OVRHand.HandFinger.Index); //tracks finger pinch
        _confidence = hand.GetFingerConfidence(OVRHand.HandFinger.Index); // high or low confidence in pinch

        if (!_hasPinched && _isIndexFingerPinching && _confidence == OVRHand.TrackingConfidence.High)
        {
            _hasPinched = true;

            // New lines added below this point
            foreach (var b in skeleton.Bones)
            {
                if (b.Id == OVRSkeleton.BoneId.Hand_IndexTip)
                {
                    handIndexTipTransform = b.Transform;
                    break;
                }
            }
            GameObject go = Instantiate(snapInteractablePrefab, handIndexTipTransform.position, quaternion.identity);
            var snapInteractable = go.GetComponentInChildren<SnapInteractable>(); 
            if (snapInteractable == null)
            {
                Debug.LogError("snapInteractablePrefab does not contain a SnapInteractable!");
                return;
            }
            // this is the **only** valid way to set the Rigidbody at runtime
            snapInteractable.InjectRigidbody(snapAreaRb);

            Debug.Log("Spawned & injected Rigidbody on SnapInteractable at index tip.");
        }
        else if (_hasPinched && !_isIndexFingerPinching)
        {
            _hasPinched = false;
            Debug.Log("let go of pinch");
        }
    }

    void AssignRb()
    {

    }
    
}
