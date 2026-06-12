using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameLogicController : NetworkBehaviour
{
    [SerializeField]
    private NetworkVariable<bool> isOtherDone =
        new NetworkVariable<bool>(false);
    [SerializeField] private string nextSceneName = "WinScene"; // set in Inspector

    public UnityEvent winEvent;
    public UnityEvent PuzzleCompleteCutsceneEvent;

    [Rpc(SendTo.Server)]
    public void CheckFlagRpc()
    {
        if (!isOtherDone.Value)
        {
            isOtherDone.Value = true;
            PlayPuzzleCompleteCutsceneClientRpc();
        }
        else
        {
            // Everyone is done — destroy players, then load next scene
            DespawnPlayersAndLoadScene();
        }
    }

    private void DespawnPlayersAndLoadScene()
    {
        // Must run on server
        if (!IsServer) return;

        // Despawn every connected player object before the scene transition
        foreach (var client in NetworkManager.Singleton.ConnectedClientsList)
        {
            if (client.PlayerObject != null)
            {
                client.PlayerObject.Despawn(destroy: true);
            }
        }

        // NetworkSceneManager handles the load on all clients automatically
        NetworkManager.Singleton.SceneManager.LoadScene(
            nextSceneName,
            LoadSceneMode.Single
        );
    }

    [Rpc(SendTo.Everyone)]
    private void PlayPuzzleCompleteCutsceneClientRpc()
    {
        PuzzleCompleteCutsceneEvent.Invoke();
    }
}