using UnityEngine;

public class BuildCost : MonoBehaviour
{
    /// <summary>
    /// Enum to indicate type of build object.
    /// </summary>
    public enum ObjectType
    {
        Node,
        MainBeam,
        SupportBeam
    }

    [Tooltip("Type of this placed object.")]
    [SerializeField] private ObjectType objectType;

    [Tooltip("Cost that was spent to place this object.")]
    [SerializeField] private float costPlaced;

    /// <summary>
    /// If the beam was destroyed by breaking (overload), mark broken = true to prevent refund.
    /// </summary>
    [HideInInspector] public bool broken = false;

    /// <summary>
    /// Initialize BuildCost after instantiation.
    /// </summary>
    public void Initialize(ObjectType type, float cost)
    {
        objectType = type;
        costPlaced = cost;
    }

    public ObjectType GetObjectType() => objectType;
    public float GetCostPlaced() => costPlaced;
}
