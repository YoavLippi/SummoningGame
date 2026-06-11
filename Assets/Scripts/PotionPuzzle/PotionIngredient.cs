using UnityEngine;
using Unity.Netcode;

public class PotionIngredient : NetworkBehaviour
{
	[Header("Ingredient Attributes")]
	[SerializeField] private int potencyValue = 3;
	[SerializeField] private int instabilityValue = 0;

	// SAFETY GUARD: Prevents double-processing if physics triggers twice in one frame
	private bool hasBeenIngested = false;

	//private void OnTriggerEnter(Collider other)
	//{
	//	CheckIngest(other.gameObject);
	//}

	//private void OnCollisionEnter(Collision collision)
	//{
	//	CheckIngest(collision.gameObject);
	//}

	public void CheckIngest(GameObject otherObj)
	{
		if (!IsServer) return;
		if (hasBeenIngested) return;

		hasBeenIngested = true;

		// Locate the cauldron in your scene layout
		PotionCauldron cauldron = Object.FindFirstObjectByType<PotionCauldron>();

		if (cauldron != null)
		{
			// Directly push the math values to the cauldron's ServerRpc
			cauldron.MixIngredientServerRpc(potencyValue, instabilityValue);
			Debug.Log($"[INGREDIENT] {gameObject.name} successfully ingested! Potency +{potencyValue}, Instability +{instabilityValue}");
		}
		else
		{
			Debug.LogError("[INGREDIENT] Could not find PotionCauldron in the active scene hierarchy!");
		}
				
		GetComponent<NetworkObject>().Despawn(true);
	}
}
