using System.Collections;
using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// network behaviour that handles the candle puzzle logic, input validation, and visual feedback
public class CandleBehaviour : NetworkBehaviour
{
    // networked lists that store the player's input sequence and the colors they used
    [Header("Sequence State")]
    [SerializeField] private NetworkList<int> inputtedSequence = new NetworkList<int>();
    [SerializeField] private NetworkList<int> litColors = new NetworkList<int>();
    [SerializeField] private List<int> debugSequenceViewer;
    [SerializeField] private List<int> debugColorViewer;

    // reference to the candle controller that defines rounds and solutions
    [Header("Controllers")]
    [SerializeField] private CandleController _candleController;

    // arrays of candle gameobjects and their point lights for visual feedback
    [Header("Candle Visuals")]
    [SerializeField] private GameObject[] candleObjects;
    [SerializeField] private Light[] candleLights;

    // local flags tracking whether a sequence is in progress, input is allowed, and current round number
    [Header("Runtime Flags")]
    [SerializeField] private bool sequenceActive = false;
    [SerializeField] private bool canInput = true;
    [SerializeField] private int currentRound = 0;

    // public getter that returns true when the controller is ready and input is not locked
    public bool IsReadyForInput => _candleController != null && _candleController.IsRoundReady && canInput;

    // finds the candle controller on start if it wasn't assigned in the inspector
    private void Start()
    {
        Debug.Log($"[Candles] Start called. canInput={canInput}, sequenceActive={sequenceActive}, currentRound={currentRound}");

        if (_candleController == null)
        {
            var ctrlObj = GameObject.FindWithTag("CandleController");
            if (ctrlObj != null)
                _candleController = ctrlObj.GetComponent<CandleController>();
        }
    }

    // initializes debug lists and subscribes to network list changes when the object spawns
    public override void OnNetworkSpawn()
    {
        debugSequenceViewer = new List<int>();
        debugColorViewer = new List<int>();
        inputtedSequence.OnListChanged += OnSequenceChanged;
        litColors.OnListChanged += OnLitColorsChanged;

        if (_candleController != null)
            UpdateAllCandles();
    }

    // unsubscribes from network list events when the object despawns
    public override void OnNetworkDespawn()
    {
        inputtedSequence.OnListChanged -= OnSequenceChanged;
        litColors.OnListChanged -= OnLitColorsChanged;
    }

    // callback when the inputted sequence network list changes, syncs debug view and updates visuals
    private void OnSequenceChanged(NetworkListEvent<int> changeEvent)
    {
        debugSequenceViewer.Clear();
        foreach (var val in inputtedSequence)
            debugSequenceViewer.Add(val);

        UpdateAllCandles();
    }

    // callback when the lit colors network list changes, syncs debug view and updates visuals
    private void OnLitColorsChanged(NetworkListEvent<int> changeEvent)
    {
        debugColorViewer.Clear();
        foreach (var val in litColors)
            debugColorViewer.Add(val);

        UpdateAllCandles();
    }

    // returns true if a candle sequence is currently being attempted
    public bool IsSequenceLocked() => sequenceActive;

    // server rpc that clears all progress and resets the puzzle state for a new attempt
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void NullSequenceRpc()
    {
        if (!IsServer) return;

        litColors.Clear();
        inputtedSequence.Clear();
        sequenceActive = false;
        canInput = true;

        int stepCount = _candleController != null ? _candleController.GetStepCount(currentRound) : 0;
        ResetAllCandlesClientRpc(stepCount);

        Debug.Log("[Candles] Sequence nulled — round reset.");
    }

    // main server rpc that validates player input against the current round's solution step
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void InputCandleRpc(int slotIndex, InteractionHandler.Color shotColor)
    {
        if (_candleController == null)
        {
            Debug.LogWarning($"[CandleBehaviour] Input rejected — no CandleController. slot={slotIndex}, color={shotColor}");
            return;
        }

        if (!_candleController.IsRoundReady)
        {
            Debug.LogWarning($"[CandleBehaviour] Input rejected — round not ready. slot={slotIndex}, color={shotColor}");
            return;
        }

        if (!canInput)
        {
            Debug.LogWarning($"[CandleBehaviour] Input rejected — input locked. slot={slotIndex}, color={shotColor}");
            return;
        }

        Debug.Log($"[CandleBehaviour] RPC received — slot: {slotIndex}, color: {shotColor}, canInput: {canInput}");

        if (!sequenceActive)
        {
            sequenceActive = true;
        }

        var step = _candleController.GetStep(currentRound, inputtedSequence.Count);
        if (step == null)
        {
            Debug.LogWarning($"[CandleBehaviour] No step defined for round {currentRound}, index {inputtedSequence.Count}");
            return;
        }

        bool slotCorrect = slotIndex == step.requiredSlotIndex;
        bool colorCorrect = shotColor == step.requiredColor;

        Debug.Log($"[Candle] Input slot={slotIndex}, color={shotColor} | " +
                  $"Expected slot={step.requiredSlotIndex}, color={step.requiredColor}");

        if (slotCorrect && colorCorrect)
        {
            litColors.Add((int)shotColor);
            inputtedSequence.Add(slotIndex);

            SetCandleColorClientRpc(slotIndex, shotColor);

            if (inputtedSequence.Count >= _candleController.GetStepCount(currentRound))
            {
                canInput = false;
                StartCoroutine(EvaluateSequence());
            }
        }
        else
        {
            Debug.Log($"[Candles] Wrong input at step {inputtedSequence.Count}. " +
                      $"Expected slot {step.requiredSlotIndex}, got {slotIndex}. " +
                      $"Expected color {step.requiredColor}, got {shotColor}.");
            canInput = false;
            StartCoroutine(FailAndReset());
        }
    }

    // coroutine that waits briefly then resets the puzzle after an incorrect input
    private IEnumerator FailAndReset()
    {
        yield return new WaitForSeconds(0.5f);
        NullSequenceRpc();
    }

    // coroutine that evaluates the completed sequence and either advances the round or resets
    private IEnumerator EvaluateSequence()
    {
        yield return new WaitForSeconds(0.2f);

        bool correct = CheckSequence();
        Debug.Log(correct
            ? $"[Candles] Round {currentRound + 1} CORRECT."
            : $"[Candles] Round {currentRound + 1} WRONG.");

        if (correct)
        {
            sequenceActive = false;
            LightUpRoundClientRpc(currentRound);
            yield return new WaitForSeconds(1.5f);

            currentRound++;

            if (currentRound >= _candleController.TotalRounds)
            {
                PuzzleCompleteClientRpc();
            }
            else
            {
                _candleController.StartRound(currentRound);

                int stepCount = _candleController != null ? _candleController.GetStepCount(currentRound) : 0;
                ResetAllCandlesClientRpc(stepCount);

                litColors.Clear();
                inputtedSequence.Clear();
                canInput = true;
            }
        }
        else
        {
            yield return new WaitForSeconds(0.5f);
            NullSequenceRpc();
        }
    }

    // compares the inputted sequence against the controller's solution for the current round
    private bool CheckSequence()
    {
        var solution = _candleController.GetCurrentSolution();
        if (inputtedSequence.Count != solution.Count) return false;

        for (int i = 0; i < inputtedSequence.Count; i++)
        {
            if (inputtedSequence[i] != solution[i].requiredSlotIndex) return false;
        }

        return true;
    }

    /// <summary>
    /// local visual update. Runs on every client via OnListChanged when the NetworkList syncs.
    /// lights reflect progress (how many steps are done), not raw slot index.
    /// </summary>
    // updates candle object visibility and light states based on current round progress
    private void UpdateAllCandles()
    {
        int count = _candleController != null ? _candleController.GetStepCount(currentRound) : 0;

        for (int i = 0; i < candleObjects.Length; i++)
        {
            if (i >= count)
            {
                candleObjects[i].SetActive(false);
                continue;
            }

            candleObjects[i].SetActive(true);

            bool isLit = i < inputtedSequence.Count;

            if (candleLights[i] != null)
            {
                if (isLit && i < litColors.Count)
                {
                    candleLights[i].enabled = true;
                    candleLights[i].color = GetUnityColor((InteractionHandler.Color)litColors[i]);
                    candleLights[i].intensity = 3f;
                }
                else
                {
                    candleLights[i].enabled = false;
                }
            }
        }
    }

    // helper that converts the interaction handler color enum to a unity color
    private Color GetUnityColor(InteractionHandler.Color color)
    {
        switch (color)
        {
            case InteractionHandler.Color.Red: return Color.red;
            case InteractionHandler.Color.Blue: return Color.blue;
            case InteractionHandler.Color.Yellow: return Color.yellow;
            case InteractionHandler.Color.Green: return Color.green;
            case InteractionHandler.Color.Orange: return new Color(1f, 0.5f, 0f);
            case InteractionHandler.Color.Purple: return new Color(0.6f, 0.2f, 0.8f);
            case InteractionHandler.Color.Pink: return new Color(1f, 0.4f, 0.7f);
            case InteractionHandler.Color.White: return Color.white;
            default: return Color.white;
        }
    }

    // client rpc that immediately lights up a specific candle with the shot color
    [ClientRpc]
    private void SetCandleColorClientRpc(int slotIndex, InteractionHandler.Color shotColor)
    {
        if (slotIndex >= candleLights.Length) return;
        if (candleLights[slotIndex] == null) return;

        candleLights[slotIndex].enabled = true;
        candleLights[slotIndex].color = GetUnityColor(shotColor);
        candleLights[slotIndex].intensity = 3f;
    }

    // client rpc that resets all candles to the correct count for a new round and disables lights
    [ClientRpc]
    private void ResetAllCandlesClientRpc(int stepCount)
    {
        int objCount = candleObjects != null ? candleObjects.Length : 0;
        int lightCount = candleLights != null ? candleLights.Length : 0;
        int maxIter = Mathf.Max(objCount, lightCount);

        for (int i = 0; i < maxIter; i++)
        {
            bool active = i < stepCount;

            if (i < objCount && candleObjects[i] != null)
                candleObjects[i].SetActive(active);

            if (i < lightCount && candleLights[i] != null)
                candleLights[i].enabled = false;
        }
    }

    // client rpc that brightens all candles in the completed round to show success
    [ClientRpc]
    private void LightUpRoundClientRpc(int roundIndex)
    {
        for (int i = 0; i < inputtedSequence.Count; i++)
        {
            if (i >= candleLights.Length) break;
            if (candleLights[i] == null) continue;

            candleLights[i].enabled = true;

            if (i < litColors.Count)
                candleLights[i].color = GetUnityColor((InteractionHandler.Color)litColors[i]);

            candleLights[i].intensity = 4f;
        }

        Debug.Log($"[Candles] Round {roundIndex + 1} row locked in.");
    }

    // client rpc that fires when all rounds are successfully completed
    [ClientRpc]
    private void PuzzleCompleteClientRpc()
    {
        Debug.Log("[Candles] All 3 rounds complete. Puzzle solved!");
    }
}