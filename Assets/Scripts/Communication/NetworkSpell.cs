using Unity.Netcode;
using UnityEngine;

public class NetworkSpell : NetworkBehaviour
{
    //public enum SpellType { Red, Blue, Yellow, Green, Cyan}

    public NetworkVariable<InteractionHandler.Color> CurrentSpellType = new NetworkVariable<InteractionHandler.Color>(
        InteractionHandler.Color.Red,
        NetworkVariableReadPermission.Everyone,
        NetworkVariableWritePermission.Owner
        );
}
