using UnityEngine;

public class DriveManager : MonoBehaviour
{
    private bool isOn = false;
    void Start()
    {

    }

    [ContextMenu("Toggle")]
    public void OnPressed()
    {
        isOn = !isOn;
        gameObject.SetActive(isOn);
    }
}
