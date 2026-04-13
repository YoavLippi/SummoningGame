using UnityEngine;
using Unity.Netcode;

public class PotionManager : NetworkBehaviour
{
	// The "True" recipe known only by the Server/Librarian (one with book) logic
	private int[] winningSequence = { 3, 1, 5 };
	private int currentProgress = 0;

	[Rpc(SendTo.Server)]
	public void TryIngredientServerRpc(int id)
	{
		if (id == winningSequence[currentProgress])
		{
			currentProgress++;
			Debug.Log("Librarian: 'Correct! Next step...'");

			if (currentProgress >= winningSequence.Length)
			{
				NotifyWinClientRpc(); // Tell everyone they won!
			}
		}
		else
		{
			currentProgress = 0; // Reset on mistake
			Debug.Log("Librarian: 'BOOM! Start over!'");
		}
	}

	[ClientRpc]
	void NotifyWinClientRpc()
	{
		// Trigger fireworks or open a door 
	}
}
