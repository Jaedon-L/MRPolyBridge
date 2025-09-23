using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public class SFXClip
{
    public string id;       // unique ID to reference this SFX
    public AudioClip clip;  // the actual AudioClip
}

public class AudioManager : MonoBehaviour
{
    public static AudioManager Instance;

    [Header("Music Settings")]
    [SerializeField] private List<AudioClip> musicClips;
    [SerializeField] private AudioSource musicSource;
    [SerializeField] private float startDelay = 2f;

    [Header("SFX Settings")]
    [SerializeField] private List<SFXClip> sfxClips;
    [SerializeField] private AudioSource sfxSource;  // dedicated SFX audio source

    private Dictionary<string, AudioClip> sfxDictionary = new Dictionary<string, AudioClip>();

    // Music playlist
    private List<int> shuffledIndices = new List<int>();
    private int currentTrackIndex = 0;
    private Coroutine playlistRoutine;

    private float musicVolume = 1f;
    private bool isMuted = false;

    // --- New SFX Volume/Mute ---
    private float sfxVolume = 1f;
    private bool sfxMuted = false;

    private const string KEY_MUSIC_VOLUME = "AM_MusicVolume";
    private const string KEY_MUSIC_MUTED = "AM_MusicMuted"; // int 0/1
    private const string KEY_SFX_VOLUME = "AM_SFXVolume";
    private const string KEY_SFX_MUTED = "AM_SFXMuted";   // int 0/1

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (musicSource == null)
            musicSource = gameObject.AddComponent<AudioSource>();
        if (sfxSource == null)
            sfxSource = gameObject.AddComponent<AudioSource>();

        musicSource.playOnAwake = false;
        musicSource.loop = false;
        LoadSoundSettings();

        // Build SFX dictionary
        foreach (var sfx in sfxClips)
        {
            if (!sfxDictionary.ContainsKey(sfx.id) && sfx.clip != null)
            {
                sfxDictionary.Add(sfx.id, sfx.clip);
            }
        }
    }

    private void Start()
    {
        ApplyMusicVolumeToSource();

        if (musicClips.Count > 0)
        {
            ShuffleTracks();
            playlistRoutine = StartCoroutine(RunPlaylist());
        }
    }

    // === Music Playlist Coroutine ===
    private System.Collections.IEnumerator RunPlaylist()
    {
        if (startDelay > 0f) yield return new WaitForSeconds(startDelay);

        while (true)
        {
            PlayTrack(currentTrackIndex);
            yield return new WaitUntil(() => !musicSource.isPlaying);
            NextTrack();
        }
    }

    private void PlayTrack(int index)
    {
        int trackIndex = shuffledIndices[index];
        musicSource.clip = musicClips[trackIndex];
        musicSource.volume = isMuted ? 0 : musicVolume;
        musicSource.Play();
    }

    private void NextTrack()
    {
        currentTrackIndex++;
        if (currentTrackIndex >= shuffledIndices.Count)
        {
            ShuffleTracks();
            currentTrackIndex = 0;
        }
    }

    private void ShuffleTracks()
    {
        shuffledIndices.Clear();
        for (int i = 0; i < musicClips.Count; i++)
            shuffledIndices.Add(i);

        // Fisher-Yates shuffle
        for (int i = 0; i < shuffledIndices.Count; i++)
        {
            int rand = Random.Range(i, shuffledIndices.Count);
            int temp = shuffledIndices[i];
            shuffledIndices[i] = shuffledIndices[rand];
            shuffledIndices[rand] = temp;
        }
    }

    // === Public Music Control ===
    public void SetMusicVolume(float volume)
    {
        musicVolume = Mathf.Clamp01(volume);
        if (!isMuted) musicSource.volume = musicVolume;

        SaveSoundSettings();
    }

    public void ToggleMute(bool mute)
    {
        isMuted = mute;
        musicSource.volume = mute ? 0 : musicVolume;

        SaveSoundSettings();
    }

    public float GetMusicVolume() => musicVolume;
    public bool IsMuted() => isMuted;

    // === SFX Control ===
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
        SaveSoundSettings();
    }

    public float GetSFXVolume() => sfxVolume;

    public void ToggleSFXMute(bool mute)
    {
        sfxMuted = mute;
        SaveSoundSettings();
    }

    public bool IsSFXMuted() => sfxMuted;

    public void PlaySFX(string id)
    {
        if (sfxSource == null) return;

        if (sfxDictionary.TryGetValue(id, out AudioClip clip))
        {
            float volume = sfxMuted ? 0f : sfxVolume;
            sfxSource.PlayOneShot(clip, volume);
        }
        else
        {
            Debug.LogWarning($"SFX with ID '{id}' not found!");
        }
    }

    // =========================
    // PlayerPrefs save / load
    // =========================
    public void SaveSoundSettings()
    {
        PlayerPrefs.SetFloat(KEY_MUSIC_VOLUME, musicVolume);
        PlayerPrefs.SetInt(KEY_MUSIC_MUTED, isMuted ? 1 : 0);
        PlayerPrefs.SetFloat(KEY_SFX_VOLUME, sfxVolume);
        PlayerPrefs.SetInt(KEY_SFX_MUTED, sfxMuted ? 1 : 0);

        PlayerPrefs.Save();
    }

    private void LoadSoundSettings()
    {
        // defaults: music 1.0, not muted; sfx 1.0, not muted
        musicVolume = PlayerPrefs.GetFloat(KEY_MUSIC_VOLUME, 1f);
        isMuted = PlayerPrefs.GetInt(KEY_MUSIC_MUTED, 0) == 1;
        sfxVolume = PlayerPrefs.GetFloat(KEY_SFX_VOLUME, 1f);
        sfxMuted = PlayerPrefs.GetInt(KEY_SFX_MUTED, 0) == 1;
    }

    private void ApplyMusicVolumeToSource()
    {
        if (musicSource != null)
            musicSource.volume = isMuted ? 0f : musicVolume;
    }

    // optional: call SaveSoundSettings again on shutdown as a last-resort
    private void OnApplicationQuit()
    {
        SaveSoundSettings();
    }

    private void OnDisable()
    {
        // in editor playstop this helps persist final values
        if (Application.isPlaying) SaveSoundSettings();
    }

    // Helper to reset saved settings (useful for testing)
    public void ResetSoundSettings()
    {
        PlayerPrefs.DeleteKey(KEY_MUSIC_VOLUME);
        PlayerPrefs.DeleteKey(KEY_MUSIC_MUTED);
        PlayerPrefs.DeleteKey(KEY_SFX_VOLUME);
        PlayerPrefs.DeleteKey(KEY_SFX_MUTED);
        PlayerPrefs.Save();

        // revert in memory to defaults
        musicVolume = 1f;
        isMuted = false;
        sfxVolume = 1f;
        sfxMuted = false;

        ApplyMusicVolumeToSource();
    }
}
