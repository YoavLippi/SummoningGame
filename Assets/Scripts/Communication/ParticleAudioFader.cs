using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ParticleAudioFader : MonoBehaviour
{
    [SerializeField] private AudioSource _audioSource;
    [SerializeField] private AudioClip _clip;
    [SerializeField] private ParticleSystem _fadeTarget;
    [SerializeField] private int _playOrder;

    private static Dictionary<int, ParticleAudioFader> _registry = new Dictionary<int, ParticleAudioFader>();

    private void Start()
    {
        if (_playOrder == 0)
            StartCoroutine(PlaySequence());
    }

    private void Update()
    {
        if (_playOrder == 0) return;
        if (_audioSource.isPlaying) return;
        if (_fadeTarget.isPlaying && !_audioSource.isPlaying)
            StartCoroutine(PlaySequence());
    }

    private IEnumerator PlaySequence()
    {
        _audioSource.PlayOneShot(_clip);
        Debug.Log($"{gameObject.name} played sound at volume {_audioSource.volume} position {transform.position}");
        Debug.Log($"{gameObject.name} played sound at {Time.time}");
        float fadeTime = _fadeTarget.main.startLifetime.constantMax;
        Debug.Log($"{gameObject.name} fadeTime: {fadeTime}");
        yield return StartCoroutine(FadeOut(fadeTime));

       /* int next = _playOrder + 1;
        if (_registry.ContainsKey(next))
            _registry[next].Play(); */
    }

    private IEnumerator FadeOut(float duration)
    {
        float startVolume = _audioSource.volume;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            _audioSource.volume = Mathf.Lerp(startVolume, 0f, elapsed / duration);
            yield return null;
        }

        _audioSource.Stop();
        _audioSource.volume = startVolume;
    }
}