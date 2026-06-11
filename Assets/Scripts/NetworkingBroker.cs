using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

public class NetworkingBroker : NetworkBehaviour
{
    public static NetworkingBroker Instance;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(this);
        }
        else
        {
            Instance = this;
        }
    }

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

    [Rpc(SendTo.Everyone)]
    public void ChangeSceneForAllRpc(string sceneName)
    {
        NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
    }
}
