using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class IngredientSpawner : NetworkBehaviour
{
	[Header("Spawn Settings")]
	[SerializeField] private GameObject ingredientPrefab;
	[SerializeField] private float respawnDelay = 1.5f;

	private GameObject currentSpawnedItem;
	private bool isWaitingToRespawn = false;

	public GameObject IngredientPrefab => ingredientPrefab;

	public void Start()
	{
		StopAllCoroutines();
	}

	public override void OnNetworkSpawn()
	{
		if (!IsServer) return;
		SpawnNewIngredient();
	}

	public void SpawnNewIngredient()
	{
		if (!IsServer) return;

		if (ingredientPrefab == null)
		{
			return;
		}

		currentSpawnedItem = Instantiate(ingredientPrefab, transform.position, transform.rotation);
		var netObj = currentSpawnedItem.GetComponent<NetworkObject>();
		if (netObj != null)
		{
			netObj.Spawn(true);			
		}		

		var ingredientScript = currentSpawnedItem.GetComponent<PotionIngredient>();
		if (ingredientScript != null)
		{
			ingredientScript.SetAssignedSpawner(this);
			
		}		
	}

	public void NotifyObjectLeftPlate()
	{	

		if (!IsServer) return;

		if (isWaitingToRespawn)
		{			
			return;
		}

		StartCoroutine(RespawnSequence());
	}

	private IEnumerator RespawnSequence()
	{
		isWaitingToRespawn = true;
		
		yield return new WaitForSeconds(respawnDelay);

		SpawnNewIngredient();

		isWaitingToRespawn = false;		
	}
}