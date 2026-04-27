using UnityEngine;

[RequireComponent(typeof(AudioSource))]
public class MusicManager : MonoBehaviour
{
    public AudioClip gameMusicClip;
    private AudioSource audioSource;
    public static MusicManager Instance;

    private const string VolumeKey = "Volume_GameMusic";

    void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
        audioSource = GetComponent<AudioSource>();
        audioSource.clip = gameMusicClip;
        audioSource.loop = true;
        audioSource.playOnAwake = false;
    }

    void Start()
    {
        float savedVolume = PlayerPrefs.GetFloat(VolumeKey, 1f);
        audioSource.volume = savedVolume;
        audioSource.Play();
    }

    public void SetVolume(float volume)
    {
        volume = Mathf.Clamp01(volume);
        audioSource.volume = volume;
        PlayerPrefs.SetFloat(VolumeKey, volume);
        PlayerPrefs.Save();
    }
}