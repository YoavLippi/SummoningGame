using UnityEngine;

[CreateAssetMenu(fileName = "SoundContainer", menuName = "Scriptable Objects/UI/SoundContainer")]
public class SoundContainer : ScriptableObject
{
    public AudioManager.Sound associatedSound;
    public AudioClip soundClip;
}
