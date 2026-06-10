using System;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Serialization;

public class SymbolBehaviour : NetworkBehaviour
{
    [SerializeField] private SymbolData symbolData;

    public SymbolData SymbolData
    {
        get => symbolData;
        set => symbolData = value;
    }

    [SerializeField] private NetworkVariable<int> symbolID;

    public int SymbolID
    {
        get => symbolID.Value;
        set => symbolID.Value = value;
    }

    private void Awake()
    {
        symbolID = new NetworkVariable<int>();
    }

    [SerializeField] private SpriteRenderer spriteRenderer;
    public override void OnNetworkSpawn()
    {
        symbolID.OnValueChanged += HandleIDChange;
    }

    private void HandleIDChange(int prev, int current)
    {
        symbolData = SymbolsLookup.Instance.GetSymbol(current);
        spriteRenderer.sprite = symbolData.symbolSprite;
    }

    public void HandleInteract()
    {
        
    }
}
