using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
public class GameLogicController : NetworkBehaviour
{
    [SerializeField] private NetworkVariable<bool> isOtherDone = new NetworkVariable<bool>(false);
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
            winEvent.Invoke();
        }
    }
    [Rpc(SendTo.Everyone)]
    private void PlayPuzzleCompleteCutsceneClientRpc()
    {
        PuzzleCompleteCutsceneEvent.Invoke();
    }
}