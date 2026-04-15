using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class AudioManager : MonoBehaviour
{
    [Header("Interaction Audio")]
    public List<SoundContainer> allSounds;
    public AudioSource uiAudio;
    public bool isGameMusicPlaying = false;
    
    //enum to denote the correct audio sounds
    [Serializable]
    public enum Sound
    {
        ButtonPressed,
        GameMusic,
        RoundMusic,
        ButtonHover
    }

    private void Start()
    {
        uiAudio = GetComponent<AudioSource>();
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

    public void ToggleMusic ()
    {
        if (isGameMusicPlaying)
        {
            //   gameMusic.Pause();
            isGameMusicPlaying = false;
        }
        else
        {
            //   gameMusic.Play();
            isGameMusicPlaying = true;
        }
    }
    
    public void PlaySound(string soundName)
    {
        //can't expose enum in event, so we'll have to do it like this - ignores upper and lower case
        Sound correspondingSound = (Sound)Enum.Parse(typeof(Sound), soundName, true);
        if (ContainsItemWithSound(allSounds, correspondingSound))
        {
            uiAudio.PlayOneShot(GetSound(allSounds, correspondingSound).soundClip);
        }
    }
}
