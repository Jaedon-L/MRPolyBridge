using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

public class InstructionMenu : MonoBehaviour
{
    public TextMeshProUGUI instructionText;
    public VideoPlayer player;
    // Start is called before the first frame update
    void Start()
    {
        EventManager.Instance.OnInstructionChange += SetInstruction;
    }

    private void SetInstruction(string message, VideoClip clip)
    {
        instructionText.text = message;
        PlayInstructionVideo(clip);
    }

    private void PlayInstructionVideo(VideoClip clip)
    {
        if (player == null) return;
        player.clip = clip;
        player.Play();
    }
}
