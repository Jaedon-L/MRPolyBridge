using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(this.gameObject);
        }
    }

    public event Action OnChangeInstruction;

    public void ChangeInstruction()
    {
        OnChangeInstruction?.Invoke();
    }

    public event Action OnInstructionEnd;

    public void InstructionEnd()
    {
        OnInstructionEnd?.Invoke();
    }

    public event Action<string> OnInstructionChange;

    public void InstructionChange(string newInstruction)
    {
        OnInstructionChange?.Invoke(newInstruction);
    }

    public Action StartExperiment;
    public Action InstructionDone;
}