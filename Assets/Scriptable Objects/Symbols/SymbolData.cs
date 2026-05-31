using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SymbolData", menuName = "Scriptable Objects/Puzzles/SymbolData")]
[Serializable]
//Using this as a scriptableObject so that it's more consistent for checks later on
public class SymbolData : ScriptableObject
{
    public int symbolID;
    public Sprite symbolSprite;
}
