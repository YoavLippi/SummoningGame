using UnityEngine;

public class PotionIngredient : MonoBehaviour
{
	[Header("Ingredient Attributes")]
	[SerializeField] private int potencyValue = 0;
	[SerializeField] private int instabilityValue = 0;

	// Handles when it glides inside as a trigger
	private void OnTriggerEnter(Collider other)
	{
		CheckIngest(other.gameObject);
	}

	// Backup: Handles normal physics drops if it falls in naturally
	private void OnCollisionEnter(Collision collision)
	{
		CheckIngest(collision.gameObject);
	}

	private void CheckIngest(GameObject otherObj)
	{
		if (otherObj.CompareTag("Cauldron"))
		{
			PotionCauldron cauldron = otherObj.GetComponent<PotionCauldron>();
			if (cauldron != null)
			{
				cauldron.MixIngredientServerRpc(potencyValue, instabilityValue);
			}

			// Vanish into the brew
			Destroy(gameObject);
		}
	}
}
