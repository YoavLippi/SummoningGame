using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UIElements;

[RequireComponent(typeof(NetworkObject))]
public class StartButtonNetwork : NetworkBehaviour
{
    public static StartButtonNetwork Instance;

    public NetworkVariable<bool> isReady = new NetworkVariable<bool>(false);

    public UIDocument browserTree;
    private StartButton model;

    private void Awake()
    {
        Instance = this;
    }

    private void Start()
    {
        model = browserTree.rootVisualElement.Q<StartButton>("StartReady");
    }

    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            NetworkManager.Singleton.OnClientDisconnectCallback += OnClientDisconnected;
        }
        isReady.OnValueChanged += HandleChangeRpc;
        
        SetReadyRpc(false);
    }
    
    [Rpc(SendTo.Everyone)]
    private void HandleChangeRpc(bool oldVal, bool newVal) 
    {
        if (!IsServer)
        {
            model.m_Model.SetTextFromNetwork(newVal? "Unready" : "Ready");
        }
    }

    [Rpc(SendTo.Server)]
    public void ToggleReadyRpc()
    {
        isReady.Value = !isReady.Value;
        model.m_Model.SetReadyFromNetwork(isReady.Value);
    }

    [Rpc(SendTo.Server)]
    public void SetReadyRpc(bool value)
    {
        isReady.Value = value;
        model.m_Model.SetReadyFromNetwork(value);
    }

    public void OnClientDisconnected(ulong clientID)
    {
        //UnreadyRpc();
        isReady.Value = false;
        model.m_Model.SetReadyFromNetwork(false);
    }
}