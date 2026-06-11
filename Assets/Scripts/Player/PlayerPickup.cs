using UnityEngine;
using Unity.Netcode;
using UnityEngine.InputSystem;
using Unity.Cinemachine;

public class PlayerPickup : NetworkBehaviour
{
	// THIS SCRIPT MUST ONLY GO ON THE TOWER PLAYER
	//------------------------------------------------------------------------------------------------------------

	[Header("Input References")]
	[SerializeField] private PlayerInput playerInput; // Drag PlayerInput component here
	[SerializeField] private string actionMapName = "PlayerController";
	[SerializeField] private string pickupActionName = "PickUp";

	[Header("Pickup Settings")]
	//[SerializeField] private Camera playerCamera;
	[SerializeField] private Transform holdPosition;
	[SerializeField] private LayerMask interactableLayer;
	[SerializeField] private float pickupRange = 3f;
	[SerializeField] private float sphereRadius = 0.3f;

	[Header("Smart Drop Settings")]
	[SerializeField] private float cauldronTargetingRadius = 1.5f; // How forgiving the drop aim is
	//[SerializeField] private float throwForce = 5f;

	private PotionCauldron cauldronInstance;

	private InputAction pickupAction;
	private GameObject carriedItem;
	private Rigidbody carriedRb;
	private Transform activeCamTransform;

	private void Start()
	{
		StopAllCoroutines();
		cauldronInstance = FindFirstObjectByType<PotionCauldron>();
	}
	public override void OnNetworkSpawn()
	{
		// Only the local player who owns this character should process inputs
		if (!IsOwner) return;

		FindActiveCinemachineCamera();

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
			pickupAction.performed += OnPickupPressed;
			pickupAction.canceled += OnPickupReleased;
		}
	}

	private void FindActiveCinemachineCamera()
	{
		// In Unity 6 / Cinemachine 3, the brain keeps track of the active camera
		var brain = Camera.main.GetComponent<CinemachineBrain>();
		if (brain != null && brain.ActiveVirtualCamera != null)
		{
			Component camComponent = brain.ActiveVirtualCamera as Component;
			if (camComponent != null)
			{
				activeCamTransform = camComponent.transform;
			}
			else
			{
				activeCamTransform = Camera.main.transform;
			}
		}
		else
		{
			// Fallback to Main Camera transform if the brain isn't fully active yet
			activeCamTransform = Camera.main.transform;
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
		if (cauldronInstance != null && cauldronInstance.IsPuzzleComplete)
		{
			// If the player is currently holding an item when victory triggers, force drop it safely
			if (carriedRb != null)
			{
				carriedRb.useGravity = true;
				carriedItem = null;
				carriedRb = null;
			}
			return; // Exit out early, ignoring interaction keys completely!
		}

		if (!IsOwner) return;

		// Smoothly move the item to the hold position every frame while holding it
		if (carriedItem != null)
		{
			MoveCarriedItem();
		}
	}

	private void OnPickupPressed(InputAction.CallbackContext context)
	{
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
		FindActiveCinemachineCamera();

		Ray ray = new Ray(activeCamTransform.position, activeCamTransform.forward); 
		RaycastHit hit;

		// We replace Physics.Raycast with Physics.SphereCast
		if (Physics.SphereCast(ray, sphereRadius, out hit, pickupRange, interactableLayer))
		{
			PotionIngredient ingredient = hit.collider.GetComponent<PotionIngredient>();
			if (ingredient != null)
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
			FindActiveCinemachineCamera();

			// Shoot a thick volumetric cylinder forward from the eyes
			Ray ray = new Ray(activeCamTransform.position, activeCamTransform.forward);
			RaycastHit hit;

			// Multiply pickupRange by 2 or 3 so players can "toss" items from across the room!
			if (Physics.SphereCast(ray, sphereRadius, out hit, pickupRange * 2.5f))
			{
				// If our aim line passes through the large invisible child trigger bubble...
				if (hit.collider.CompareTag("CauldronMagnet"))
				{
					// Find the center of the ACTUAL cauldron (the parent) so it glides to the middle
					Vector3 cauldronTrueCenter = hit.collider.transform.parent != null ?
							hit.collider.transform.parent.gameObject.GetComponent<Collider>().bounds.center :
							hit.collider.bounds.center;

					// Adjust target destination slightly above the surface liquid
					cauldronTrueCenter.y += 0.2f;

					// Fire the smooth tractor-beam glide!
					StartCoroutine(GlideIntoCauldron(carriedItem, carriedRb, cauldronTrueCenter));

					carriedItem = null;
					carriedRb = null;
					return;
				}
			}

			// Standard Fallback Drop: If looking completely away, fall flat to the floor
			carriedRb.useGravity = true;
			carriedRb.linearVelocity = Vector3.zero;
		}

		carriedItem = null;
		carriedRb = null;
	}

	private System.Collections.IEnumerator GlideIntoCauldron(GameObject item, Rigidbody rb, Vector3 targetPos)
	{
		// Shuts off gravity and physics so it doesn't drop while moving toward the pot
		rb.useGravity = false;
		rb.linearVelocity = Vector3.zero;
		rb.angularVelocity = Vector3.zero;

		// Disable its colliders temporarily so it smoothly passes through the cauldron rim without bouncing off the edges
		Collider itemCollider = item.GetComponent<Collider>();
		if (itemCollider != null) itemCollider.isTrigger = true;

		float travelTime = 0f;
		Vector3 startPos = item.transform.position;

		// Over the course of 0.4 seconds, slide the item cleanly to the target center
		while (travelTime < 0.4f && item != null)
		{
			travelTime += Time.deltaTime;
			float progress = travelTime / 0.4f;

			// Smooth step makes it start slow, speed up, then slow down as it lands
			item.transform.position = Vector3.Lerp(startPos, targetPos, Mathf.SmoothStep(0f, 1f, progress));

			yield return null;
		}
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
		if (activeCamTransform == null && Camera.main != null)
			activeCamTransform = Camera.main.transform;

		if (activeCamTransform == null) return;

		Gizmos.color = Color.deepPink;
		// Draw the new Drop Detection Bubble zone in your scene window
		Vector3 detectionCenter = activeCamTransform.position + (activeCamTransform.forward * (pickupRange * 0.5f));
		Gizmos.DrawWireSphere(detectionCenter, cauldronTargetingRadius);
	}
}
