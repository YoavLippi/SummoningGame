using UnityEngine;

public class PotionIngredient : MonoBehaviour
{
	[Header("Ingredient Attributes")]
	[SerializeField] private int potencyValue = 0;      // Can be positive or negative
	[SerializeField] private int instabilityValue = 0;    // Can be positive or negative

	private void OnTriggerEnter(Collider other)
	{
		if (other.CompareTag("Cauldron"))
		{
			PotionCauldron cauldron = other.GetComponent<PotionCauldron>();
			if (cauldron != null)
			{
				// Send this specific item's values to the cauldron
				cauldron.MixIngredientServerRpc(potencyValue, instabilityValue);
			}

			// Destroy the ingredient asset after it falls in
			Destroy(gameObject);
		}
	}
}
