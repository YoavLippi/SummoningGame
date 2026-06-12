using UnityEngine;
using Unity.Netcode;
using System.Collections;

public class PlayerPickup : NetworkBehaviour
{
	[Header("Placement Configuration")]
	[SerializeField] private float dropForwardOffset = 1.2f;
	[SerializeField] private Transform handLocation;

	public GameObject CarriedItem => carriedItem;

	private GameObject carriedItem;
	private NetworkObject carriedNetObj;
	private Collider carriedCollider;

	public void InsidePressPickup(RaycastHit hitInfo, bool hitSomething)
	{
		//  SAFETY GUARD: Only the local controller owner can trigger a pickup event!
		PotionCauldron cauldron = Object.FindFirstObjectByType<PotionCauldron>();
		if (cauldron != null && cauldron.IsPuzzleComplete)
		{
			// If the potion is successfully brewed, freeze all pickup interactions!
			return;
		}

		if (!IsOwner || carriedItem != null) return;

		if (hitSomething && hitInfo.collider.CompareTag("Ingredient"))
		{
			NetworkObject hitNetObj = hitInfo.collider.GetComponent<NetworkObject>();
			if (hitNetObj != null)
			{
				carriedItem = hitNetObj.gameObject;
				carriedNetObj = hitNetObj;
				carriedCollider = carriedItem.GetComponent<Collider>();

				if (carriedCollider != null) carriedCollider.enabled = false;

				Debug.Log($"[PICKUP TRACE] Successfully grabbed {carriedItem.name}. Initial World Position: {carriedItem.transform.position}");

				RequestPickupServerRpc(hitNetObj.NetworkObjectId);
			}
		}
	}

	public void InsideReleaseDrop(RaycastHit crosshairHit, bool hitSomething)
	{
		if (!IsOwner || carriedItem == null) return;

		// CHOICE A: Magnetize Glide straight into the Cauldron
		if (hitSomething && crosshairHit.collider.CompareTag("CauldronMagnet"))
		{
			Vector3 targetCenter = crosshairHit.collider.bounds.center;
			targetCenter.y += 0.2f;

			Debug.Log("[DROP TRACE] Target detected as CAULDRON! Commencing glide calculation sequence.");
			RequestMagnetizeServerRpc(carriedNetObj.NetworkObjectId, targetCenter);

			ClearLocalReferences();
			return;
		}

		// CHOICE B: MAGICAL MISDROP RETURN
		// If it wasn't dropped in the cauldron, float it back to its specific table plate!
		Debug.Log("[DROP TRACE] Misdrop registered! Commanding item to float home.");

		var ingredientScript = carriedItem.GetComponent<PotionIngredient>();
		if (ingredientScript != null)
		{
			// Tell the item to slide back gracefully to its table spawner location
			ingredientScript.TriggerReturnGlide();
		}
		else
		{
			// Fallback emergency safety if script component goes missing
			Collider col = carriedItem.GetComponent<Collider>();
			if (col != null) col.enabled = true;
		}

		//  RULES CHECK: Notice we DO NOT talk to the spawner here! 
		// Because the item went back to the plate, the table remains occupied, 
		// preventing duplicate item spawning loops entirely.

		ClearLocalReferences();
	}

	private void LateUpdate()
	{
		// CRUCIAL MULTIPLAYER NETCODE FIX: 
		// If I do not own this player controller body instance, do NOT snap items to it!
		// This stops the remote ghost player from hijacking your held ingredient transformations.
		if (!IsOwner) return;

		if (carriedItem != null && handLocation != null)
		{
			carriedItem.transform.position = handLocation.position;
			carriedItem.transform.rotation = handLocation.rotation;
			carriedItem.transform.localScale = Vector3.one;
		}
	}

	[ServerRpc(RequireOwnership = false)]
	private void RequestPickupServerRpc(ulong networkObjectId, ServerRpcParams rpcParams = default)
	{
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
		{
			netObj.gameObject.transform.SetParent(null, true);
			netObj.ChangeOwnership(rpcParams.Receive.SenderClientId);
			NotifyPickupClientRpc(networkObjectId);
		}
	}

	[ClientRpc]
	private void NotifyPickupClientRpc(ulong networkObjectId)
	{
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
		{
			netObj.gameObject.transform.SetParent(null, true);
			Collider col = netObj.gameObject.GetComponent<Collider>();
			if (col != null) col.enabled = false;
		}
	}

	[ServerRpc]
	private void RequestDropServerRpc(ulong networkObjectId, Vector3 finalDropPosition)
	{
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
		{
			netObj.ChangeOwnership(0);
			PlaceOnSurfaceClientRpc(networkObjectId, finalDropPosition);
		}
	}

	[ClientRpc]
	private void PlaceOnSurfaceClientRpc(ulong networkObjectId, Vector3 finalDropPosition)
	{
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
		{
			GameObject item = netObj.gameObject;
			item.transform.SetParent(null, true);
			item.transform.position = finalDropPosition;
			item.transform.rotation = Quaternion.identity;

			Collider col = item.GetComponent<Collider>();
			if (col != null) col.enabled = true;
		}
	}

	[ServerRpc]
	private void RequestMagnetizeServerRpc(ulong networkObjectId, Vector3 cauldronCenter)
	{
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
		{
			netObj.ChangeOwnership(0);
			TriggerGlideClientRpc(networkObjectId, cauldronCenter);
		}
	}

	[ClientRpc]
	private void TriggerGlideClientRpc(ulong networkObjectId, Vector3 cauldronCenter)
	{
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
		{
			StartCoroutine(ScriptedGlideCoroutine(netObj.gameObject, cauldronCenter));
		}
	}

	private IEnumerator ScriptedGlideCoroutine(GameObject item, Vector3 destination)
	{
		float duration = 0.5f;
		float elapsed = 0f;
		Vector3 startPosition = item.transform.position;

		while (elapsed < duration)
		{
			if (item == null) yield break;
			elapsed += Time.deltaTime;
			item.transform.position = Vector3.Lerp(startPosition, destination, elapsed / duration);
			yield return null;
		}

		if (item != null)
		{
			item.transform.position = destination;
			if (NetworkManager.Singleton.IsServer)
			{
				var ingredientScript = item.GetComponent<PotionIngredient>();
				if (ingredientScript != null)
				{
					ingredientScript.CheckIngest(item);
				}
				else
				{
					// Fallback security cleanup if the object lacks an ingredient component
					var netObj = item.GetComponent<NetworkObject>();
					if (netObj != null && netObj.IsSpawned) netObj.Despawn(true);
					else Destroy(item);
				}
			}
		}
	}

	private void ClearLocalReferences()
	{
		carriedItem = null;
		carriedNetObj = null;
		carriedCollider = null;
	}
}