using UnityEngine;
using Unity.Netcode;
using System.Collections;

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
				
		if (assignedSpawner != null)
		{
			assignedSpawner.NotifyObjectLeftPlate();
		}

		if (NetworkManager.Singleton.IsServer)
		{
			var netObj = GetComponent<NetworkObject>();
			if (netObj != null && netObj.IsSpawned) netObj.Despawn(false);
			Destroy(gameObject);
		}
	}

	public void TriggerReturnGlide()
	{
		if (assignedSpawner != null)
		{
			// Run the smooth movement glide back to the spawner's exact transform coordinates
			StartCoroutine(ReturnToTableCoroutine(assignedSpawner.transform.position));
		}
	}

	private IEnumerator ReturnToTableCoroutine(Vector3 targetTablePosition)
	{
		float duration = 0.6f; // Adjust for float speed charm
		float elapsed = 0f;
		Vector3 startPosition = transform.position;
		Quaternion startRotation = transform.rotation;

		while (elapsed < duration)
		{
			elapsed += Time.deltaTime;
			float t = elapsed / duration;

			// Smooth easing curve out for a graceful floating touchdown
			float smoothT = Mathf.Sin(t * Mathf.PI * 0.5f);

			transform.position = Vector3.Lerp(startPosition, targetTablePosition, smoothT);
			transform.rotation = Quaternion.Lerp(startRotation, Quaternion.identity, smoothT);
			yield return null;
		}

		transform.position = targetTablePosition;
		transform.rotation = Quaternion.identity;

		// Re-enable the physics collider locally now that it's safe home on the plate
		Collider col = GetComponent<Collider>();
		if (col != null) col.enabled = true;
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
