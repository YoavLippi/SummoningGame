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

	[Header("Cutscene Setup")]
	[SerializeField] private GameObject cutsceneDirectorObject;

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

		// Optional: If you want to run code the moment the targets land on the client
		//targetPotency.OnValueChanged += (oldVal, newVal) => {
		//	Debug.Log($"[CLIENT] My book needs to look for a potion with Potency: {newVal}");
		//};
	}

	[ServerRpc(RequireOwnership = false)]
	public void MixIngredientServerRpc(int potencyModifier, int instabilityModifier)
	{
		currentPotency.Value += potencyModifier;
		currentInstability.Value += instabilityModifier;

		if (currentPotency.Value < 0) currentPotency.Value = 0;
		if (currentInstability.Value < 0) currentInstability.Value = 0;

		CheckPotionSolution();
	}

	private void CheckPotionSolution()
	{
		if (!IsServer) return;

		// We compare the current NetworkVariable values against the random target values
		if (currentPotency.Value == targetPotency.Value && currentInstability.Value == targetInstability.Value)
		{
			Debug.Log("Dynamic potion recipe matched perfectly! Playing cutscene...");
			PlaySuccessCutsceneClientRpc();
		}
	}

	[ClientRpc]
	private void PlaySuccessCutsceneClientRpc()
	{
		if (cutsceneDirectorObject != null)
		{
			cutsceneDirectorObject.SetActive(true);
		}
		//DisablePlayerControls();
	}

	//private void DisablePlayerControls()
	//{
	//	if (NetworkManager.Singleton.LocalClient.PlayerObject != null)
	//	{
	//		// Add your character controller disabling code here
	//	}
	//}
}
