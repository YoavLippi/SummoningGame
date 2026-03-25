using System;
using UnityEngine;

[CreateAssetMenu(fileName = "SymbolData", menuName = "Scriptable Objects/Puzzles/SymbolData")]
[Serializable]
//Using this as a scriptableObject so that it's more consistent for checks later on
public class SymbolData : ScriptableObject
{
    public string symbolID;
    public Sprite symbolSprite;
    
#if UNITY_EDITOR
    //Assigning ID based on GUID
    protected virtual void OnValidate()
    {
        // If the ID is empty, generate a new one
        if (string.IsNullOrEmpty(symbolID))
        {
            symbolID = Guid.NewGuid().ToString();
            // This apparently makes it save properly
            UnityEditor.EditorUtility.SetDirty(this);
        }
    }
#endif
}
