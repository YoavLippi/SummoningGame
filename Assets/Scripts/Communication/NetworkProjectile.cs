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
        _particleSystem.startColor = newColor;
    }

    public override void OnNetworkSpawn()
    {
        _particleSystem = GetComponent<ParticleSystem>();
        SetParticleColor(spellType.Value);
        
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
