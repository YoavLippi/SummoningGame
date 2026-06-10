using UnityEngine;
using Unity.Netcode;

public class PotionCauldron : NetworkBehaviour
{
	[Header("Current Mixture State")]
	public NetworkVariable<int> currentPotency = new NetworkVariable<int>(0);
	public NetworkVariable<int> currentInstability = new NetworkVariable<int>(0);

	[Header("Dynamic Targets (Server Picked)")]
	// We make these NetworkVariables so the Apprentice's UI/Book can display the correct answer!
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

	private static readonly int BaseColorProperty = Shader.PropertyToID("_BaseColor");
	private static readonly int EmissionColorProperty = Shader.PropertyToID("_EmissionColor");
	private static readonly int ColorProperty = Shader.PropertyToID("_Color");
	public override void OnNetworkSpawn()
	{
		// Only the Server is allowed to roll the dice on the targets
		if (IsServer)
		{
			// Pick a random required potency between 8 and 18
			targetPotency.Value = Random.Range(8, 19);

			// Usually, you want instability to hit 0, but we can make it require exactly 0 or 1!
			targetInstability.Value = Random.Range(0, 2);

			Debug.Log($"[SERVER] New Potion Recipe Generated! Target Potency: {targetPotency.Value}, Target Instability: {targetInstability.Value}");
		}
	}

	[ServerRpc(RequireOwnership = false)]
	public void MixIngredientServerRpc(int potencyModifier, int instabilityModifier)
	{
		if (IsPuzzleComplete) return;

		currentPotency.Value += potencyModifier;
		currentInstability.Value += instabilityModifier;

		if (currentPotency.Value < 0) currentPotency.Value = 0;
		if (currentInstability.Value < 0) currentInstability.Value = 0;

		UpdateCauldronVisualsClientRpc(currentPotency.Value, currentInstability.Value);

		CheckPotionSolution();
	}

	private void CheckPotionSolution()
	{
		if (!IsServer) return;

		// We compare the current NetworkVariable values against the random target values
		if (currentPotency.Value == targetPotency.Value && currentInstability.Value == targetInstability.Value)
		{
			Debug.Log("Dynamic potion recipe matched perfectly! Playing cutscene...");
			IsPuzzleComplete = true; // Lock local server interactions
			LockPuzzleStateClientRpc();
			PlaySuccessCutsceneClientRpc();
		}
	}

	[ClientRpc]
	private void LockPuzzleStateClientRpc()
	{
		IsPuzzleComplete = true; // Every client machine now registers the lock state
	}

	[ClientRpc]
	private void PlaySuccessCutsceneClientRpc()
	{
		if (cutsceneDirectorObject != null)
		{
			cutsceneDirectorObject.SetActive(true);
		}
	}


	[ClientRpc]
	private void UpdateCauldronVisualsClientRpc(int potency, int instability)
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
				
				// Level 1 instability = 65 particles | Level 2 = 100 particles | Level 3 = 135 particles
				smokeEmission.rateOverTime = 30f + (instability * 35f);

				// Make the smoke shoot upward much faster as pressure builds
				smokeMain.startSpeed = 1.5f + (instability * 0.8f);
				smokeMain.startColor = stateColor; // Uses your designer configured yellow/red states

				
				// Level 1 = 50 bubbles | Level 2 = 90 bubbles | Level 3 = 130 bubbles
				bubbleEmission.rateOverTime = 10f + (instability * 40f);
			}
			else
			{
				// Baseline Calm State (Instability == 0)
				smokeEmission.rateOverTime = 10f;
				smokeMain.startSpeed = 0.6f;
				smokeMain.startColor = stableColor; // Clean, calm cyan/purple tint

				bubbleEmission.rateOverTime = 10f; // Soft, slow bubbling
			}
		}

		// 2. HANDLE POTENCY LIQUID GLOW
		if (liquidSurfaceRenderer != null)
		{
			Material liquidMat = liquidSurfaceRenderer.material;
			if (liquidMat != null)
			{
				// Define a steady, unmoving brightness level for your magic liquid.
				// 1.5f to 2.0f gives a beautiful, rich neon glow without overexposing to white.
				float constantGlow = 1.25f;

				// Force a slightly brighter burst only when they successfully win the puzzle
				if (stateColor == recipeCompleteColor) constantGlow = 3.5f;

				Color finalEmission = stateColor * constantGlow;

				// Apply clean, solid tint colors to the base channels
				if (liquidMat.HasProperty(BaseColorProperty)) liquidMat.SetColor(BaseColorProperty, stateColor);
				if (liquidMat.HasProperty(ColorProperty)) liquidMat.SetColor(ColorProperty, stateColor);

				// Apply the steady emission strength to the shader registers
				if (liquidMat.HasProperty(EmissionColorProperty))
				{
					liquidMat.SetColor(EmissionColorProperty, finalEmission);
					liquidMat.EnableKeyword("_EMISSION");
				}

				liquidMat.color = stateColor;

				// Push updates straight to the real-time illumination loop
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
