using Unity.Netcode;
using UnityEngine;

public class NetworkSpell : NetworkBehaviour
{
    public enum SpellType { Red, Blue, Yellow, Green, Cyan}

    public NetworkVariable<SpellType> CurrentSpellType = new NetworkVariable<SpellType>(
        SpellType.Red,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
        );
}
