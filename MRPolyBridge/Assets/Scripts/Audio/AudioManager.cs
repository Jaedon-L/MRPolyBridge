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
    }

    public void ToggleMute(bool mute)
    {
        isMuted = mute;
        musicSource.volume = mute ? 0 : musicVolume;
    }

    public float GetMusicVolume() => musicVolume;
    public bool IsMuted() => isMuted;

    // === SFX Control ===
    public void SetSFXVolume(float volume)
    {
        sfxVolume = Mathf.Clamp01(volume);
    }

    public float GetSFXVolume() => sfxVolume;

    public void ToggleSFXMute(bool mute)
    {
        sfxMuted = mute;
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
}
