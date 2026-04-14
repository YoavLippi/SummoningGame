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
                newPlayer.GetComponent<CinemachineCamera>().Priority = 10;
                newPlayer.GetComponent<NetworkObject>().SpawnAsPlayerObject(player.Key);
            }
        }
    }
}
