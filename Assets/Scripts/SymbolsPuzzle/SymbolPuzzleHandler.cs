using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Serialization;
using Random = UnityEngine.Random;

[RequireComponent(typeof(NetworkObject))]
public class SymbolPuzzleHandler : NetworkBehaviour
{
    [Header("Setup")]
    [SerializeField] private int columnAmount;
    [SerializeField] private int columnSize;
    [SerializeField] private int symbolDisplayAmount;
    //[SerializeField] private GameObject[] allSymbols;
    [FormerlySerializedAs("displaySymbols")] [SerializeField] private SymbolBehaviour[] displaySymbolsSmall;
    [SerializeField] private GameObject displaySymbolsLargeParent;
    
    [Header("Runtime")]
    [SerializeField] private List<SymbolOrder> symbolOrders;
    //This will be the truth that is referred back to at runtime
    [SerializeField] private List<SymbolData> correctOrderFull;
    [SerializeField] private List<SymbolData> correctOrderRelevantOnly;
    [SerializeField] private List<SymbolData> correctSymbols;
    [SerializeField] private int correctIndex;

    [Header("Network")]
    //Flattening all orders so that they can be transmitted
    public NetworkList<int> allOrdersNetwork = new NetworkList<int>();
    public NetworkList<int> correctOrderNetwork = new NetworkList<int>();
    public NetworkList<int> correctRelevantOnlyNetwork = new NetworkList<int>();
    public NetworkList<int> displaySymbolsNetwork = new NetworkList<int>();

    [Serializable]
    public class SymbolOrder
    {
        public List<SymbolData> thisOrder = new();
    }

    [Header("Events")]
    [SerializeField] private UnityEvent onComplete;
    [SerializeField] private UnityEvent onFail;
    public override void OnNetworkSpawn()
    {
        if (IsServer)
        {
            correctSymbols = new List<SymbolData>();
            StartCoroutine(DelaySpawn());
        }
    }
    
    //manually delaying because there's an issue with race conditions
    private IEnumerator DelaySpawn()
    {
        yield return new WaitForSeconds(0.2f);
        PopulateSymbolOrders(columnSize, columnAmount);
    }

    /// <summary>
    /// Helper method to fill the orders that symbols appear in and select the symbols that will be showm
    /// </summary>
    /// <param name="colSize">how big each individual column will be</param>
    /// <param name="colAmount">the number of columns</param>
    /// <exception cref="IndexOutOfRangeException"></exception>
    public void PopulateSymbolOrders(int colSize, int colAmount)
    {
        //Generating initial list of symbol data to pull from
        List<SymbolData> pullList = new List<SymbolData>();
        foreach (var symbol in SymbolsLookup.Instance.symbols)
        {
            pullList.Add(symbol);   
        }
        
        //resetting correct symbols and populating random new ones
        symbolOrders.Clear();
        correctSymbols.Clear();
        correctOrderFull.Clear();
        correctOrderRelevantOnly.Clear();
        
        for (int i = 0; i < symbolDisplayAmount; i++)
        {
            int pullIndex = Random.Range(0, pullList.Count);
            correctSymbols.Add(pullList[pullIndex]);
            pullList.RemoveAt(pullIndex);
        }
        
        correctIndex = Random.Range(0, colAmount);
        //populating columns
        for (int i = 0; i < colAmount; i++)
        {
            //reset pull list
            pullList.Clear();
            foreach (var symbol in SymbolsLookup.Instance.symbols)
            {
                pullList.Add(symbol);
            }
            
            if (colSize >= pullList.Count)
            {
                throw new IndexOutOfRangeException("There are less symbols in 'all symbols' than the size of the columns - this would cause duplication. Please add more symbols.");
            }

            SymbolOrder newOrder = new SymbolOrder();
            for (int j = 0; j < colSize; j++)
            {
                int pullIndex = Random.Range(0, pullList.Count);
                newOrder.thisOrder.Add(pullList[pullIndex]);
                pullList.RemoveAt(pullIndex);
            }

            if (i == correctIndex)
            {
                //making sure it doesn't overwrite already modified entries
                List<int> availableIndexes = new List<int>();
                for (int k = 0; k < colSize; k++)
                {
                    availableIndexes.Add(k);
                }
                
                foreach (var symbol in correctSymbols)
                {
                    if (newOrder.thisOrder.Contains(symbol))
                    {
                        availableIndexes.Remove(newOrder.thisOrder.IndexOf(symbol));
                        continue;
                    }
                    
                    int subIndex = Random.Range(0, availableIndexes.Count);
                    int replaceIndex = availableIndexes[subIndex];

                    newOrder.thisOrder[replaceIndex] = symbol;
                    availableIndexes.Remove(replaceIndex);
                }

                correctOrderFull = newOrder.thisOrder;
                
                //pupulating the correct symbol order in particular
                foreach (var symbol in correctOrderFull)
                {
                    if (correctSymbols.Contains(symbol))
                    {
                        correctOrderRelevantOnly.Add(symbol);
                    }
                }
            }
            else
            {
                //making sure the column at this (incorrect) index does not have the correct symbols 
                if (HasAllSymbols(correctSymbols.ToArray(), newOrder.thisOrder.ToArray()))
                {
                    i--;
                    continue;
                }
            }
            
            //adding new column to actual columns
            symbolOrders.Add(newOrder);
        }
        
        PopulateNetworkOrders();
    }

    //network lists can't store structs, so we're flattening them down to 1d arrays of symbol IDs that we can then reconstruct later
    private void PopulateNetworkOrders()
    {
        //each column will occupy [columnSize] spaces in the 1D array
        int totalOrderSize = columnSize * columnAmount;
        foreach (var symbolOrder in symbolOrders)
        {
            var temp = symbolOrder;
            foreach (var symbol in temp.thisOrder)
            {
                allOrdersNetwork.Add(symbol.symbolID);
            }
        }
        SymbolBehaviour[] displaySymbolsLarge = displaySymbolsLargeParent.GetComponentsInChildren<SymbolBehaviour>();
        int count = 0;
        for (int i = 0; i < columnAmount; i++)
        {
            for (int j = 0; j < columnSize; j++)
            {
                displaySymbolsLarge[count].SymbolID = symbolOrders[i].thisOrder[j].symbolID;
                count++;
            }
        }
        

        int startingIndex = correctIndex * columnSize;
        for (int i = 0; i < columnSize; i++)
        {
            correctOrderNetwork.Add(allOrdersNetwork[startingIndex+i]);
        }

        foreach (var symbol in correctOrderRelevantOnly)
        {
            correctRelevantOnlyNetwork.Add(symbol.symbolID);
        }

        foreach (var symbol in correctSymbols)
        {
            displaySymbolsNetwork.Add(symbol.symbolID);
        }
        
        for (int i = 0; i < displaySymbolsSmall.Length; i++)
        {
            displaySymbolsSmall[i].SymbolID = correctSymbols[i].symbolID;
        }
    }

    /// <summary>
    /// Helper method. Returns true if all the symbols in symbols are found within the full array
    /// </summary>
    /// <param name="symbols">The smaller list of symbols within the array</param>
    /// <param name="fullArray">The array itself</param>
    /// <returns>true if all the symbols in symbols are found within the full array, false otherwise</returns>
    /// <exception cref="IndexOutOfRangeException"></exception>
    private bool HasAllSymbols(SymbolData[] symbols, SymbolData[] fullArray)
    {
        foreach (var symbol in symbols)
        {
            if (!fullArray.Contains(symbol)) return false;
        }

        return true;
    }

    [Rpc(SendTo.Server)]
    //needs to be an int array because symbolData is not allowed to be passed over the network
    public void ValidateChoiceRpc(int[] choices)
    {
        for (int i = 0; i < choices.Length; i++)
        {
            if (choices[i] != correctRelevantOnlyNetwork[i])
            {
                onFail.Invoke();
                return;
            }
        }
        onComplete.Invoke();
    }
}
