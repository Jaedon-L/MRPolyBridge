using UnityEngine;

public class DestructibleNode : MonoBehaviour
{
    [SerializeField] private string beanTag;

    private void OnCollisionEnter(Collision collision)
    {
        if (!collision.gameObject.tag.Equals(beanTag)) return;
        BridgeGraph.UnregisterBeam(collision.gameObject);
        Destroy(collision.gameObject);
    }
}