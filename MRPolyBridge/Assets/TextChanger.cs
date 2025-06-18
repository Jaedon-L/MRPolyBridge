using UnityEngine;
using TMPro;
using Oculus.Interaction; // Required for MaterialPropertyBlockEditor

public class TextChanger : MonoBehaviour
{
    [Header("Display Text")]
    [SerializeField] private TextMeshPro supportMode;
    [SerializeField] private string OnText;
    [SerializeField] private string OffText;

    [Header("Optional Visual Feedback for Button")]
    [SerializeField] private MaterialPropertyBlockEditor roundedBoxEditor;
    [SerializeField] private string colorPropertyName = "_Color"; // or "_BaseColor" depending on shader
    [SerializeField] private Color onColor;
    [SerializeField] private Color offColor;

    private bool supportModeState;

    public void OnToggleSupport()
    {
        supportModeState = !supportModeState;
        supportMode.text = supportModeState ? OnText : OffText;

        if (roundedBoxEditor != null && !string.IsNullOrEmpty(colorPropertyName))
        {
            roundedBoxEditor.ColorProperties = new()
            {
                new MaterialPropertyColor
                {
                    name = colorPropertyName,
                    value = supportModeState ? onColor : offColor
                }
            };
            roundedBoxEditor.UpdateMaterialPropertyBlock();
        }
    }
}
