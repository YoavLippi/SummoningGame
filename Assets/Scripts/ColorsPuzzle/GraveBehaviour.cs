using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using System.Linq;

public class GraveBehaviour : NetworkBehaviour
{
    //[SerializeField]
    //private NetworkVariable<List<InteractionHandler.Color>> inputtedColors =
        //new NetworkVariable<List<InteractionHandler.Color>>();
    //[SerializeField] private List<InteractionHandler.Color> inputtedColors;
    [SerializeField] private NetworkList<int> inputtedColors = new NetworkList<int>();
    
    //just to see in the editor: DO NOT USE
    [SerializeField] private List<int> debugColorsViewer;
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
        Debug.Log("Recieved RPC");
        inputtedColors.Add((int)newColor);
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
}
