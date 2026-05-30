using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using System.Globalization;

public class PlayerPickup : NetworkBehaviour
{
	// THIS SCRIPT MUST ONLY GO ON THE TOWER PLAYER
	//------------------------------------------------------------------------------------------------------------

	[Header("Input References")]
	[SerializeField] private PlayerInput playerInput; // Drag PlayerInput component here
	[SerializeField] private string actionMapName = "PlayerController";
	[SerializeField] private string pickupActionName = "PickUp";

	[Header("Pickup Settings")]
	[SerializeField] private Transform holdPosition;
	[SerializeField] private LayerMask interactableLayer;
	[SerializeField] private float pickupRange = 3f;

	private InputAction pickupAction;
	private GameObject carriedItem;
	private Rigidbody carriedRb;

	public override void OnNetworkSpawn()
	{
		// Only the local player who owns this character should process inputs
		if (!IsOwner) return;

		Debug.Log($"[Pickup Debug] Spawning Local Player. Looking for Map: {actionMapName}, Action: {pickupActionName}");

		// Find the specific action from the PlayerInput component
		if (playerInput != null)
		{
			var actionMap = playerInput.actions.FindActionMap(actionMapName);
			if (actionMap != null)
			{
				pickupAction = actionMap.FindAction(pickupActionName);
			}
		}

		// Subscribe to the input events
		if (pickupAction != null)
		{
			Debug.Log("[Pickup Debug] Action successfully found and bound!");
			pickupAction.performed += OnPickupPressed;
			pickupAction.canceled += OnPickupReleased;
		}
	}

	public override void OnNetworkDespawn()
	{
		// Unsubscribe from events when despawning to prevent memory leaks
		if (pickupAction != null)
		{
			pickupAction.performed -= OnPickupPressed;
			pickupAction.canceled -= OnPickupReleased;
		}
	}

	private void Update()
	{
		if (!IsOwner) return;

		// Smoothly move the item to the hold position every frame while holding it
		if (carriedItem != null)
		{
			MoveCarriedItem();
		}
	}

	private void OnPickupPressed(InputAction.CallbackContext context)
	{
		Debug.Log("[Pickup Debug] Input Detected! Right Click Performed. Trying to raycast...");
		if (carriedItem == null)
		{
			TryPickUpItem();
		}
	}

	private void OnPickupReleased(InputAction.CallbackContext context)
	{
		if (carriedItem != null)
		{
			DropItem();
		}
	}

	private void TryPickUpItem()
	{
		Ray ray = new Ray(transform.position, transform.forward);
		RaycastHit hit;

		if (Physics.Raycast(ray, out hit, pickupRange, interactableLayer))
		{
			if (hit.collider.GetComponent<PotionIngredient>() != null)
			{
				carriedItem = hit.collider.gameObject;
				carriedRb = carriedItem.GetComponent<Rigidbody>();

				if (carriedRb != null)
				{
					carriedRb.useGravity = false;
					carriedRb.linearVelocity = Vector3.zero;
					carriedRb.angularVelocity = Vector3.zero;
				}

				var netObj = carriedItem.GetComponent<NetworkObject>();
				if (netObj != null)
				{
					RequestPickupServerRpc(netObj.NetworkObjectId);
				}
			}
		}
	}

	private void MoveCarriedItem()
	{
		carriedItem.transform.position = Vector3.Lerp(carriedItem.transform.position, holdPosition.position, Time.deltaTime * 15f);
		carriedItem.transform.rotation = Quaternion.Lerp(carriedItem.transform.rotation, holdPosition.rotation, Time.deltaTime * 15f);
	}

	private void DropItem()
	{
		if (carriedRb != null)
		{
			carriedRb.useGravity = true;
			carriedRb.AddForce(transform.forward * 2f, ForceMode.Impulse);
		}

		carriedItem = null;
		carriedRb = null;
	}

	[ServerRpc]
	private void RequestPickupServerRpc(ulong networkObjectId)
	{
		if (NetworkManager.Singleton.SpawnManager.SpawnedObjects.TryGetValue(networkObjectId, out NetworkObject netObj))
		{
			netObj.ChangeOwnership(OwnerClientId);
		}
	}

	private void OnDrawGizmos()
	{
		// Draws a blue line in the Scene window showing your pickup reach
		Gizmos.color = Color.deepPink;
		Gizmos.DrawRay(transform.position, transform.forward * pickupRange);
	}
}
