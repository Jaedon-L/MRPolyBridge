using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class StringCheckCondition : Conditions
{
    [Tooltip("The value to be compared with")]
    public string value;
    [Tooltip("The current value")]
    public TMP_Text stringCompare;
    public override void CheckCondition()
    {
        if (isActive)
        {
            isFinished = true;
            EventManager.Instance.ChangeInstruction();
            isActive = false;
        }
    }

    public void StringCompareValue()
    {
        if (!isActive)
            return;
        if(stringCompare != null && stringCompare.text == value)
        {
            CheckCondition();
        }
    }
}
