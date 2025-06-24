using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InstructionMenu : MonoBehaviour
{
    public TextMeshProUGUI instructionText;
    // Start is called before the first frame update
    void Start()
    {
        EventManager.Instance.OnInstructionChange += SetInstruction;
    }

    private void SetInstruction(string message)
    {
        instructionText.text = message;
    }
}
