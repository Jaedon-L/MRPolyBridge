using UnityEngine;

[CreateAssetMenu(fileName = "Level", menuName = "Game/Level", order = 1)]
public class Level : ScriptableObject
{
    [Header("Level Data")]
    public string levelName;
    public GameObject levelPrefab;
    public bool isUnlocked = false;

    [Header("Support Settings")]
    public float supportBonusForce = 3f;
    public float supportBonusTorque = 2f;
}
