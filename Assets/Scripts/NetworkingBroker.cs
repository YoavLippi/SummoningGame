using Unity.Netcode;
using UnityEngine;
using UnityEngine.SceneManagement;

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

	[Rpc(SendTo.Server)]
	public void LoadSceneRpc(string sceneName)
	{
		if (!IsHost) return;
		NetworkManager.Singleton.SceneManager.LoadScene(sceneName, LoadSceneMode.Single);
	}
}
