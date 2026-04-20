using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionHandler : NetworkBehaviour
{
    public void Start()
    {
        if (!IsOwner) return;
        //setup hotbar
        //these are each slot
        var allSlots = GameObject.FindGameObjectsWithTag("Hotbar");
        foreach (var slot in allSlots)
        {
            hotbarSlots.Add(slot);
        }

        hotbarSize = allSlots.Length;
        //finding selector
        selectionBox = GameObject.FindWithTag("HotbarSelector");
        
        SortHotbar();
        StartCoroutine(SetSelectionAfterDelay(0));
    }
    
    #region Hotbar Controller

    private IEnumerator SetSelectionAfterDelay(int num)
    {
        yield return new WaitForFixedUpdate();
        SetSelection(num);
    }

    [Header("Hotbar")] 
    [Header("Runtime")]
    [SerializeField] private int hotbarSize = 9;
    [SerializeField] private List<GameObject> hotbarSlots;
    //indicates which slot is currently selected (Some sort of overlay)
    [SerializeField] private GameObject selectionBox;
    [SerializeField] private int currentSelection = 0;
    
    public enum Color
    {
        Red,
        Blue,
        Yellow,
        Green,
        Orange,
        Purple,
        Pink,
        White
    }

    public int CurrentSelection
    {
        get => currentSelection;
        set => currentSelection = value;
    }
    
    public void OnScroll(InputAction.CallbackContext context)
    {
        //making sure it only catches the inputs from the player
        if (!IsOwner) return;
        //should only fire once per scroll
        if (!context.performed) return;
        
        //casting current selection from a possible float value to nice integers
        int scrollDelta = context.ReadValue<float>() > 0 ? 1 : -1;
        currentSelection += scrollDelta;
        
        //looping from either side, 0 counting so adjusting for that
        if (currentSelection > hotbarSize-1) currentSelection = 0;
        if (currentSelection < 0) currentSelection = hotbarSize-1;
        
        SetSelection(currentSelection);
    }

    private void SetSelection(int value)
    {
        //redundancy so we can also set it manually
        currentSelection = value;
        /*RectTransform slotRect = hotbarSlots[value].GetComponent<RectTransform>();
        RectTransform selectionRect = selectionBox.GetComponent<RectTransform>();

        selectionRect.anchoredPosition = slotRect.anchoredPosition;*/
        selectionBox.transform.position = hotbarSlots[value].transform.position;
    }

    private void SortHotbar()
    {
        //we need to make sure the hotbar pieces are in order, furthest left is lowest index
        for (int i = 0; i < hotbarSize; i++)
        {
            for (int j = 0; j < hotbarSize; j++)
            {
                if (GetSlotValue(hotbarSlots[i].name) < GetSlotValue(hotbarSlots[j].name))
                {
                    //swapping with destructor notation! How cool??
                    (hotbarSlots[i],hotbarSlots[j]) = (hotbarSlots[j], hotbarSlots[i]);
                }
            }
        }
    }

    private int GetSlotValue(string slotName)
    {
        if (!slotName.Contains(' '))
        {
            Debug.LogError("Slot names require a space with a number directly afterwards!");
        }
        int.TryParse(slotName.Substring(slotName.IndexOf(' ')), out int slot);
        return slot;
    }

    #endregion
}
