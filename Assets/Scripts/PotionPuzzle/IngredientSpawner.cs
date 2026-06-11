using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class IngredientSpawner : NetworkBehaviour
{
	[Header("Spawn Settings")]
	[SerializeField] private GameObject ingredientPrefab;
	[SerializeField] private float respawnDelay = 1.5f;
	[SerializeField] private float vacancyRadius = 0.5f;   // Distance the item must move before a new one spawns

	private GameObject currentSpawnedItem;
	private bool isWaitingToRespawn = false;

	public void Start()
	{
		StopAllCoroutines();
	}

	public override void OnNetworkSpawn()
	{
		if (!IsServer) return;
		SpawnNewIngredient();
	}

	private void Update()
	{
		if (!IsServer) return;
		if (isWaitingToRespawn) return;

		//  THE NEW SECURITY CHECK 
		// If the item was destroyed (cauldron ingestion) OR it was picked up and carried away...
		if (currentSpawnedItem == null || Vector3.Distance(transform.position, currentSpawnedItem.transform.position) > vacancyRadius)
		{
			// Forget about the item we just let go of so we don't track it while the player walks around
			currentSpawnedItem = null;

			// Fire the timed replacement cycle!
			StartCoroutine(RespawnSequence());
		}
	}

	private void SpawnNewIngredient()
	{
		// FIXED INSTANTIATION LINK: Notice we do NOT pass 'transform' as a parent argument here.
		// This forces the item to spawn parentless at the root level of the scene, completely
		// eliminating nested scale/position bugs when players grab it off the table!
		currentSpawnedItem = Instantiate(ingredientPrefab, transform.position, transform.rotation);

		var netObj = currentSpawnedItem.GetComponent<NetworkObject>();
		if (netObj != null)
		{
			netObj.Spawn(true);
		}
	}

	private IEnumerator RespawnSequence()
	{
		isWaitingToRespawn = true;

		yield return new WaitForSeconds(respawnDelay);

		SpawnNewIngredient();

		isWaitingToRespawn = false;
	}
}