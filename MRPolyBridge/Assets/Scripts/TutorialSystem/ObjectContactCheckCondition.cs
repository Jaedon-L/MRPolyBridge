using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ObjectContactCheckCondition : Conditions
{
    public bool isAttatchObjCheck;

    public override void CheckCondition()
    {
        if (isActive)
        {
            isFinished = true;
            EventManager.Instance.ChangeInstruction();
            isActive = false;
        }
    }
}
