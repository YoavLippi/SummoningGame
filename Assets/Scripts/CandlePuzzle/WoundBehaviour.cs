using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;
using UnityEngine.UI;

// network behaviour that manages the wound state, visuals, and candle sequence interactions
public class WoundBehaviour : NetworkBehaviour
{
    // reference to the candle behaviour for sequence locking and nulling
    [Header("References")]
    [SerializeField] private CandleBehaviour _candleBehaviour;

    // optional wound visuals and blood splatter flash settings
    [Header("Wound Visuals, optional")]
    [SerializeField] private GameObject woundObject;
    [SerializeField] private Renderer[] bloodSplatterObjects;
    [SerializeField] private float flashInterval = 0.15f;
    [SerializeField] private int flashCount = 5;

    // popup panel that only appears for player two
    [Header("Popup Panel (P2 Only)")]
    [SerializeField] private GameObject popupPanel;

    // sequence indicator objects that only player two can see
    [Header("Sequence Display (P2 Only)")]
    [SerializeField] private GameObject[] sequenceIndicators;
    // [SerializeField] private UnityEngine.Color[] symbolColors;

    // networked runtime state tracking whether the wound is open and if a sequence is ready
    [Header("Runtime")]
    [SerializeField]
    private NetworkVariable<bool> woundOpen =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
                                        NetworkVariableWritePermission.Server);

    // networked flag indicating whether a new candle sequence is available
    [SerializeField]
    private NetworkVariable<bool> sequenceReady =
        new NetworkVariable<bool>(false, NetworkVariableReadPermission.Everyone,
                                        NetworkVariableWritePermission.Server);

    // locally stored current candle sequence for display purposes
    private int[] currentSequence;

    // public getter for the sequence ready state
    public bool IsSequenceReady => sequenceReady.Value;

    // server-only setter for the sequence ready flag
    public void SetSequenceReady(bool ready)
    {
        if (!IsServer) return;
        sequenceReady.Value = ready;
    }

    // called when the network object spawns, sets up listeners and initial state
    public override void OnNetworkSpawn()
    {
        if (_candleBehaviour == null)
            _candleBehaviour = FindObjectOfType<CandleBehaviour>();

        woundOpen.OnValueChanged += OnWoundOpenChanged;
        sequenceReady.OnValueChanged += OnSequenceReadyChanged;

        OnWoundOpenChanged(false, woundOpen.Value);
    }

    // called when the network object despawns, cleans up event listeners
    public override void OnNetworkDespawn()
    {
        woundOpen.OnValueChanged -= OnWoundOpenChanged;
        sequenceReady.OnValueChanged -= OnSequenceReadyChanged;
    }

    // rpc that allows any client to request opening the wound, but only server processes it and only allows player one
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestOpenWoundRpc(RpcParams rpcParams = default)
    {
        if (!IsServer) return;

        ulong senderId = rpcParams.Receive.SenderClientId;
        if (senderId != 0)
        {
            Debug.Log($"[Wound] Rejected open request from client {senderId}");
            return;
        }

        // if the candle sequence is currently locked, null the round and flash blood instead of opening
        if (_candleBehaviour != null && _candleBehaviour.IsSequenceLocked())
        {
            Debug.Log("[Wound] Opened mid-sequence — nulling round.");
            _candleBehaviour.NullSequenceRpc();
            StartCoroutine(FlashSplatter());
            return;
        }

        woundOpen.Value = true;
        Debug.Log("[Wound] Wound opened by P1.");
    }

    // rpc that allows any client to request closing the wound, server only
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void RequestCloseWoundRpc()
    {
        if (!IsServer) return;
        woundOpen.Value = false;
        Debug.Log("[Wound] Wound closed.");
    }

    // client rpc that notifies all clients a new sequence is available and triggers blood splatter
    [ClientRpc]
    public void SignalNewSequenceClientRpc()
    {
        StartCoroutine(FlashSplatter());
        Debug.Log("[Wound] New sequence available — blood splatter triggered.");
    }

    // callback when the wound open state changes, updates wound object and player two ui visibility
    private void OnWoundOpenChanged(bool previous, bool current)
    {
        if (woundObject != null)
            woundObject.SetActive(current);

        bool isPlayerTwo = IsLocalPlayerTwo();

        if (popupPanel != null)
            popupPanel.SetActive(current && isPlayerTwo);

        // toggle sequence indicators based on wound state, player identity, and sequence length
        if (sequenceIndicators != null)
        {
            int count = currentSequence != null ? currentSequence.Length : 0;

            for (int i = 0; i < sequenceIndicators.Length; i++)
            {
                if (sequenceIndicators[i] == null) continue;

                bool inRange = i < count;
                sequenceIndicators[i].SetActive(current && isPlayerTwo && inRange);
            }
        }
    }

    // callback when sequence ready state changes, triggers blood flash on true
    private void OnSequenceReadyChanged(bool previous, bool current)
    {
        if (current)
            StartCoroutine(FlashSplatter());
    }

    // client rpc that sends the candle sequence and colors to player two for display
    [ClientRpc]
    public void DisplaySequenceClientRpc(int[] sequence, int[] colors)
    {
        if (!IsLocalPlayerTwo()) return;

        currentSequence = sequence;

        // step 1: hide all indicators first
        for (int i = 0; i < sequenceIndicators.Length; i++)
        {
            if (sequenceIndicators[i] != null)
                sequenceIndicators[i].SetActive(false);
        }

        // step 2: activate and color indicators in SEQUENCE ORDER
        for (int step = 0; step < sequence.Length && step < colors.Length; step++)
        {
            if (step >= sequenceIndicators.Length || sequenceIndicators[step] == null)
                continue;

            int slotIndex = sequence[step];
            UnityEngine.Color displayColor = GetColorFromEnum((InteractionHandler.Color)colors[step]);

            // apply color to whichever component exists
            Renderer r = sequenceIndicators[step].GetComponent<Renderer>();
            if (r != null) r.material.color = displayColor;

            Light l = sequenceIndicators[step].GetComponent<Light>();
            if (l != null) l.color = displayColor;

            UnityEngine.UI.Image img = sequenceIndicators[step].GetComponent<UnityEngine.UI.Image>();
            if (img != null) img.color = displayColor;

            SpriteRenderer sr = sequenceIndicators[step].GetComponent<SpriteRenderer>();
            if (sr != null) sr.color = displayColor;

            // show the slot index number as text
            UpdateIndicatorText(sequenceIndicators[step], slotIndex);

            sequenceIndicators[step].SetActive(true);
        }

        OnWoundOpenChanged(woundOpen.Value, woundOpen.Value);
    }

    // helper to update text on an indicator showing the slot number
    private void UpdateIndicatorText(GameObject indicator, int slotIndex)
    {
        var tmp = indicator.GetComponentInChildren<TMPro.TextMeshProUGUI>(true);
        if (tmp != null)
        {
            tmp.text = (slotIndex + 1).ToString();  // +1 for human-readable numbering
            tmp.gameObject.SetActive(true);
            return;
        }

        var tmpWorld = indicator.GetComponentInChildren<TMPro.TextMeshPro>(true);
        if (tmpWorld != null)
        {
            tmpWorld.text = (slotIndex + 1).ToString();  // +1 for player readability
            tmpWorld.gameObject.SetActive(true);
            return;
        }

        var legacyText = indicator.GetComponentInChildren < UnityEngine.UI.Text > (true);
        if (legacyText != null)
        {
            legacyText.text = (slotIndex + 1).ToString();  // +1 for player readability 
            legacyText.gameObject.SetActive(true);
            return;
        }
    }



    // helper that converts the interaction handler color enum to a unity color
    private UnityEngine.Color GetColorFromEnum(InteractionHandler.Color color)
    {
        switch (color)
        {
            case InteractionHandler.Color.Red: return UnityEngine.Color.red;
            case InteractionHandler.Color.Blue: return UnityEngine.Color.blue;
            case InteractionHandler.Color.Yellow: return UnityEngine.Color.yellow;
            case InteractionHandler.Color.Green: return UnityEngine.Color.green;
            case InteractionHandler.Color.Orange: return new UnityEngine.Color(1f, 0.5f, 0f);
            case InteractionHandler.Color.Purple: return new UnityEngine.Color(0.6f, 0.2f, 0.8f);
            case InteractionHandler.Color.Pink: return new UnityEngine.Color(1f, 0.4f, 0.7f);
            case InteractionHandler.Color.White: return UnityEngine.Color.white;
            default: return UnityEngine.Color.white;
        }
    }

    // coroutine that flashes all blood splatter renderers between red and their original color
    private IEnumerator FlashSplatter()
    {
        if (bloodSplatterObjects == null || bloodSplatterObjects.Length == 0)
            yield break;

        UnityEngine.Color[] origColors = new UnityEngine.Color[bloodSplatterObjects.Length];
        for (int i = 0; i < bloodSplatterObjects.Length; i++)
            if (bloodSplatterObjects[i] != null)
                origColors[i] = bloodSplatterObjects[i].material.color;

        for (int f = 0; f < flashCount; f++)
        {
            foreach (var r in bloodSplatterObjects)
                if (r != null) r.material.color = UnityEngine.Color.red;

            yield return new WaitForSeconds(flashInterval);

            for (int i = 0; i < bloodSplatterObjects.Length; i++)
                if (bloodSplatterObjects[i] != null)
                    bloodSplatterObjects[i].material.color = origColors[i];

            yield return new WaitForSeconds(flashInterval);
        }
    }

    // returns true if the local client is player two (client id 1)
    private bool IsLocalPlayerTwo()
    {
        return NetworkManager.Singleton.LocalClientId == 1;
    }
}