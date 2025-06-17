using UnityEngine;
using TMPro;
public class TextChanger : MonoBehaviour
{
    [SerializeField] TextMeshPro supportMode;
    private bool supportModeState;
    [SerializeField] string OnText; 
    [SerializeField] string OffText; 
    public void OnToggleSupport()
    {
        supportModeState = !supportModeState;
        if (supportModeState)
        {
            supportMode.text = OnText;
        }
        else
        {
            supportMode.text = OffText;
        }
    }

}
