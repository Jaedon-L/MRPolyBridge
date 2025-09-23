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

    private void Awake()
    {
        stepSize = 1f / Mathf.Max(1, steps);
    }

    private void OnEnable()
    {
        // Always refresh when enabled so UI matches saved manager state
        Refresh();
    }

    private void Start()
    {
        // Ensure UI immediately matches persisted settings on first start
        Refresh();
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

    // ===================== Public API =====================
    // Call this if settings may have changed outside the helper
    public void Refresh()
    {
        if (AudioManager.Instance == null) return;

        // Read current saved values
        currentMusicVolume = AudioManager.Instance.GetMusicVolume();
        currentSFXVolume = AudioManager.Instance.GetSFXVolume();

        // If muted, show the bar as empty (but keep currentVolume intact)
        targetMusicWidth = AudioManager.Instance.IsMuted() ? 0f : currentMusicVolume * maxFillWidth;
        targetSFXWidth = AudioManager.Instance.IsSFXMuted() ? 0f : currentSFXVolume * maxFillWidth;

        // Immediately set fill sizes so UI matches saved/muted state on load
        UpdateFillBarInstant();
        UpdateMuteVisuals();
    }

    // ===================== Music Controls =====================
    [ContextMenu("increaseMusic")]
    public void IncreaseMusicVolume()
    {
        if (AudioManager.Instance == null) return;

        // If currently muted, unmute first (and restore visuals)
        if (AudioManager.Instance.IsMuted())
            AudioManager.Instance.ToggleMute(false);

        currentMusicVolume = Mathf.Clamp01(currentMusicVolume + stepSize);
        AudioManager.Instance.SetMusicVolume(currentMusicVolume);

        // when unmuted, target should reflect new volume
        targetMusicWidth = currentMusicVolume * maxFillWidth;

        UpdateMuteVisuals();
    }

    [ContextMenu("decreaseMusic")]
    public void DecreaseMusicVolume()
    {
        if (AudioManager.Instance == null) return;

        if (AudioManager.Instance.IsMuted())
            AudioManager.Instance.ToggleMute(false);

        currentMusicVolume = Mathf.Clamp01(currentMusicVolume - stepSize);
        AudioManager.Instance.SetMusicVolume(currentMusicVolume);
        targetMusicWidth = currentMusicVolume * maxFillWidth;

        // auto-mute when hitting zero
        if (currentMusicVolume <= 0f && !AudioManager.Instance.IsMuted())
        {
            AudioManager.Instance.ToggleMute(true);
            // reflect mute visually (targetWidth already set to 0 after ToggleMute via Refresh below)
        }

        // ensure visuals are consistent (ToggleMute will save state, but call Refresh to update target if muted)
        Refresh();
    }

    [ContextMenu("toggleMusic")]
    public void ToggleMusicMute()
    {
        if (AudioManager.Instance == null) return;

        bool newMute = !AudioManager.Instance.IsMuted();
        AudioManager.Instance.ToggleMute(newMute);

        // Set visual target: 0 if muted, else the stored volume
        targetMusicWidth = newMute ? 0f : currentMusicVolume * maxFillWidth;
        UpdateMuteVisuals();
    }

    // ===================== SFX Controls =====================
    [ContextMenu("increaseSFX")]
    public void IncreaseSFXVolume()
    {
        if (AudioManager.Instance == null) return;

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
        if (AudioManager.Instance == null) return;

        if (AudioManager.Instance.IsSFXMuted())
            AudioManager.Instance.ToggleSFXMute(false);

        currentSFXVolume = Mathf.Clamp01(currentSFXVolume - stepSize);
        AudioManager.Instance.SetSFXVolume(currentSFXVolume);
        targetSFXWidth = currentSFXVolume * maxFillWidth;

        if (currentSFXVolume <= 0f && !AudioManager.Instance.IsSFXMuted())
        {
            AudioManager.Instance.ToggleSFXMute(true);
        }

        Refresh();
    }

    [ContextMenu("toggleSFX")]
    public void ToggleSFXMute()
    {
        if (AudioManager.Instance == null) return;

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
        if (AudioManager.Instance == null) return;

        // Music visuals
        bool musicMuted = AudioManager.Instance.IsMuted();
        if (musicMuteButtonImage != null)
            musicMuteButtonImage.color = musicMuted ? mutedColor : soundOnColor;
        if (musicMuteOverlayImage != null)
            musicMuteOverlayImage.enabled = musicMuted;

        // SFX visuals
        bool sfxMuted = AudioManager.Instance.IsSFXMuted();
        if (sfxMuteButtonImage != null)
            sfxMuteButtonImage.color = sfxMuted ? mutedColor : soundOnColor;
        if (sfxMuteOverlayImage != null)
            sfxMuteOverlayImage.enabled = sfxMuted;
    }
}
