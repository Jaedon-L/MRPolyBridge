using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class TextConditionCheck : Conditions
{
    public List<TMP_InputField> checkTexts; // List of Text elements

    public override void CheckCondition()
    {
        if (isActive)
        {
            isFinished = true;
            EventManager.Instance.ChangeInstruction();
            isActive = false;
        }
    }

    private void Update()
    {
        if (!isActive)
            return;

        if (checkTexts == null || checkTexts.Count == 0)
        {
            return;
        }

        // Check if all texts are non-empty
        foreach (TMP_InputField text in checkTexts)
        {
            if (text == null || string.IsNullOrEmpty(text.text))
            {
                return; // If any text is null or empty, exit the loop and do not call CheckCondition
            }
        }

        // Only call CheckCondition if all texts are not empty
        CheckCondition();
    }
}
