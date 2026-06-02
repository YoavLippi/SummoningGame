using System;
using System.Collections.Generic;
using UnityEngine;

public class SymbolsLookup : MonoBehaviour
{
    public static SymbolsLookup Instance;
    public SymbolData[] symbols;
    
    private Dictionary<int, SymbolData> lookupTable;
    private void Awake()
    {
        Instance = this;

        lookupTable = new Dictionary<int, SymbolData>();

        foreach (var symbol in symbols)
        {
            lookupTable.Add(symbol.symbolID, symbol);
        }
    }

    public SymbolData GetSymbol(int symbolID)
    {
        //SymbolData output = new SymbolData();
        return lookupTable[symbolID];
    }
}
