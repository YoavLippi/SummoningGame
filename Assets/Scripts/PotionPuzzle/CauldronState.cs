using UnityEngine;
using TMPro;
using System.Collections;

public class CauldronState : MonoBehaviour
{
	[Header("Live State UI Elements")]
	[SerializeField] private TextMeshProUGUI currentPotencyText;
	[SerializeField] private TextMeshProUGUI currentInstabilityText;

	[Header("Target Recipe UI Elements")]
	[SerializeField] private TextMeshProUGUI targetPotencyText;
	[SerializeField] private TextMeshProUGUI targetInstabilityText;

	private PotionCauldron cauldron;

	void Start()
	{
		StopAllCoroutines();
		// Start a coroutine to find the cauldron safely, 
		// just in case it takes a split second to spawn over the network.
		StartCoroutine(InitializeDisplay());
	}

	private IEnumerator InitializeDisplay()
	{
		while (cauldron == null)
		{
			cauldron = FindFirstObjectByType<PotionCauldron>();
			yield return new WaitForSeconds(0.1f);
		}

		// 1. Subscribe to the TARGET values as well! 
		// This ensures if the server takes a moment to generate them, the UI catches it instantly.
		cauldron.targetPotency.OnValueChanged += OnTargetPotencyChanged;
		cauldron.targetInstability.OnValueChanged += OnTargetInstabilityChanged;

		// Force a manual check right now just in case they are already loaded
		UpdateTargetText(cauldron.targetPotency.Value, cauldron.targetInstability.Value);

		// 2. Initialize and subscribe to the Live Mixing State values
		UpdateLiveText(cauldron.currentPotency.Value, cauldron.currentInstability.Value);
		cauldron.currentPotency.OnValueChanged += OnCurrentPotencyChanged;
		cauldron.currentInstability.OnValueChanged += OnCurrentInstabilityChanged;
	}

	void OnDestroy()
	{
		// Unsubscribe from everything to prevent memory leaks
		if (cauldron != null)
		{
			cauldron.targetPotency.OnValueChanged -= OnTargetPotencyChanged;
			cauldron.targetInstability.OnValueChanged -= OnTargetInstabilityChanged;
			cauldron.currentPotency.OnValueChanged -= OnCurrentPotencyChanged;
			cauldron.currentInstability.OnValueChanged -= OnCurrentInstabilityChanged;
		}
	}

	// --- TARGET RECIPE LISTENERS ---
	private void OnTargetPotencyChanged(int oldValue, int newValue)
	{
		UpdateTargetText(newValue, cauldron.targetInstability.Value);
	}

	private void OnTargetInstabilityChanged(int oldValue, int newValue)
	{
		UpdateTargetText(cauldron.targetPotency.Value, newValue);
	}

	private void UpdateTargetText(int targetPotency, int targetInstability)
	{
		if (targetPotencyText != null)
			targetPotencyText.text = $"Target Potency: {targetPotency}";

		if (targetInstabilityText != null)
			targetInstabilityText.text = $"Target Instability: {targetInstability}";
	}


	// --- LIVE STATE LISTENERS ---
	private void OnCurrentPotencyChanged(int oldValue, int newValue)
	{
		UpdateLiveText(newValue, cauldron.currentInstability.Value);
	}

	private void OnCurrentInstabilityChanged(int oldValue, int newValue)
	{
		UpdateLiveText(cauldron.currentPotency.Value, newValue);
	}

	private void UpdateLiveText(int currentPotency, int currentInstability)
	{
		if (currentPotencyText != null)
			currentPotencyText.text = $"Current Potency: {currentPotency}";

		if (currentInstabilityText != null)
		{
			currentInstabilityText.text = $"Current Instability: {currentInstability}";

			// Turn text red if they go over the target stability threshold
			currentInstabilityText.color = currentInstability > cauldron.targetInstability.Value ? Color.red : Color.darkGreen;
		}
	}
}
