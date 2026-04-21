using Unity.Netcode;
using UnityEngine;

public class NetworkProjectile : NetworkBehaviour
{
    private NetworkVariable<NetworkSpell.SpellType> spellType =
        new NetworkVariable<NetworkSpell.SpellType>();

    private Vector3 direction;
    private float speed;

    public void Initialize(Vector3 dir, float spd, float duration, NetworkSpell.SpellType type)
    {
        direction = dir;
        speed = spd;
        spellType.Value = type;
        Destroy(gameObject, duration);
    }

    void Update()
    {
        if (!IsServer) return;
        transform.position += direction * speed * Time.deltaTime;
    }
}
