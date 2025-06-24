using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public abstract class Conditions : MonoBehaviour
{
    public EnumClass.InstructionsID instructionID;
    //[HideInInspector]
    public bool isActive;
    public bool isFinished;

    public virtual void Awake()
    {

    }

    private void Start()
    {
        if (transform.parent.TryGetComponent(out InstructionManager manager))
            manager.AddConditions(this);

        InstructionManager.Instance.AddConditions(this);
    }

    public abstract void CheckCondition();
}