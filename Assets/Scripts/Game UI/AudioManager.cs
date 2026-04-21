using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Rendering;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("Interaction Audio")]
    public List<SoundContainer> allSounds;
    public AudioSource uiAudio;
    public bool isGameMusicPlaying = false;

    public static AudioManager Instance;

    //enum to denote the correct audio sounds
    [Serializable]
    public enum Sound
    {
        ButtonPressed,
        GameMusic,
        RoundMusic,
        ButtonHover
    }

    public void SetVolume_GameMusic(float volume) => SetVolume(Sound.GameMusic, volume);
    public void SetVolume_RoundMusic(float volume) => SetVolume(Sound.RoundMusic, volume);
    public void SetVolume_ButtonPressed(float volume) => SetVolume(Sound.ButtonPressed, volume);
    public void SetVolume_ButtonHover(float volume) => SetVolume(Sound.ButtonHover, volume);

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        uiAudio = GetComponent<AudioSource>();
        SetVolume(Sound.GameMusic, PlayerPrefs.GetFloat("MusicVolume", 0.5f));

        foreach (var container in allSounds)
            container.volume = PlayerPrefs.GetFloat($"Volume_{container.associatedSound}", 1f);
    }

    private bool ContainsItemWithSound(List<SoundContainer> inList, Sound associatedSound)
    {
        // Use LINQ's Any() to check if any item in the list matches t
        return inList.Any(item => item.associatedSound == associatedSound);
    }

    private SoundContainer GetSound(List<SoundContainer> inList, Sound associatedSound)
    {
        // Use LINQ's FirstOrDefault() to retrieve the item itself
        return inList.FirstOrDefault(item => item.associatedSound == associatedSound);
    }

    // Music control logic (General logic we may need)

    public void ToggleMusic()
    {
        if (uiAudio.isPlaying)
            uiAudio.Pause();

        else
            uiAudio.Play();

        isGameMusicPlaying = uiAudio.isPlaying; // syncs the bool with the actual state of the music

    }

    public void PlaySound(string soundName)
    {
        //can't expose enum in event, so we'll have to do it like this - ignores upper and lower case
        Sound correspondingSound = (Sound)Enum.Parse(typeof(Sound), soundName, true);
        if (!ContainsItemWithSound(allSounds, correspondingSound)) return;

        SoundContainer container = GetSound(allSounds, correspondingSound);
        uiAudio.PlayOneShot(container.soundClip, container.volume);
        }

    public void SetVolume(Sound sound, float volume)
    {
        volume = Mathf.Clamp01(volume); // Ensure the volume is between 0 and 1
        SoundContainer container = GetSound(allSounds, sound); 
        if (container == null) return;

        container.volume = volume;
        PlayerPrefs.SetFloat($"Volume_{sound}", volume); // Save the volume setting for this sound using PlayerPrefs
        PlayerPrefs.Save();
    }

    public void SetVolume(float volume, params Sound[] sounds)
    {
        foreach (Sound sound in sounds)
            SetVolume(sound, volume); // reuse the single sound volume setting method to set the volume for each sound in the array
    }
}
