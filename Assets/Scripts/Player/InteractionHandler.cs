using System;
using System.Collections;
using System.Collections.Generic;
using Unity.Cinemachine;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.InputSystem;

public class InteractionHandler : NetworkBehaviour
{
    [Header("Controllers")]
    [SerializeField] private WandController _wandController;

    [SerializeField] private CinemachineCamera playerViewCam;

    public void Start()
    {
        if (!IsOwner) return;

        // setup hotbar
        // these are each slot
        var allSlots = GameObject.FindGameObjectsWithTag("Hotbar");
        foreach (var slot in allSlots)
        {
            hotbarSlots.Add(slot);
        }

        hotbarSize = allSlots.Length;
        // finding selector
        selectionBox = GameObject.FindWithTag("HotbarSelector");
        symbolPuzzleHandler = GameObject.FindWithTag("GameController").GetComponent<SymbolPuzzleHandler>();
        SortHotbar();
        StartCoroutine(SetSelectionAfterDelay(0));

        _wandController = GetComponent<WandController>();
        
        //setup interaction prompt
        interactPrompt = GameObject.FindWithTag("InteractPrompt");
    }

    #region Hotbar Controller

    private IEnumerator SetSelectionAfterDelay(int num)
    {
        yield return new WaitForSeconds(0.2f);
        SetSelection(num);
    }

    [Header("Hotbar")]
    [SerializeField] private int hotbarSize = 9;
    [SerializeField] public List<GameObject> hotbarSlots = new List<GameObject>();
    // indicates which slot is currently selected (Some sort of overlay)
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

    public void OnKeynum(InputAction.CallbackContext context)
    {
        // making sure it only catches the inputs from the player
        if (!IsOwner) return;
        // should only fire once
        if (!context.performed) return;
        if (!isDetectingInput) return;

        // value is the key on the keypad which has been pressed
        if (int.TryParse(context.control.name, out int value))
        {
            SetSelection(value - 1);
        }
    }

    public void OnScroll(InputAction.CallbackContext context)
    {
        // making sure it only catches the inputs from the player
        if (!IsOwner) return;
        // should only fire once per scroll
        if (!context.performed) return;
        if (!isDetectingInput) return;

        // casting current selection from a possible float value to nice integers
        int scrollDelta = context.ReadValue<float>() > 0 ? 1 : -1;
        currentSelection += scrollDelta;

        // looping from either side, 0 counting so adjusting for that
        if (currentSelection > hotbarSize - 1) currentSelection = 0;
        if (currentSelection < 0) currentSelection = hotbarSize - 1;

        SetSelection(currentSelection);
    }

    private void SetSelection(int value)
    {
        // redundancy so we can also set it manually
        currentSelection = value;
        /*RectTransform slotRect = hotbarSlots[value].GetComponent<<RectTransform>();
        RectTransform selectionRect = selectionBox.GetComponent<<RectTransform>();

        selectionRect.anchoredPosition = slotRect.anchoredPosition;*/
        if (selectionBox != null && hotbarSlots != null && hotbarSlots.Count > value)
            selectionBox.transform.position = hotbarSlots[value].transform.position;
    }

    private void SortHotbar()
    {
        // we need to make sure the hotbar pieces are in order, furthest left is lowest index
        for (int i = 0; i < hotbarSize; i++)
        {
            for (int j = 0; j < hotbarSize; j++)
            {
                if (GetSlotValue(hotbarSlots[i].name) < GetSlotValue(hotbarSlots[j].name))
                {
                    // swapping with destructor notation! How cool??
                    (hotbarSlots[i], hotbarSlots[j]) = (hotbarSlots[j], hotbarSlots[i]);
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

    #region FiringBehaviour

    [Header("Firing")]
    [SerializeField] private Transform wandPos;

    [SerializeField] private float reachDistance = 50f;

    [SerializeField] private GameObject interactPrompt;
    
    public bool isDetectingInput = true;
    
    private void FixedUpdate()
    {
        if (!IsOwner) return;
        Ray caster = new Ray(playerViewCam.transform.position, playerViewCam.transform.forward);
        #if UNITY_EDITOR
        Debug.DrawRay(playerViewCam.transform.position, playerViewCam.transform.forward * reachDistance, UnityEngine.Color.red);
        #endif
        LayerMask totalMask = catMask | symbolMask | candleMask | woundMask;

        interactPrompt.SetActive(Physics.Raycast(playerViewCam.transform.position, playerViewCam.transform.forward, reachDistance, totalMask));
    }

    [SerializeField] private LayerMask catMask;
    
    [Header("Symbols Puzzle")]
    [SerializeField] private LayerMask symbolMask;

    [SerializeField] private List<int> hitSymbols = new List<int>();
    //also storing the gameobject references because it's easier to code for resetting them later
    [SerializeField] private List<SymbolBehaviour> hitSymbolObjects = new List<SymbolBehaviour>();
    [SerializeField] private SymbolPuzzleHandler symbolPuzzleHandler;
    
    [Header("Candle Puzzle")]
    [SerializeField] private LayerMask candleMask;
    [SerializeField] private LayerMask woundMask;

    // tracks whether this player is currently holding the wound open
    private bool isHoldingWound = false;

    public void OnFire(InputAction.CallbackContext context)
    {
        if (!IsOwner) return;
        if (!isDetectingInput) return;

        // Press
        if (context.performed)
        {
            Debug.Log($"OnFire triggered at {Time.time}");

            // Vector3 dir = playerViewCam.transform.rotation
            Ray caster = new Ray(playerViewCam.transform.position, playerViewCam.transform.forward);

            // Grave check (resolve by component, not tag)
            if (Physics.Raycast(playerViewCam.transform.position, playerViewCam.transform.forward, out RaycastHit hitInfo, reachDistance, catMask))
            {
                Debug.Log($"[Interactor] Hit collider: {hitInfo.collider.name}; Root: {hitInfo.collider.transform.root.name}");
                var grave = hitInfo.collider.GetComponentInParent < GraveBehaviour > ();
                if (grave != null)
                {
                    Debug.Log($"[Interactor] Resolved GraveBehaviour on {((Component)grave).gameObject.name}");
                    // should do the grave behaviour if it hit the grave, color shot else
                    grave.AddColorRpc(hotbarSlots[currentSelection].GetComponent < HotbarSlot > ().associatedColour);
                    return;
                }
            }

            if (Physics.Raycast(playerViewCam.transform.position, playerViewCam.transform.forward,
                    out RaycastHit symbolInfo, reachDistance, symbolMask))
            {
                SymbolBehaviour symbol = symbolInfo.collider.GetComponent<SymbolBehaviour>();
                if (symbol)
                {
                    //no duplicates
                    if (hitSymbols.Contains(symbol.SymbolID)) return;
                    
                    symbol.HandleInteract();
                    hitSymbols.Add(symbol.SymbolID);
                    hitSymbolObjects.Add(symbol);
                    if (hitSymbols.Count >= 4)
                    {
                        symbolPuzzleHandler.ValidateChoiceRpc(hitSymbols.ToArray());
                    }
                    
                }
            }

            // wound check P1 holds to keep wound open, release to close
            if (Physics.Raycast(playerViewCam.transform.position, playerViewCam.transform.forward, out RaycastHit woundHit, reachDistance, woundMask))
            {
                Debug.Log($"[Interactor] Hit collider: {woundHit.collider.name}; Root: {woundHit.collider.transform.root.name}");
                var wound = woundHit.collider.GetComponentInParent<WoundBehaviour>();
                if (wound != null)
                {
                    Debug.Log($"[Interactor] Resolved WoundBehaviour on {((Component)wound).gameObject.name}");
                    // Only Player 1 (host) can open wounds
                    if (NetworkManager.Singleton.LocalClientId != 0)
                        return;

                    WoundBehaviour woundBehaviour = woundHit.collider.GetComponentInParent<WoundBehaviour>();
                    woundBehaviour.RequestOpenWoundRpc();
                    isHoldingWound = true;
                    return;
                }
            }

            // candle slot check P1 taps to input sequence, P2 taps to swap melting candles
            if (Physics.Raycast(playerViewCam.transform.position, playerViewCam.transform.forward, out RaycastHit candleHit, reachDistance, candleMask))
            {
                Debug.Log($"[Interactor] Hit collider: {candleHit.collider.name}; Root: {candleHit.collider.transform.root.name}");
                // Resolve the CandleSlot component from the hit collider's parent chain
                var slot = candleHit.collider.GetComponentInParent<CandleSlot>();
                if (slot != null)
                {
                    Debug.Log($"[Interactor] Resolved CandleSlot on {((Component)slot).gameObject.name}");
                    // Call the slot's interact method and pass the hit for context
                    slot.OnPlayerInteract(candleHit);
                    return;
                }
            }

            // If nothing interactable was hit, fire the wand projectile with the selected color
            _wandController.FireWithColor(wandPos.position, playerViewCam.transform.rotation, hotbarSlots[currentSelection].GetComponent < HotbarSlot > ().slotColor);
        }

        // close wound if we were holding it open
        if (context.canceled && isHoldingWound)
        {
            isHoldingWound = false;
            WoundBehaviour wound = FindObjectOfType<WoundBehaviour>();
            if (wound != null)
                wound.RequestCloseWoundRpc();
        }
    }

    #endregion

    #region Helper Methods

    public void ClearSymbolChoices()
    {
        hitSymbols.Clear();
        foreach (var sBehaviour in hitSymbolObjects)
        {
            sBehaviour.ResetSymbol();
        }
        hitSymbolObjects.Clear();
    }

    #endregion
}