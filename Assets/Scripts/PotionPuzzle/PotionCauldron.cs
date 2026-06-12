using UnityEngine;
using Unity.Netcode;
using UnityEngine.Events;

public class PotionCauldron : NetworkBehaviour
{
	[Header("Current Mixture State")]
	public NetworkVariable<int> currentPotency = new NetworkVariable<int>(0);
	public NetworkVariable<int> currentInstability = new NetworkVariable<int>(0);

	[Header("Dynamic Targets (Server Picked)")]
	public NetworkVariable<int> targetPotency = new NetworkVariable<int>(0);
	public NetworkVariable<int> targetInstability = new NetworkVariable<int>(0);

	public bool IsPuzzleComplete { get; private set; } = false;

	[Header("Designer Color Configuration")]
	[SerializeField] public Color stableColor = Color.cyan;
	[SerializeField] public Color instableColor = new Color(1f, 0.5f, 0f); // Orange
	[SerializeField] public Color completelyInstableColor = Color.red;
	[SerializeField] public Color recipeCompleteColor = Color.green;

	[Header("Instability Thresholds")]
	[SerializeField] private int highInstabilityThreshold = 3;

	[Header("Cutscene Setup")]
	[SerializeField] private GameObject cutsceneDirectorObject;

	[Header("Visual Component References")]
	[SerializeField] private ParticleSystem smokeParticles;
	[SerializeField] private ParticleSystem bubbleParticles;
	[SerializeField] private Renderer liquidSurfaceRenderer;
	[SerializeField] private Light cauldronAmbientLight;

	[Header("Events")][SerializeField] private UnityEvent winEvent;

	private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
	private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
	private static readonly int ColorProperty = Shader.PropertyToID("_Color");

	public override void OnNetworkSpawn()
	{
		// Only the Server is allowed to roll the dice on the targets
		if (IsServer)
		{
			targetPotency.Value = Random.Range(8, 19);
			targetInstability.Value = Random.Range(0, 2);

			Debug.Log($"[SERVER] New Potion Recipe Generated! Target Potency: {targetPotency.Value}, Target Instability: {targetInstability.Value}");
		}

		// THE FIX: Subscribe both the host and clients to value change listeners!
		// The exact frame the values sync across the network, the visuals will update locally,
		// completely bypassing the need for a separate ClientRpc call!
		currentPotency.OnValueChanged += OnCauldronValuesChanged;
		currentInstability.OnValueChanged += OnCauldronValuesChanged;

		// Initialize visual elements to their baseline starting state
		UpdateCauldronVisuals(currentPotency.Value, currentInstability.Value);
	}

	public override void OnNetworkDespawn()
	{
		// Clean up our listeners when leaving the game to prevent memory leaks
		currentPotency.OnValueChanged -= OnCauldronValuesChanged;
		currentInstability.OnValueChanged -= OnCauldronValuesChanged;
	}

	private void OnCauldronValuesChanged(int oldValue, int newValue)
	{
		// Whenever the network variables change, fire the visual layout loop safely!
		UpdateCauldronVisuals(currentPotency.Value, currentInstability.Value);
	}

	[ServerRpc(RequireOwnership = false)]
	public void MixIngredientServerRpc(int potencyModifier, int instabilityModifier)
	{
		if (IsPuzzleComplete) return;

		currentPotency.Value += potencyModifier;
		currentInstability.Value += instabilityModifier;

		if (currentPotency.Value < 0) currentPotency.Value = 0;
		if (currentInstability.Value < 0) currentInstability.Value = 0;

		// REMOVED: UpdateCauldronVisualsClientRpc call is gone from here.
		// Simply changing the NetworkVariable values above automatically triggers our new listeners!

		CheckPotionSolution();
	}

	private void CheckPotionSolution()
	{
		if (!IsServer) return;

		if (currentPotency.Value == targetPotency.Value && currentInstability.Value == targetInstability.Value)
		{
			Debug.Log("Dynamic potion recipe matched perfectly! Playing cutscene...");
			IsPuzzleComplete = true;
			LockPuzzleStateClientRpc();
			winEvent.Invoke();
		}
	}

	[ClientRpc]
	private void LockPuzzleStateClientRpc()
	{
		IsPuzzleComplete = true;
	}

	// REMOVED [ClientRpc] attribute. This is now a regular local calculation method!
	private void UpdateCauldronVisuals(int potency, int instability)
	{
		Color stateColor = stableColor;

		if (potency == targetPotency.Value && instability == targetInstability.Value)
		{
			stateColor = recipeCompleteColor;
		}
		else if (instability >= highInstabilityThreshold)
		{
			stateColor = completelyInstableColor;
		}
		else if (instability > 0)
		{
			stateColor = instableColor;
		}

		// 1. HANDLE PARTICLES
		if (smokeParticles != null && bubbleParticles != null)
		{
			var smokeEmission = smokeParticles.emission;
			var smokeMain = smokeParticles.main;
			var bubbleEmission = bubbleParticles.emission;

			if (instability > 0)
			{
				smokeEmission.rateOverTime = 30f + (instability * 35f);
				smokeMain.startSpeed = 1.5f + (instability * 0.8f);
				smokeMain.startColor = stateColor;
				bubbleEmission.rateOverTime = 10f + (instability * 40f);
			}
			else
			{
				smokeEmission.rateOverTime = 10f;
				smokeMain.startSpeed = 0.6f;
				smokeMain.startColor = stableColor;
				bubbleEmission.rateOverTime = 10f;
			}
		}

		// 2. HANDLE POTENCY LIQUID GLOW
		if (liquidSurfaceRenderer != null)
		{
			Material liquidMat = liquidSurfaceRenderer.material;
			if (liquidMat != null)
			{
				float constantGlow = 1.25f;
				if (stateColor == recipeCompleteColor) constantGlow = 3.5f;

				Color finalEmission = stateColor * constantGlow;

				if (liquidMat.HasProperty(BaseColorProperty)) liquidMat.SetColor(BaseColorProperty, stateColor);
				if (liquidMat.HasProperty(ColorProperty)) liquidMat.SetColor(ColorProperty, stateColor);

				if (liquidMat.HasProperty(EmissionColorProperty))
				{
					liquidMat.SetColor(EmissionColorProperty, finalEmission);
					liquidMat.EnableKeyword("_EMISSION");
				}

				liquidMat.color = stateColor;
				DynamicGI.SetEmissive(liquidSurfaceRenderer, finalEmission);
			}
		}

		//  3. AMBIENT LIGHTING CUES
		if (cauldronAmbientLight != null)
		{
			cauldronAmbientLight.color = stateColor;
			cauldronAmbientLight.intensity = stateColor == recipeCompleteColor ? 5f : 1f + (potency * 0.3f);
		}
	}
}