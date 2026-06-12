using Unity.Netcode;
using UnityEngine;

public class PlayerSpawner : NetworkBehaviour
{
    [SerializeField] private GameObject playerPrefab;
    [SerializeField] private Transform hostSpawn;
    [SerializeField] private Transform clientSpawn;

    public override void OnNetworkSpawn()
    {
        // Only the server should spawn player objects
        if (!IsServer) return;

        SpawnPlayers();
    }

    private void SpawnPlayers()
    {
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            Transform spawnPos = (client.ClientId == NetworkManager.ServerClientId)
                ? hostSpawn
                : clientSpawn;

            GameObject newPlayer = Instantiate(
                playerPrefab,
                spawnPos.position,
                Quaternion.identity
            );

            newPlayer.GetComponent<NetworkObject>()
                     .SpawnAsPlayerObject(client.ClientId);
        }
    }
}