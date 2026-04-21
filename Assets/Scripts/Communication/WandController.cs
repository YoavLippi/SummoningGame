using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct SpellCharge
{
    public NetworkSpell.SpellType spellType;
    public GameObject prefab;
}

public class WandController : NetworkBehaviour
{
    public SpellCharge[] spellCharges;
    public float spellSpeed;
    public float spellDuration;

    private NetworkSpell networkSpell;
    private int currentColorIndex = 0;

    public override void OnNetworkSpawn()
    {
        networkSpell = GetComponent<NetworkSpell>();
    }

    void Update()
    {
        if (!IsOwner) return;

        var gamepad = Gamepad.current;
        if (gamepad == null) return;

        if (gamepad.buttonEast.wasPressedThisFrame)
            Fire();

        if (gamepad.leftShoulder.wasPressedThisFrame)
            CycleColor(-1);

        if (gamepad.rightShoulder.wasPressedThisFrame)
            CycleColor(1);
    }

    void CycleColor(int direction)
    {
        int count = System.Enum.GetValues(typeof(NetworkSpell.SpellType)).Length;
        currentColorIndex = (currentColorIndex + direction + count) % count;
        networkSpell.CurrentSpellType.Value = (NetworkSpell.SpellType)currentColorIndex;
    }

    void Fire()
    {
        SpellCharge selected = GetCurrentSpellCharge();
        FireServerRpc(transform.position, transform.up, selected.spellType);
    }

    SpellCharge GetCurrentSpellCharge()
    {
        NetworkSpell.SpellType current = networkSpell.CurrentSpellType.Value;
        foreach (SpellCharge charge in spellCharges)
        {
            if (charge.spellType == current) return charge;
        }

        return spellCharges[0];
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 position, Vector3 direction, NetworkSpell.SpellType spellType)
    {
        SpellCharge selected = GetSpellChargeByType(spellType);
        GameObject spell = Instantiate(selected.prefab, position, Quaternion.identity);
        NetworkObject netObj = spell.GetComponent<NetworkObject>();
        netObj.Spawn();

        NetworkProjectile projectile = spell.GetComponent<NetworkProjectile>();
        projectile?.Initialize(direction, spellSpeed, spellDuration, spellType);
    }

    SpellCharge GetSpellChargeByType(NetworkSpell.SpellType spellType)
    {
        foreach (SpellCharge ammo in spellCharges)
        {
            if (ammo.spellType == spellType) return ammo;
        }
        return spellCharges[0];
    }
}
