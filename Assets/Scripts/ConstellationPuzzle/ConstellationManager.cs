using UnityEngine;
using Unity.Netcode;

public class ConstellationManager : NetworkBehaviour
{
	// A 1D array to represent our 4x4 grid (16 elements)
	// NetworkVariable ensures all players see the same "On/Off" states
	public NetworkList<bool> starStates;
	public StarButton[] allStars;

	// The correct solution (only checked on the server)
	private bool[] solution = {
				true, false, true, false,
				false, true, false, true,
				true, true, false, false,
				false, false, true, true
		};

	void Awake()
	{
		// Initialize the list with 16 'false' values
		for (int i = 0; i < 16; i++) starStates.Add(false);
	}

	// Called when the Apprentice clicks a star
	//[ServerRpc(RequireOwnership = false)]
	[Rpc(SendTo.Server,InvokePermission = RpcInvokePermission.Owner)] 
	public void ToggleStarServerRpc(int index)
	{
		starStates[index] = !starStates[index];
		CheckWinCondition();
	}

	private void CheckWinCondition()
	{
		for (int i = 0; i < 16; i++)
		{
			if (starStates[i] != solution[i]) return; // Not a match yet
		}
		Debug.Log("Puzzle Solved! Opening Magic Door...");
		// Trigger win event here (e.g., Change Scene or Open Door)
	}

	public override void OnNetworkSpawn()
	{
		starStates.OnListChanged += (changeEvent) => {
			// Update the visual for the specific star that changed
			allStars[changeEvent.Index].SetVisual(starStates[changeEvent.Index]);
		};
	}
}
