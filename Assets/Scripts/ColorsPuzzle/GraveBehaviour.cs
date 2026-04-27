using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;
using UnityEngine.SceneManagement;

public class GraveBehaviour : NetworkBehaviour
{
	//[SerializeField]
	//private NetworkVariable<List<InteractionHandler.Color>> inputtedColors =
	//new NetworkVariable<List<InteractionHandler.Color>>();
	//[SerializeField] private List<InteractionHandler.Color> inputtedColors;
	[SerializeField] private NetworkList<int> inputtedColors = new NetworkList<int>();

	//just to see in the editor: DO NOT USE
	[SerializeField] private List<int> debugColorsViewer;
	[SerializeField] private ColorGameController _gameController;
	[SerializeField] private bool isSolutionCorrect = false;

	[Header("Ritual Visuals")]
	[SerializeField] private GameObject[] runeObjects;
	[SerializeField] private UnityEngine.Color[] palette;

	[Header("Status Colors")]
	[SerializeField] private UnityEngine.Color successColor = UnityEngine.Color.green;
	[SerializeField] private UnityEngine.Color failColor = UnityEngine.Color.red;
	[SerializeField] private float flashDuration = 5f;

    [Header("Absorption Effect")]
    [SerializeField] private ParticleSystem absorptionEffect;

    private bool isFlashing = false;

	private void Start()
	{
		if (!IsServer) return;
		_gameController = GameObject.FindWithTag("GameController").GetComponent<ColorGameController>();
		StopAllCoroutines();
	}

	public override void OnNetworkSpawn()
	{
		debugColorsViewer = new List<int>();

		inputtedColors.OnListChanged += (changeEvent) =>
		{
			debugColorsViewer.Clear();
			foreach (var color in inputtedColors)
			{
				debugColorsViewer.Add(color);
			}
			UpdateAllRunes();
		};
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void AddColorRpc(InteractionHandler.Color newColor)
	{
		if (newColor == InteractionHandler.Color.White)
		{
			RemoveTopRpc();
		}
		else
		{
			inputtedColors.Add((int)newColor);
			if (inputtedColors.Count >= _gameController.solutionSize)
			{
				StartCoroutine(WaitCheckSolutionRpc());
				//StartCheckSolutionRpc();
				/*CheckSolutionRpc();
				if (isSolutionCorrect)
				{
					//win logic here
					Debug.Log("WIN!!");
					TriggerStatusFlashClientRpc(true);
				}
				else
				{
					//lose logic here
					Debug.Log("LOSE!");
					TriggerStatusFlashClientRpc(false);
					inputtedColors.Clear();
				}
				//cleanup user objects (?)
				CleanupPlayersClientRpc();

				//boot to main menu
				//TODO: ADD IN END CUTSCENE
				NetworkManager.Singleton.SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);*/
			}
		}
		//Debug.Log("Recieved RPC");
	}

	[ClientRpc]
	public void CleanupPlayersClientRpc()
	{
		Cursor.lockState = CursorLockMode.None;
		Cursor.visible = true;
		NetworkManager.Singleton.LocalClient.PlayerObject.Despawn(true);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void RemoveColorRpc(InteractionHandler.Color newColor)
	{
		if (inputtedColors.Contains((int)newColor))
		{
			inputtedColors.Remove((int)newColor);
		}
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	public void RemoveTopRpc()
	{
		if (inputtedColors.Count <= 0) return;
		inputtedColors.RemoveAt(inputtedColors.Count - 1);
	}

	[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void StartCheckSolutionRpc()
	{
		StartCoroutine(WaitCheckSolutionRpc());
	}
	
	private IEnumerator WaitCheckSolutionRpc()
	{
		if (!IsServer) yield break;
		CheckSolutionRpc();
		yield return new WaitForSeconds(0.2f);
		
		Debug.Log(isSolutionCorrect?"WIN":"LOSE");
		
		ChangeLightColorsClientRpc(isSolutionCorrect);

		yield return new WaitForSeconds(flashDuration);
		//cleanup user objects (?)
		CleanupPlayersClientRpc();

		//boot to main menu
		//TODO: ADD IN END CUTSCENE
		NetworkManager.Singleton.SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
	}

	//[Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
	private void CheckSolutionRpc()
	{
		if (inputtedColors.Count != _gameController.solution.Count)
		{
			isSolutionCorrect = false;
			return;
		}

		for (int i = 0; i < inputtedColors.Count; i++)
		{
			if (inputtedColors[i] != _gameController.solution[i])
			{
				isSolutionCorrect = false;
				return;
			}
		}

		isSolutionCorrect = true;
	}

	private void UpdateAllRunes()
	{
		for (int i = 0; i < runeObjects.Length; i++)
		{
			// If the Apprentice has shot this many colors, light up the rune
			if (i < inputtedColors.Count)
			{
				runeObjects[i].SetActive(true);
				int colorIndex = inputtedColors[i];
				UnityEngine.Color shotColor = palette[colorIndex];

				Light light = runeObjects[i].GetComponent<Light>();
				//var light = runeObjects[i].GetComponentInChildren<Light>();

				if (light != null)
				{
					light.color = shotColor;
					light.intensity = 3f; 
				}

                // Play absorption with matching color on the most recently added rune
                if (i == inputtedColors.Count - 1 && absorptionEffect != null)
                {
                    var main = absorptionEffect.main;
                    main.startColor = shotColor;
                    absorptionEffect.Play();
                }

            }
            else
			{
				runeObjects[i].SetActive(false);
			}
		}
	}
	
	[ClientRpc]
	private void ChangeLightColorsClientRpc(bool success)
	{
		Debug.Log($"ClientRpc running on client: {NetworkManager.Singleton.LocalClientId}");
		UnityEngine.Color targetColor = success ? successColor : failColor;
		foreach (var rune in runeObjects)
		{
			Debug.Log("Checking new rune");
			//rune.SetActive(true);
			//var light = rune.GetComponentInChildren<Light>();
			Light light = rune.GetComponent<Light>();
			if (light != null)
			{
				Debug.Log("LIGHT WAS NOT NULL");
				light.color = targetColor;
				light.intensity = 5f; 
			}
		}
	}
	private System.Collections.IEnumerator FlashSequence(bool success)
	{
		isFlashing = true;
		//UnityEngine.Color targetColor = success ? successColor : failColor;
		// Turn ALL runes to the status color
		/*foreach (var rune in runeObjects)
		{
			Debug.Log("Checking new rune");
			rune.SetActive(true);
			//var light = rune.GetComponentInChildren<Light>();
			Light light = rune.GetComponent<Light>();
			if (light != null)
			{
				Debug.Log("LIGHT WAS NOT NULL");
				light.color = targetColor;
				light.intensity = 5f; 
			}
		}*/
		Debug.Log("CallingRpc");
		ChangeLightColorsClientRpc(success);

		yield return new WaitForSeconds(flashDuration);
		isFlashing = false;

		if (!success)
		{
			// If they failed, turn everything off 
			foreach (var rune in runeObjects) rune.SetActive(false);
			// Only the server clears the actual data
			if (IsServer) inputtedColors.Clear();
		}
		else
		{
			// When they won, we want to keep them Green 
			// until the scene loads for the cat to rise!
			Debug.Log("Ritual Complete.");
		}
	}
}
