using UnityEngine;

public class LevelSelectButton : MonoBehaviour
{
    [Tooltip("Overlay shown when level is locked")]
    public GameObject lockOverlay;

    [Tooltip("Star icons in ascending order: 1, 2, 3")]
    public GameObject[] starIcons = new GameObject[3];
}
