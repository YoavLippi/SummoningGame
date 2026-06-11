using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;

public class GameLogicController : NetworkBehaviour
{
    [SerializeField] private NetworkVariable<bool> isOtherDone = new NetworkVariable<bool>(false);

    public UnityEvent winEvent;

    [Rpc(SendTo.Server)]
    public void CheckFlagRpc()
    {
        if (!isOtherDone.Value)
        {
            isOtherDone.Value = true;
        }
        else
        {
            winEvent.Invoke();
        }
    }
}
