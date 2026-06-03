using System;
using System.Collections;
using Unity.Netcode;
using UnityEngine;
public class NetworkProjectile : NetworkBehaviour
{
    public NetworkVariable<Color> spellType =
        new NetworkVariable<Color>(Color.clear);
    [SerializeField] private ParticleSystem _particleSystem;
    private Vector3 direction;
    private float speed;
    private float lifetime;
    public void Initialize(Vector3 dir, float spd, float duration)
    {
        direction = dir;
        speed = spd;
        lifetime = duration;
        //spellType.Value = type;
        // Destroy(gameObject, duration);
        StartCoroutine(LifetimeSequence(duration));
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
    /* private void PlayAllParticleEffects()
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
    } */

    // CHANGED: removed Play() from here — InitializeClientRpc handles color + play
    // to avoid race condition where OnNetworkSpawn reads default color before
    // spellType.Value is set post-spawn
    public override void OnNetworkSpawn()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        // SetParticleColor(spellType.Value);
        // PlayAllParticleEffects();
        // if (_particleSystem != null)
        //     _particleSystem.Play();
        spellType.OnValueChanged += (oldColor, newColor) =>
        {
            SetParticleColor(newColor);
        };
    }

    // ADDED: pushes correct color to all clients immediately post-spawn
    // replaces the Play() call that was in OnNetworkSpawn
    [ClientRpc]
    public void InitializeClientRpc(Color spellColor)
    {
        SetParticleColor(spellColor);
        if (_particleSystem != null)
            _particleSystem.Play();
    }

    void Update()
    {
        // if (!IsServer) return;
        // transform.position += direction * speed * Time.deltaTime;
    }
    private IEnumerator LifetimeSequence(float moveDuration)
    {
        // 1. Move for the gameplay duration (rocket flies)
        yield return new WaitForSeconds(moveDuration);
        speed = 0f; // Stop moving before particles die
        // 2. Stop emission so particles begin dying naturally
        var allSystems = GetComponentsInChildren<ParticleSystem>(true);
        foreach (var ps in allSystems)
        {
            ps.Stop(false, ParticleSystemStopBehavior.StopEmitting);
        }
        // 3. Wait until ALL particles are dead, including sub-emitter explosions
        bool anyAlive = true;
        while (anyAlive)
        {
            anyAlive = false;
            foreach (var ps in allSystems)
            {
                if (ps.IsAlive(true)) // true = include sub-emitters
                {
                    anyAlive = true;
                    break;
                }
            }
            yield return null;
        }
        // 4. Only now is it safe to kill the networked object
        if (NetworkObject != null && NetworkObject.IsSpawned)
            NetworkObject.Despawn();
    }
}