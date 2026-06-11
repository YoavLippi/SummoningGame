using UnityEngine;

public class PotionIngredient : MonoBehaviour
{
	[Header("Ingredient Attributes")]
	[SerializeField] private int potencyValue = 3;
	[SerializeField] private int instabilityValue = 0;

	// SAFETY GUARD: Prevents double-processing if physics triggers twice in one frame
	private bool hasBeenIngested = false;

	private void OnTriggerEnter(Collider other)
	{
		CheckIngest(other.gameObject);
	}

	private void OnCollisionEnter(Collision collision)
	{
		CheckIngest(collision.gameObject);
	}

	private void CheckIngest(GameObject otherObj)
	{
		// If it already processed, completely ignore any secondary impacts
		if (hasBeenIngested) return;

		if (otherObj.CompareTag("Cauldron"))
		{
			hasBeenIngested = true; // Lock it instantly!

			PotionCauldron cauldron = otherObj.GetComponent<PotionCauldron>();
			if (cauldron != null)
			{
				cauldron.MixIngredientServerRpc(potencyValue, instabilityValue);
			}

			Destroy(gameObject);
		}
	}
}
