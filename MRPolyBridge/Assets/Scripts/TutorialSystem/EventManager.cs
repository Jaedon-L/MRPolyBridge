using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Video;

/// <summary>
/// Singleton-based event manager that centralizes event handling 
/// for instruction flow, including start, change, and completion notifications.
/// </summary>
public class EventManager : MonoBehaviour
{
    public static EventManager Instance;

    /// <summary>
    /// Unity Awake callback. Initializes the singleton instance of the EventManager.
    /// Ensures only one instance exists in the scene.
    /// </summary>
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

    /// <summary>
    /// Event triggered to request a step change in the instruction sequence.
    /// </summary>
    public event Action OnChangeInstruction;

    /// <summary>
    /// Invokes the OnChangeInstruction event to notify listeners that the current instruction should change.
    /// </summary>
    public void ChangeInstruction()
    {
        OnChangeInstruction?.Invoke();
    }

    /// <summary>
    /// Event triggered when the instruction sequence has ended.
    /// </summary>
    public event Action OnInstructionEnd;

    /// <summary>
    /// Invokes the OnInstructionEnd event to notify listeners that all instructions are complete.
    /// </summary>
    public void InstructionEnd()
    {
        OnInstructionEnd?.Invoke();
    }

    /// <summary>
    /// Event triggered when the instruction message or associated video changes.
    /// </summary>
    public event Action<string, VideoClip> OnInstructionChange;

    /// <summary>
    /// Invokes the OnInstructionChange event with the new instruction text and associated video.
    /// </summary>
    /// <param name="newInstruction">The new instruction message to display.</param>
    /// <param name="clip">The video clip related to the instruction.</param>
    public void InstructionChange(string newInstruction, VideoClip clip)
    {
        OnInstructionChange?.Invoke(newInstruction, clip);
    }

    /// <summary>
    /// Event triggered to signal the start of the experiment or tutorial sequence.
    /// </summary>
    public Action StartTutorial;
}
