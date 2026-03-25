using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.Events;
using Random = UnityEngine.Random;

public class SymbolPuzzleHandler : MonoBehaviour
{
    [Header("Setup")]
    [SerializeField] private int columnAmount;
    [SerializeField] private int columnSize;
    [SerializeField] private int symbolDisplayAmount;
    [SerializeField] private GameObject[] allSymbols;
    
    [Header("Runtime")]
    [SerializeField] private List<SymbolOrder> symbolOrders;
    //This will be the truth that is referred back to at runtime
    [SerializeField] private List<SymbolData> correctOrder;
    [SerializeField] private List<SymbolData> correctSymbols;
    [SerializeField] private int correctIndex;

    [Serializable]
    public class SymbolOrder
    {
        public List<SymbolData> thisOrder = new();
    }

    [Header("Events")]
    public UnityEvent puzzleFailure;
    void Start()
    {
        correctSymbols = new List<SymbolData>();
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
        foreach (var symbolObj in allSymbols)
        {
            pullList.Add(symbolObj.GetComponent<SymbolBehaviour>().SymbolData);
        }
        
        //resetting correct symbols and populating random new ones
        symbolOrders.Clear();
        correctSymbols.Clear();
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
            foreach (var symbolObj in allSymbols)
            {
                pullList.Add(symbolObj.GetComponent<SymbolBehaviour>().SymbolData);
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
                    availableIndexes.Remove(subIndex);
                }

                correctOrder = newOrder.thisOrder;
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
}
