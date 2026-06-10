using Unity.Netcode;
using UnityEngine;

public class NetworkingBroker : NetworkBehaviour
{
    public void SendMessageToAllPlayers(string message)
    {
        SendMessageGlobalRpc(message);
    }

    public void SendMessageToLocalPlayer(string message)
    {
        SendMessageLocal(message);
    }
    
    private void SendMessageLocal(string message)
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.SendMessage(message);
    }

    [Rpc(SendTo.Everyone)]
    private void SendMessageGlobalRpc(string message)
    {
        NetworkManager.Singleton.LocalClient.PlayerObject.SendMessage(message);
    }
}
