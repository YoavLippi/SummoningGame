using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class IngredientSpawner : NetworkBehaviour
{
	[Header("Spawn Settings")]
	[SerializeField] private GameObject ingredientPrefab; // Drop your Bone/Mushroom prefab here
	[SerializeField] private float respawnDelay = 1.0f;    // Time to wait before a new one appears

	private GameObject currentSpawnedItem;
	private bool isWaitingToRespawn = false;

	public void Start()
	{
		StopAllCoroutines();
	}

	public override void OnNetworkSpawn()
	{
		// Only the server should handle spawning physical network objects
		if (!IsServer) return;

		SpawnNewIngredient();
	}

	private void Update()
	{
		if (!IsServer) return;
		if (isWaitingToRespawn) return;

		// If the item we spawned is gone (either picked up, destroyed, or fell in the cauldron)
		if (currentSpawnedItem == null)
		{
			StartCoroutine(RespawnSequence());
		}
	}

	private void SpawnNewIngredient()
	{
		// Create the object at the spawner's exact position and rotation
		currentSpawnedItem = Instantiate(ingredientPrefab, transform.position, transform.rotation);

		// If your ingredients are NetworkObjects, spawn them across the network:
		var netObj = currentSpawnedItem.GetComponent<NetworkObject>();
		if (netObj != null)
		{
			netObj.Spawn(true);
		}
	}

	private IEnumerator RespawnSequence()
	{
		isWaitingToRespawn = true;

		// Wait for the delay (gives a nice visual beat so it doesn't just instantly snap back)
		yield return new WaitForSeconds(respawnDelay);

		SpawnNewIngredient();

		isWaitingToRespawn = false;
	}
}
