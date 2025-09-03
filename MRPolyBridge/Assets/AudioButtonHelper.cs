using UnityEngine;
using UnityEngine.UI;

public enum AudioType
{
    Music,
    SFX
}

public class AudioButtonHelper : MonoBehaviour
{
    [Header("Volume Settings")]
    [SerializeField] private RectTransform musicFillBar; // UI bar for Music
    [SerializeField] private RectTransform sfxFillBar;   // UI bar for SFX
    [SerializeField] private float maxFillWidth = 250f;  
    [SerializeField] private int steps = 10;             
    [SerializeField] private float fillLerpSpeed = 10f;  

    [Header("Mute Button Visuals")]
    [SerializeField] private Image musicMuteButtonImage;
    [SerializeField] private Image musicMuteOverlayImage;
    [SerializeField] private Image sfxMuteButtonImage;
    [SerializeField] private Image sfxMuteOverlayImage;
    [SerializeField] private Color soundOnColor = Color.green;
    [SerializeField] private Color mutedColor = Color.red;

    private float stepSize;
    private float currentMusicVolume;
    private float currentSFXVolume;
    private float targetMusicWidth;
    private float targetSFXWidth;

    private void Start()
    {
        stepSize = 1f / steps;

        currentMusicVolume = AudioManager.Instance.GetMusicVolume();
        currentSFXVolume = AudioManager.Instance.GetSFXVolume();

        targetMusicWidth = currentMusicVolume * maxFillWidth;
        targetSFXWidth = currentSFXVolume * maxFillWidth;

        UpdateFillBarInstant();
        UpdateMuteVisuals();
    }

    private void Update()
    {
        // Smoothly animate Music fill
        if (musicFillBar != null)
        {
            float newWidth = Mathf.Lerp(musicFillBar.sizeDelta.x, targetMusicWidth, Time.deltaTime * fillLerpSpeed);
            musicFillBar.sizeDelta = new Vector2(newWidth, musicFillBar.sizeDelta.y);
        }

        // Smoothly animate SFX fill
        if (sfxFillBar != null)
        {
            float newWidth = Mathf.Lerp(sfxFillBar.sizeDelta.x, targetSFXWidth, Time.deltaTime * fillLerpSpeed);
            sfxFillBar.sizeDelta = new Vector2(newWidth, sfxFillBar.sizeDelta.y);
        }
    }

    // ===================== Music Controls =====================
    [ContextMenu("increaseMusic")]
    public void IncreaseMusicVolume()
    {
        if (AudioManager.Instance.IsMuted())
            AudioManager.Instance.ToggleMute(false);

        currentMusicVolume = Mathf.Clamp01(currentMusicVolume + stepSize);
        AudioManager.Instance.SetMusicVolume(currentMusicVolume);
        targetMusicWidth = currentMusicVolume * maxFillWidth;

        UpdateMuteVisuals();
    }
    [ContextMenu("decreaseMusic")]
    public void DecreaseMusicVolume()
    {
        if (AudioManager.Instance.IsMuted())
            AudioManager.Instance.ToggleMute(false);

        currentMusicVolume = Mathf.Clamp01(currentMusicVolume - stepSize);
        AudioManager.Instance.SetMusicVolume(currentMusicVolume);
        targetMusicWidth = currentMusicVolume * maxFillWidth;

        if (currentMusicVolume <= 0f && !AudioManager.Instance.IsMuted())
            AudioManager.Instance.ToggleMute(true);

        UpdateMuteVisuals();
    }
    [ContextMenu("toggleMusic")]
    public void ToggleMusicMute()
    {
        bool newMute = !AudioManager.Instance.IsMuted();
        AudioManager.Instance.ToggleMute(newMute);
        targetMusicWidth = newMute ? 0f : currentMusicVolume * maxFillWidth;

        UpdateMuteVisuals();
    }

    // ===================== SFX Controls =====================
    [ContextMenu("increaseSFX")]
    public void IncreaseSFXVolume()
    {
        if (AudioManager.Instance.IsSFXMuted())
            AudioManager.Instance.ToggleSFXMute(false);

        currentSFXVolume = Mathf.Clamp01(currentSFXVolume + stepSize);
        AudioManager.Instance.SetSFXVolume(currentSFXVolume);
        targetSFXWidth = currentSFXVolume * maxFillWidth;

        UpdateMuteVisuals();
    }
    [ContextMenu("decreaseSFX")]
    public void DecreaseSFXVolume()
    {
        if (AudioManager.Instance.IsSFXMuted())
            AudioManager.Instance.ToggleSFXMute(false);

        currentSFXVolume = Mathf.Clamp01(currentSFXVolume - stepSize);
        AudioManager.Instance.SetSFXVolume(currentSFXVolume);
        targetSFXWidth = currentSFXVolume * maxFillWidth;

        if (currentSFXVolume <= 0f && !AudioManager.Instance.IsSFXMuted())
            AudioManager.Instance.ToggleSFXMute(true);

        UpdateMuteVisuals();
    }
    [ContextMenu("toggleSFX")]
    public void ToggleSFXMute()
    {
        bool newMute = !AudioManager.Instance.IsSFXMuted();
        AudioManager.Instance.ToggleSFXMute(newMute);
        targetSFXWidth = newMute ? 0f : currentSFXVolume * maxFillWidth;

        UpdateMuteVisuals();
    }

    // ===================== Helper Methods =====================
    private void UpdateFillBarInstant()
    {
        if (musicFillBar != null)
            musicFillBar.sizeDelta = new Vector2(targetMusicWidth, musicFillBar.sizeDelta.y);

        if (sfxFillBar != null)
            sfxFillBar.sizeDelta = new Vector2(targetSFXWidth, sfxFillBar.sizeDelta.y);
    }

    private void UpdateMuteVisuals()
    {
        // Music visuals
        if (musicMuteButtonImage != null)
            musicMuteButtonImage.color = AudioManager.Instance.IsMuted() ? mutedColor : soundOnColor;
        if (musicMuteOverlayImage != null)
            musicMuteOverlayImage.enabled = AudioManager.Instance.IsMuted();

        // SFX visuals
        if (sfxMuteButtonImage != null)
            sfxMuteButtonImage.color = AudioManager.Instance.IsSFXMuted() ? mutedColor : soundOnColor;
        if (sfxMuteOverlayImage != null)
            sfxMuteOverlayImage.enabled = AudioManager.Instance.IsSFXMuted();
    }
}