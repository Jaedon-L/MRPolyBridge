using Meta.WitAi.TTS.Utilities;
using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using UnityEngine.Video;

[RequireComponent(typeof(AudioSource))]
public class InstructionManager : MonoBehaviour
{
    public static InstructionManager Instance { get; private set; }

    public List<InstructionData> instructionDatas = new List<InstructionData>();
    public EnumClass.InstructionsID currentInstructionID = EnumClass.InstructionsID.Step1;
    public EnumClass.InstructionsID endInstructionID;
    private AudioSource audioSource;
    public AudioClip taskCompletedClip, congratulationAudio;
    public Button nextButton;
    private bool isCompleted = false;

    [HideInInspector]
    public List<GameObject> allSelectedObject = new List<GameObject>();

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

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        if (instructionDatas.Count == 0) return;
        EventManager.Instance.StartExperiment += InitializeCoroutine;
        EventManager.Instance.OnChangeInstruction += ActivateInstruction;
        nextButton.onClick.AddListener(() => ActivateInstruction());
    }

    private void OnDisable()
    {
        EventManager.Instance.OnChangeInstruction -= ActivateInstruction;
        CleanUpTTS();
    }

    private void OnDestroy()
    {
        EventManager.Instance.OnChangeInstruction -= ActivateInstruction;
        CleanUpTTS();
    }

    public void StartExperiment()
    {
        EventManager.Instance.StartExperiment?.Invoke();
    }

    private void SetUpBaseConditionStructure()
    {
        for (int i = 0; i < instructionDatas.Count; i++)
        {
            instructionDatas[i].instructionsID = (EnumClass.InstructionsID)i;
        }
    }

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

    void InitializeCoroutine()
    {
        StartCoroutine(DelayInstruction());
    }

    IEnumerator DelayInstruction()
    {
        yield return new WaitForSeconds(0.1f);
        InitializeInstruction();
    }

    public void InitializeInstruction()
    {
        foreach (var item in instructionDatas[(int)currentInstructionID].conditions)
        {
            item.isActive = true;
        }

        EventManager.Instance.InstructionChange(instructionDatas[(int)currentInstructionID].procedureData, instructionDatas[(int)currentInstructionID].clip);

        instructionDatas[(int)currentInstructionID].procedureEvent?.Invoke();
        PlayInstructionAudio();
    }

    private void PlayTaskCompletedAudio()
    {
        if (audioSource != null && taskCompletedClip != null)
        {
            audioSource.clip = taskCompletedClip;
            audioSource.Play();
        }
    }

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

    private void PlayInstructionAudio()
    {
        TTSSpeaker speaker = GameObject.FindFirstObjectByType<TTSSpeaker>();
        string text = instructionDatas[(int)currentInstructionID].procedureData;
        if(text != "" && !string.IsNullOrWhiteSpace(text))
        {
            speaker.Speak(text);
        }
    }

    private void CleanUpTTS()
    {
        TTSSpeaker speaker = GameObject.FindFirstObjectByType<TTSSpeaker>();
        if (speaker != null)
            speaker.Stop();
    }

    private void Update()
    {
        if (isCompleted) return;
        if (currentInstructionID != endInstructionID)
        {
            if (instructionDatas[(int)currentInstructionID].canByPass)
            {
                ActivateNextButton(true);
            }
            else
            {
                ActivateNextButton(false);
            }
        }
        else
        {
            PlayCongratulationAudio();
            isCompleted = true;
        }
    }

    private void ActivateNextButton(bool value)
    {
        nextButton.gameObject.SetActive(value);
    }

    private void PlayCongratulationAudio()
    {
        if (audioSource != null && congratulationAudio != null)
        {
            audioSource.clip = congratulationAudio;
            audioSource.Play();
        }
    }
}

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

    public bool AllConditionsFinished()
    {
        int count = 0;

        foreach (var item in conditions)
        {
            if (item.isFinished)
                count++;
        }

        if (count == conditions.Count)
            return true;
        else
            return false;
    }
}