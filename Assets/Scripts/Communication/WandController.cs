using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

[System.Serializable]
public struct SpellCharge
{
    //public NetworkSpell.SpellType spellType;
    public InteractionHandler.Color spellColor;
    public GameObject prefab;
}

public class WandController : NetworkBehaviour
{
    public GameObject spellPrefab;
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
        int count = System.Enum.GetValues(typeof(InteractionHandler.Color)).Length;
        currentColorIndex = (currentColorIndex + direction + count) % count;
        networkSpell.CurrentSpellType.Value = (InteractionHandler.Color)currentColorIndex;
    }

    void Fire()
    {
        SpellCharge selected = GetCurrentSpellCharge();
        FireServerRpc(transform.position, transform.up, selected.spellColor);
    }

    SpellCharge GetCurrentSpellCharge()
    {
        InteractionHandler.Color current = networkSpell.CurrentSpellType.Value;
        foreach (SpellCharge charge in spellCharges)
        {
            if (charge.spellColor == current) return charge;
        }

        return spellCharges[0];
    }

    [ServerRpc]
    private void FireServerRpc(Vector3 position, Vector3 direction, InteractionHandler.Color spellType)
    {
        SpellCharge selected = GetSpellChargeByType(spellType);
        GameObject spell = Instantiate(selected.prefab, position, Quaternion.identity);
        NetworkObject netObj = spell.GetComponent<NetworkObject>();
        netObj.Spawn();

        NetworkProjectile projectile = spell.GetComponent<NetworkProjectile>();
        projectile?.Initialize(direction, spellSpeed, spellDuration);
    }

    public void FireWithColor(Vector3 position, Quaternion direction, Color spellColor)
    {
        //Debug.Log("Trying to fire spell");
        GameObject spell = Instantiate(spellPrefab, position, direction);
        NetworkObject netObj = spell.GetComponent<NetworkObject>();
        netObj.Spawn();

        NetworkProjectile projectile = spell.GetComponent<NetworkProjectile>();
        if (projectile)
        {
            //just using zero as a placeholder value
            projectile.Initialize(Vector3.zero, spellSpeed, spellDuration);
            projectile.spellType.Value = spellColor;
        }
    }

    SpellCharge GetSpellChargeByType(InteractionHandler.Color spellType)
    {
        foreach (SpellCharge ammo in spellCharges)
        {
            if (ammo.spellColor == spellType) return ammo;
        }
        return spellCharges[0];
    }
}
