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
		//  SAFETY GUARD: Block remote proxies from processing your release vectors!
		if (!IsOwner || carriedItem == null) return;

		Debug.Log($"[DROP TRACE] Attempting to release {carriedItem.name}. Current Position before release: {carriedItem.transform.position}");

		if (hitSomething && crosshairHit.collider.CompareTag("CauldronMagnet"))
		{
			Vector3 targetCenter = crosshairHit.collider.bounds.center;
			targetCenter.y += 0.2f;

			Debug.Log("[DROP TRACE] Target detected as CAULDRON! Commencing glide calculation sequence.");
			RequestMagnetizeServerRpc(carriedNetObj.NetworkObjectId, targetCenter);

			ClearLocalReferences();
			return;
		}

		Vector3 startingOrigin = handLocation != null ? handLocation.position : transform.position;
		Vector3 dropPosition = startingOrigin + (transform.forward * dropForwardOffset);

		if (Physics.Raycast(dropPosition, Vector3.down, out RaycastHit surfaceHit, 4f))
		{
			dropPosition = surfaceHit.point;
			Debug.Log($"[DROP TRACE] Safely hit surface layer below player eyes at: {dropPosition}");
		}
		else
		{
			// Fallback placement logic maps straight down to desk level height
			dropPosition = startingOrigin + (transform.forward * dropForwardOffset) - new Vector3(0, 0.5f, 0);
			Debug.Log($"[DROP TRACE] No structural surface found below. Defaulting to air drop layout at: {dropPosition}");
		}

		RequestDropServerRpc(carriedNetObj.NetworkObjectId, dropPosition);
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
				if (ingredientScript != null) ingredientScript.CheckIngest(item);
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