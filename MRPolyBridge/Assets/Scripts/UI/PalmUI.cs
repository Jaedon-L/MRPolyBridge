using UnityEngine;

public class PalmUI : MonoBehaviour
{
    [Header("Hand Setup")]
    [SerializeField] private OVRHand targetHand;
    [SerializeField] private Transform handWrist;
    
    [Header("Menu Settings")]
    [SerializeField] private GameObject menuPanel;
    [SerializeField] private float rotationThreshold = 160f;
    [SerializeField] private float menuDistance = 0.1f;
    [SerializeField] private Vector3 menuOffset = Vector3.zero;
    
    private bool menuVisible = false;
    private bool handTracked = false;
    
    void Update()
    {
        // Check if hand is tracked
        handTracked = targetHand != null && targetHand.IsTracked;
        
        if (!handTracked)
        {
            if (menuVisible)
            {
                HideMenu();
            }
            return;
        }
        
        // Check hand rotation
        CheckHandRotation();
        
        // Update menu position if visible
        if (menuVisible && menuPanel != null)
        {
            UpdateMenuPosition();
        }
    }
    
    private void CheckHandRotation()
    {
        if (handWrist == null) return;

        // Use palm normal as -up
        Vector3 palmNormal = handWrist.up;

        // Vector from hand to camera
        Vector3 toCamera = (Camera.main.transform.position - handWrist.position).normalized;

        // Dot product to find alignment
        float palmAlignment = Vector3.Dot(palmNormal, toCamera);

        float palmAngle = Mathf.Acos(Mathf.Clamp(palmAlignment, -1f, 1f)) * Mathf.Rad2Deg;

        if (palmAngle > rotationThreshold && !menuVisible)
        {
            ShowMenu();
        }
        else if (palmAngle <= rotationThreshold && menuVisible)
        {
            HideMenu();
        }
    }

    
    private void ShowMenu()
    {
        if (menuPanel == null) return;
        
        menuVisible = true;
        menuPanel.SetActive(true);
        
        Debug.Log("Hand menu shown!");
    }
    
    private void HideMenu()
    {
        if (menuPanel == null) return;
        
        menuVisible = false;
        menuPanel.SetActive(false);
        
        Debug.Log("Hand menu hidden!");
    }
    
    private void UpdateMenuPosition()
    {
        if (handWrist == null || menuPanel == null) return;
        
        // Position menu above the palm
        Vector3 palmPosition = handWrist.position;
        Vector3 palmForward = handWrist.forward;
        Vector3 menuPosition = palmPosition + (palmForward * menuDistance) + menuOffset;
        
        menuPanel.transform.position = menuPosition;
        
        // Make menu face the camera
        Vector3 lookDirection =  menuPosition - Camera.main.transform.position;
        if (lookDirection != Vector3.zero)
        {
            menuPanel.transform.rotation = Quaternion.LookRotation(lookDirection);
        }
    }
}