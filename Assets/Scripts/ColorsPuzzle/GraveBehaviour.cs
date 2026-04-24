using System;
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

    private void Start()
    {
        if (!IsServer) return;
        _gameController = GameObject.FindWithTag("GameController").GetComponent<ColorGameController>();
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
                CheckSolutionRpc();
                if (isSolutionCorrect)
                {
                    //win logic here
                    Debug.Log("WIN!!");
                }
                else
                {
                    //lose logic here
                    Debug.Log("LOSE!");
                }
                //cleanup user objects (?)
                CleanupPlayersClientRpc();
                
                //boot to main menu
                //TODO: ADD IN END CUTSCENE
                NetworkManager.Singleton.SceneManager.LoadScene("Main Menu", LoadSceneMode.Single);
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
        inputtedColors.RemoveAt(inputtedColors.Count-1);
    }

    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
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
}
