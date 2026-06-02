using Unity.Netcode;
using UnityEngine;

[DisallowMultipleComponent]
public class CandleSlot : NetworkBehaviour
{
    [SerializeField] private CandleBehaviour _candleBehaviour;
    [SerializeField] private int slotIndex;

    // called by local player interactor after resolving this CandleSlot
    // accepts RaycastHit so the interactor can pass collider info for debugging or context
    public void OnPlayerInteract(RaycastHit hit)
    {
        // player 1 should input the sequence, not player2
        if (!IsPlayerOne()) return;

        if (_candleBehaviour != null && !_candleBehaviour.IsReadyForInput)
        {
            Debug.Log($"[CandleSlot {slotIndex}] Input ignored — puzzle not ready yet.");
            return;
        }

        InteractionHandler handler = GetLocalInteractionHandler();
        if (handler == null)
        {
            Debug.LogWarning($"[CandleSlot {slotIndex}] Interact blocked: no local InteractionHandler found.");
            return;
        }

        // get the currently selected color from the player's hotbar
        var hotbar = handler.hotbarSlots;
        if (hotbar == null || hotbar.Count == 0)
        {
            Debug.LogWarning($"[CandleSlot {slotIndex}] Interact blocked: hotbar empty.");
            return;
        }

        var selected = handler.CurrentSelection;
        var hotbarSlot = hotbar[selected]?.GetComponent < HotbarSlot > ();
        if (hotbarSlot == null)
        {
            Debug.LogWarning($"[CandleSlot {slotIndex}] Interact blocked: selected hotbar slot missing HotbarSlot.");
            return;
        }

        InteractionHandler.Color selectedColor = hotbarSlot.associatedColour;

        // debug: show which collider was hit and which root object owns this CandleSlot
        Debug.Log($"[CandleSlot {slotIndex}] Sending input — slot: {slotIndex}, color: {selectedColor}; hitCollider={hit.collider.name}; root={hit.collider.transform.root.name}");

        if (_candleBehaviour == null)
        {
            Debug.LogError($"[CandleSlot {slotIndex}] No CandleBehaviour assigned. Interaction ignored.");
            return;
        }

        // forward to the networked CandleBehaviour RPC (keeps Netcode logic inside NetworkBehaviour)
        _candleBehaviour.InputCandleRpc(slotIndex, selectedColor);
    }

    // find the local player's InteractionHandler via Netcode's LocalClient PlayerObject
    private InteractionHandler GetLocalInteractionHandler()
    {
        if (NetworkManager.Singleton?.LocalClient?.PlayerObject == null)
            return null;

        return NetworkManager.Singleton.LocalClient.PlayerObject.GetComponent < InteractionHandler > ();
    }

    // helper checks for player roles by client id
    private bool IsPlayerOne() => NetworkManager.Singleton.LocalClientId == 0;
}