using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Game/Level", order = 1)]
public class Level : ScriptableObject
{
    [Header("Level Data")]
    public string levelName;
    public GameObject levelPrefab;
    public float budget;
    public bool isUnlocked = false;

    [Header("Bridge Settings")]
    public float breakForceThreshold = 15f;
    public float breakTorqueThreshold = 8f;

    [Header("Support Settings")]
    public float supportBonusForce = 3f;
    public float supportBonusTorque = 2f;
}
