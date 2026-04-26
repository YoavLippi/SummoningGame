using System;
using Unity.Netcode;
using UnityEngine;

public class NetworkProjectile : NetworkBehaviour
{
    public NetworkVariable<Color> spellType =
        new NetworkVariable<Color>();

    [SerializeField] private ParticleSystem _particleSystem;


    private Vector3 direction;
    private float speed;

    public void Initialize(Vector3 dir, float spd, float duration)
    {
        direction = dir;
        speed = spd;
        //spellType.Value = type;
        Destroy(gameObject, duration);
    }

    private void Start()
    {
        _particleSystem = GetComponent<ParticleSystem>();
    }

    public void SetParticleColor(Color newColor)
    {
        var allSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in allSystems)
        {
            var main = ps.main;
            main.startColor = newColor;
        }
    }

    private void PlayAllParticleEffects()
    {
        // Play main system
        if (_particleSystem != null)
            _particleSystem.Play();

        // Play all child ParticleSystems
        ParticleSystem[] childSystems = GetComponentsInChildren<ParticleSystem>();
        foreach (var ps in childSystems)
        {
            if (ps != _particleSystem) // Avoid double-playing main
            {
                ps.Play();
            }
        }
    }

    public override void OnNetworkSpawn()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        SetParticleColor(spellType.Value);


        PlayAllParticleEffects();

        spellType.OnValueChanged += (oldColor, newColor) =>
        {
            SetParticleColor(newColor);
        };
    }

    void Update()
    {
        //if (!IsServer) return;
        //transform.position += direction * speed * Time.deltaTime;
    }
}
