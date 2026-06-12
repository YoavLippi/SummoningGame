using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class ClientTransformSync : NetworkBehaviour
{
	private PlayerPickup localCarryingPlayer;
	private bool isBeingCarriedLocally = false;

	private void Start()
	{
		// Cache our local player's pickup script safely when the game starts
		// This makes sure we only listen to the person sitting at this keyboard!
		NetworkManager.Singleton.OnClientConnectedCallback += (id) => {
			FindLocalPlayerPickup();
		};
		FindLocalPlayerPickup();
	}

	private void FindLocalPlayerPickup()
	{
		foreach (var player in Object.FindObjectsByType<PlayerPickup>(FindObjectsSortMode.None))
		{
			if (player.IsOwner)
			{
				localCarryingPlayer = player;
				break;
			}
		}
	}

	private void LateUpdate()
	{
		//  CHECK CARRIED STATE: If our local pickup handler script is holding 
		// THIS specific ingredient instance, force it to sync coordinates immediately!
		if (localCarryingPlayer != null && localCarryingPlayer.CarriedItem == gameObject)
		{
			isBeingCarriedLocally = true;
			UpdateTransformServerRpc(transform.position, transform.rotation);
			return;
		}

		// If we were just dropped, run one final network broadcast sync frame
		if (isBeingCarriedLocally)
		{
			isBeingCarriedLocally = false;
			UpdateTransformServerRpc(transform.position, transform.rotation);
		}
	}

	[ServerRpc(RequireOwnership = false)] 
	private void UpdateTransformServerRpc(Vector3 newPos, Quaternion newRot)
	{
		transform.position = newPos;
		transform.rotation = newRot;

		// Broadcast coordinates to everyone else's screen instantly
		UpdateTransformClientRpc(newPos, newRot);
	}

	[ClientRpc]
	private void UpdateTransformClientRpc(Vector3 newPos, Quaternion newRot)
	{
		// Other remote players update their rendering layers to match your movements
		if (localCarryingPlayer == null || localCarryingPlayer.CarriedItem != gameObject)
		{
			transform.position = newPos;
			transform.rotation = newRot;
		}
	}
}