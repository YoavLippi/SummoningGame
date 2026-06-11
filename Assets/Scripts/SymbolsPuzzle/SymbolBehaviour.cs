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

    [SerializeField] private GameObject borderDisplay;

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
    private static readonly int IsGrayscale = Shader.PropertyToID("_isGrayscale");

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
        borderDisplay.SetActive(true);
        GetComponent<Collider>().enabled = false;
        //_isGrayscale
        GetComponent<SpriteRenderer>().material.SetFloat(IsGrayscale, 1.0f);
    }

    public void ResetSymbol()
    {
        borderDisplay.SetActive(false);
        GetComponent<Collider>().enabled = true;
        GetComponent<SpriteRenderer>().material.SetFloat(IsGrayscale, 0f);
    }
}
