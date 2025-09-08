using UnityEngine;
using System.Collections.Generic;

public class UIManager : MonoBehaviour
{
    public enum UIMode { Hammer, Pencil, Drive }
    public UIMode currentMode = UIMode.Pencil;

    [Header("Mode Objects")]
    public List<GameObject> HammerObjects;
    public List<GameObject> PencilObjects;
    public List<GameObject> DriveObjects;

    void Start()
    {
        UpdateUI();
    }

    // --- Helper Methods for UnityEvents ---
    public void SetModeToHammer()
    {
        currentMode = UIMode.Hammer;
        UpdateUI();
    }

    public void SetModeToPencil()
    {
        currentMode = UIMode.Pencil;
        UpdateUI();
    }

    public void SetModeToDrive()
    {
        currentMode = UIMode.Drive;
        UpdateUI();
    }

    /// <summary>
    /// Cycles to the next mode in sequence (optional).
    /// </summary>
    [ContextMenu("NextMode")]
    public void NextMode()
    {
        int modeCount = System.Enum.GetNames(typeof(UIMode)).Length;
        currentMode = (UIMode)(((int)currentMode + 1) % modeCount);
        UpdateUI();
    }

    /// <summary>
    /// Activates objects for current mode and disables others.
    /// </summary>
    void UpdateUI()
    {
        SetObjectListActive(HammerObjects, currentMode == UIMode.Hammer);
        SetObjectListActive(PencilObjects, currentMode == UIMode.Pencil);
        SetObjectListActive(DriveObjects, currentMode == UIMode.Drive);
    }

    void SetObjectListActive(List<GameObject> objects, bool state)
    {
        foreach (var obj in objects)
        {
            if (obj != null)
                obj.SetActive(state);
        }
    }
}
