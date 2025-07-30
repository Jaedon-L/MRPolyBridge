using Meta.WitAi.TTS.Utilities;
using Oculus.Interaction;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Singleton manager for handling step-by-step instructional flow for the game tutorial,
/// including audio prompts, video clips, and condition checking.
/// </summary>
[RequireComponent(typeof(AudioSource))]
public class InstructionManager : MonoBehaviour
{
    public static InstructionManager Instance { get; private set; }

    // Serialized fields
    [SerializeField] private List<InstructionData> instructionDatas = new List<InstructionData>();
    [SerializeField] private EnumClass.InstructionsID currentInstructionID = EnumClass.InstructionsID.Step1;
    [SerializeField] private EnumClass.InstructionsID endInstructionID;
    [SerializeField] private AudioClip taskCompletedClip, congratulationAudio;
    [SerializeField] private Button nextButton;

    private AudioSource audioSource;
    private bool isCompleted = false;

    /// <summary>
    /// Unity Awake callback. Initializes singleton instance and sets up instruction IDs.
    /// </summary>
    private void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }

        SetUpBaseConditionStructure();
    }

    /// <summary>
    /// Unity Start callback. Registers listeners and initializes the audio source.
    /// </summary>
    private void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (instructionDatas.Count == 0) return;

        EventManager.Instance.StartTutorial += InitializeCoroutine;
        EventManager.Instance.OnChangeInstruction += ActivateInstruction;
        // nextButton.onClick.AddListener(() => ActivateInstruction());
        nextButton.GetComponentInChildren<InteractableUnityEventWrapper>().WhenSelect.AddListener(() => ActivateInstruction()); 

    }

    /// <summary>
    /// Unity OnDisable callback. Cleans up TTS and event subscriptions.
    /// </summary>
    private void OnDisable()
    {
        EventManager.Instance.OnChangeInstruction -= ActivateInstruction;
        CleanUpTTS();
    }

    /// <summary>
    /// Unity OnDestroy callback. Cleans up TTS and event subscriptions.
    /// </summary>
    private void OnDestroy()
    {
        EventManager.Instance.OnChangeInstruction -= ActivateInstruction;
        CleanUpTTS();
    }

    /// <summary>
    /// Method to trigger the beginning of the tutorial or experiment via the event manager.
    /// </summary>
    public void StartInstruction()
    {
        EventManager.Instance.StartTutorial?.Invoke();
    }

    /// <summary>
    /// Sets up the base structure by assigning each instruction a unique ID based on its index.
    /// </summary>
    private void SetUpBaseConditionStructure()
    {
        for (int i = 0; i < instructionDatas.Count; i++)
        {
            instructionDatas[i].instructionsID = (EnumClass.InstructionsID)i;
        }
    }

    /// <summary>
    /// Adds a new condition to its corresponding instruction step.
    /// </summary>
    /// <param name="condition">The condition to add.</param>
    public void AddConditions(Conditions condition)
    {
        for (int i = 0; i < instructionDatas.Count; i++)
        {
            if (condition.instructionID == instructionDatas[i].instructionsID)
            {
                if (!instructionDatas[i].conditions.Contains(condition))
                {
                    instructionDatas[i].conditions.Add(condition);

                    if (instructionDatas[i].instructionsID == currentInstructionID)
                    {
                        condition.isActive = true;
                    }
                }
            }
        }
    }

    /// <summary>
    /// Begins coroutine to delay instruction initialization slightly after tutorial start.
    /// </summary>
    private void InitializeCoroutine()
    {
        StartCoroutine(DelayInstruction());
    }

    /// <summary>
    /// Coroutine that waits before calling the main instruction initialization.
    /// </summary>
    private IEnumerator DelayInstruction()
    {
        yield return new WaitForSeconds(0.1f);
        InitializeInstruction();
    }

    /// <summary>
    /// Activates the current instruction: conditions, UI update, events, and audio.
    /// </summary>
    public void InitializeInstruction()
    {
        foreach (var item in instructionDatas[(int)currentInstructionID].conditions)
        {
            item.isActive = true;
        }

        EventManager.Instance.InstructionChange(
            instructionDatas[(int)currentInstructionID].procedureData,
            instructionDatas[(int)currentInstructionID].clip
        );

        instructionDatas[(int)currentInstructionID].procedureEvent?.Invoke();
        PlayInstructionAudio();
    }

    /// <summary>
    /// Plays the audio clip indicating a task has been completed.
    /// </summary>
    private void PlayTaskCompletedAudio()
    {
        if (audioSource != null && taskCompletedClip != null)
        {
            audioSource.clip = taskCompletedClip;
            audioSource.Play();
        }
    }

    /// <summary>
    /// Proceeds to the next instruction if current conditions are met.
    /// </summary>
    [ContextMenu("Start")]
    public void ActivateInstruction()
    {
        PlayTaskCompletedAudio();
        if (!instructionDatas[(int)currentInstructionID].AllConditionsFinished())
            return;

        currentInstructionID++;
        Debug.Log("new instruction");

        if (currentInstructionID == endInstructionID)
        {
            EventManager.Instance.InstructionEnd();
            Debug.Log("finished");
            return;
        }

        InitializeInstruction();
    }

    /// <summary>
    /// Uses TTS to speak out the current instruction text.
    /// </summary>
    private void PlayInstructionAudio()
    {
        TTSSpeaker speaker = GameObject.FindFirstObjectByType<TTSSpeaker>();
        string text = instructionDatas[(int)currentInstructionID].procedureData;

        if (!string.IsNullOrWhiteSpace(text))
        {
            speaker.Speak(text);
        }
    }

    /// <summary>
    /// Stops any active TTS speech.
    /// </summary>
    private void CleanUpTTS()
    {
        TTSSpeaker speaker = GameObject.FindFirstObjectByType<TTSSpeaker>();
        if (speaker != null)
            speaker.Stop();
    }

    /// <summary>
    /// Unity Update callback. Handles button visibility and final congratulation logic.
    /// </summary>
    private void Update()
    {
        if (isCompleted) return;

        if (currentInstructionID != endInstructionID)
        {
            ActivateNextButton(instructionDatas[(int)currentInstructionID].canByPass);
        }
        else
        {
            PlayCongratulationAudio();
            isCompleted = true;
        }
    }

    /// <summary>
    /// Enables or disables the "Next" button based on the given value.
    /// </summary>
    /// <param name="value">True to show the button, false to hide it.</param>
    private void ActivateNextButton(bool value)
    {
        nextButton.gameObject.SetActive(value);
    }

    /// <summary>
    /// Plays the congratulatory audio once all instructions are complete.
    /// </summary>
    private void PlayCongratulationAudio()
    {
        if (audioSource != null && congratulationAudio != null)
        {
            audioSource.clip = congratulationAudio;
            audioSource.Play();
        }
    }
}

/// <summary>
/// Represents the data and logic tied to a single instructional step in the tutorial.
/// </summary>
[System.Serializable]
public class InstructionData
{
    public EnumClass.InstructionsID instructionsID;
    public List<Conditions> conditions = new List<Conditions>();
    [TextArea]
    public string procedureData;
    [Space]
    public UnityEvent procedureEvent;
    public VideoClip clip;
    public bool canByPass;

    /// <summary>
    /// Checks if all conditions for this instruction step have been marked as finished.
    /// </summary>
    /// <returns>True if all conditions are finished; otherwise, false.</returns>
    public bool AllConditionsFinished()
    {
        int count = 0;

        foreach (var item in conditions)
        {
            if (item.isFinished)
                count++;
        }

        return count == conditions.Count;
    }
}
