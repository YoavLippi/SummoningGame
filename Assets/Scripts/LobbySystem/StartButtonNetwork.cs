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
        isReady.OnValueChanged += HandleChange;
    }
    
    public void HandleChange(bool oldVal, bool newVal) 
    {
        
    }

    [Rpc(SendTo.Server)]
    public void ToggleReadyRpc()
    {
        isReady.Value = !isReady.Value;
        model.m_Model.SetReadyFromNetwork(isReady.Value);
    }
}