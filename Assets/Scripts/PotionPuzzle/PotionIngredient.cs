using UnityEngine;
using Unity.Netcode;

public class PotionIngredient : NetworkBehaviour
{
	[Header("Ingredient Attributes")]
	[SerializeField] private int potencyValue = 3;
	[SerializeField] private int instabilityValue = 0;

	private bool hasBeenIngested = false;

	// Tracks which specific table spawner node created this instance
	private IngredientSpawner assignedSpawner;

	public void SetAssignedSpawner(IngredientSpawner spawner)
	{
		assignedSpawner = spawner;
	}

	public void CheckIngest(GameObject otherObj)
	{
		if (!IsServer) return;
		if (hasBeenIngested) return;

		hasBeenIngested = true;

		PotionCauldron cauldron = Object.FindFirstObjectByType<PotionCauldron>();

		if (cauldron != null)
		{
			cauldron.MixIngredientServerRpc(potencyValue, instabilityValue);
			Debug.Log($"[INGREDIENT] {gameObject.name} successfully ingested! Potency +{potencyValue}, Instability +{instabilityValue}");
		}
		else
		{
			Debug.LogError("[INGREDIENT] Could not find PotionCauldron in the active scene hierarchy!");
		}

		
		if (assignedSpawner != null)
		{
			assignedSpawner.NotifyObjectLeftPlate();
		}

		if (NetworkManager.Singleton.IsServer)
		{
			var netObj = GetComponent<NetworkObject>();
			if (netObj != null && netObj.IsSpawned)
			{
				netObj.Despawn(false);
			}
			Destroy(gameObject);
		}
	}
	public void NotifySpawnerOfPickup()
	{
		if (assignedSpawner != null)
		{
			assignedSpawner.NotifyObjectLeftPlate();
			assignedSpawner = null; // Disconnect tracking link so it doesn't trigger twice
		}
	}
}
