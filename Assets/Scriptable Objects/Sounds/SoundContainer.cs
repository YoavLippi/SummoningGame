using UnityEngine;

[CreateAssetMenu(fileName = "SoundContainer", menuName = "Scriptable Objects/UI/SoundContainer")]
public class SoundContainer : ScriptableObject
{
    public AudioManager.Sound associatedSound;
    public AudioClip soundClip;
    [Range(0f, 1f)]
    public float volume = 1f;
}
