using Unity.Netcode;
using UnityEngine;

public class ColorGameController : NetworkBehaviour
{
    public NetworkList<int> solution = new NetworkList<int>(null,	NetworkVariableReadPermission.Everyone,	NetworkVariableWritePermission.Server);
    public int solutionSize = 5;
    [SerializeField] private int numberOfColors = 7;

    public override void OnNetworkSpawn()
    {
        if (!IsServer) return;

        for (int i = 0; i < solutionSize; i++)
        {
            int nextColor = Random.Range(0, numberOfColors);
            solution.Add(nextColor);
        }
    }
}
