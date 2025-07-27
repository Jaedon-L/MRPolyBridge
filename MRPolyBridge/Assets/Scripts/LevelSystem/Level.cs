using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Game/Level", order = 1)]
public class Level : ScriptableObject
{
    [Header("Level Data")]
    public string levelName;
    public GameObject levelPrefab;
    public float budget;
    public bool isUnlocked = false;

    [Header("Star Thresholds")]
    [Tooltip("Remaining budget needed to earn 1, 2, or 3 stars (in ascending order).")]
    public float[] starThresholds = new float[3] { 20f, 50f, 80f };
    

    [Header("Bridge Settings")]
    public float breakForceThreshold = 15f;
    public float breakTorqueThreshold = 8f;

    [Header("Support Settings")]
    public float supportBonusForce = 3f;
    public float supportBonusTorque = 2f;
}
