using System;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
	[SerializeField] private IReadOnlyDictionary<ulong, NetworkClient> connectedPlayers;
	[SerializeField] private GameObject playerPrefab;
	[SerializeField] private Transform hostSpawn;
	[SerializeField] private Transform clientSpawn;

	private void Start()
	{		
		connectedPlayers = NetworkManager.Singleton.ConnectedClients;
		if (NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject() != null)
		{
			RepositionExistingPlayers();
			return;
		}
		SpawnPlayers();
	}

	
	private void SpawnPlayers()
	{
		foreach (var player in connectedPlayers)
		{
			if (player.Key == NetworkManager.Singleton.LocalClientId)
			{
				Transform spawnPos = IsHost ? hostSpawn : clientSpawn;
				GameObject newPlayer = Instantiate(playerPrefab, spawnPos.position, Quaternion.identity);
				//newPlayer.GetComponent<CinemachineCamera>().Priority = 10;
				newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(player.Key);
			}
		}
	}

	private void RepositionExistingPlayers()
	{
		NetworkObject localPlayer = NetworkManager.Singleton.SpawnManager.GetLocalPlayerObject();
		if (localPlayer != null)
		{
			Transform targetMarker = IsHost ? hostSpawn : clientSpawn;

			// Turn off CharacterController temporarily if your player uses one 
			// so it doesn't fight the teleport physics
			var controller = localPlayer.GetComponent<CharacterController>();
			if (controller != null) controller.enabled = false;

			localPlayer.transform.position = targetMarker.position;
			localPlayer.transform.rotation = targetMarker.rotation;

			if (controller != null) controller.enabled = true;
			Debug.Log($"[SPAWNER] Moved existing player to: {targetMarker.position}");
		}
	}
}
