using System.Collections.Generic;
using UnityEngine;
using Oculus.Interaction; // for Grabbable

public class DeleteModeManager : MonoBehaviour
{
    [Tooltip("Assign a Garbage Can GameObject here so we can show/hide it when Delete Mode toggles.")]
    [SerializeField] private GameObject _garbageCan;

    // All Grabbable components in the scene
    private List<Grabbable> _allGrabbables = new List<Grabbable>();

    // Are we currently in Delete Mode?
    private bool _deleteMode = true;
    [SerializeField] GameObject leftHandInteractor;
    [SerializeField] GameObject rightHandInteractor;


    private void Start()
    {
        // At startup, collect every Grabbable in the scene.
        // (If you spawn new ones at runtime, you can call RefreshList() manually.)
        // ToggleDeleteMode(); 
    }


    public void ToggleDeleteMode()
    {
        _deleteMode = !_deleteMode;
        leftHandInteractor.SetActive(_deleteMode); 
        rightHandInteractor.SetActive(_deleteMode); 

        // Show or hide the garbage can in the world
        if (_garbageCan != null)
        {
            _garbageCan.SetActive(_deleteMode);
        }

        Debug.Log($"[DeleteModeManager] Delete Mode is now {(_deleteMode ? "ON" : "OFF")}");
    }


}
