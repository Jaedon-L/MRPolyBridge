using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.Video;

/// <summary>
/// Handles the display and playback of instructional content,
/// including updating text, playing videos, and showing end-of-instruction messages.
/// </summary>
[RequireComponent(typeof(VideoPlayer))]
public class InstructionMenu : MonoBehaviour
{
    [TextArea]
    [SerializeField] private string instructionEndMessage; // Message to display when all instructions are complete

    [SerializeField] private TMP_Text instructionText; // UI Text element to show current instruction
    [SerializeField] private Button homePageBtn; // Button to return to homepage once instructions end

    private VideoPlayer player;

    /// <summary>
    /// Unity Start callback. Registers event listeners and initializes the VideoPlayer.
    /// </summary>
    private void Start()
    {
        EventManager.Instance.OnInstructionChange += SetInstruction;
        EventManager.Instance.OnInstructionEnd += DisplayCompletedMessage;
        if (homePageBtn == null) return;
        homePageBtn.gameObject.SetActive(false);
        player = GetComponent<VideoPlayer>();
    }

    /// <summary>
    /// Updates the UI with the new instruction message and plays the associated video clip.
    /// </summary>
    /// <param name="message">The text of the instruction.</param>
    /// <param name="clip">The video clip associated with the instruction.</param>
    private void SetInstruction(string message, VideoClip clip)
    {
        instructionText.text = message;
        PlayInstructionVideo(clip);
    }

    /// <summary>
    /// Plays the given instruction video using the attached VideoPlayer component.
    /// </summary>
    /// <param name="clip">The video clip to play.</param>
    private void PlayInstructionVideo(VideoClip clip)
    {
        player.clip = clip;
        player.Play();
    }

    /// <summary>
    /// Displays the final message once all instructions have been completed
    /// and activates the homepage navigation button.
    /// </summary>
    private void DisplayCompletedMessage()
    {
        instructionText.text = instructionEndMessage;
        if (homePageBtn == null) return;
        homePageBtn.gameObject.SetActive(true);
    }
}
