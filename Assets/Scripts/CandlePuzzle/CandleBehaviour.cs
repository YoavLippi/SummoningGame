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

    // parallel array of particle systems mapped 1:1 by slot index.
    // these live as child objects under each candle and mirror the light color/activation.
    // if left empty in the inspector, Start() will auto-fill them from candleObjects children.
    [Header("Candle Particles")]
    [SerializeField] private ParticleSystem[] candleParticles;

    // local flags tracking whether a sequence is in progress, input is allowed, and current round number
    [Header("Runtime Flags")]
    [SerializeField] private bool sequenceActive = false;
    [SerializeField] private bool canInput = true;
    [SerializeField] private int currentRound = 0;

    // failure tracking for 3-strike lose condition
    [Header("Failure Tracking")]
    [SerializeField] private int failureCount = 0;
    [SerializeField] private int maxFailures = 3;
    [SerializeField] private bool puzzleFailed = false;

    // public getter that returns true when the controller is ready and input is not locked
    public bool IsReadyForInput => _candleController != null && _candleController.IsRoundReady && canInput && !puzzleFailed;

    // finds the candle controller on start if it wasn't assigned in the inspector
    private void Start()
    {
        Debug.Log($"[Candles] Start called. canInput={canInput}, sequenceActive={sequenceActive}, currentRound={currentRound}, failures={failureCount}");

        if (_candleController == null)
        {
            var ctrlObj = GameObject.FindWithTag("GameController");
            if (ctrlObj != null)
                _candleController = ctrlObj.GetComponent<CandleController>();
        }

        // auto-fill particles from children if the array is empty but candleObjects are assigned.
        // this matches the original pattern of finding references at runtime to reduce inspector setup.
        if ((candleParticles == null || candleParticles.Length == 0) && candleObjects != null && candleObjects.Length > 0)
        {
            candleParticles = new ParticleSystem[candleObjects.Length];
            for (int i = 0; i < candleObjects.Length; i++)
            {
                if (candleObjects[i] != null)
                    candleParticles[i] = candleObjects[i].GetComponentInChildren<ParticleSystem>(true);
            }
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

        // authoritative rebuild; by the time this fires, litColors is already in sync
        // because the server always adds to litColors BEFORE inputtedSequence.
        UpdateAllCandles();
    }

    // callback when the lit colors network list changes, syncs debug view ONLY
    private void OnLitColorsChanged(NetworkListEvent<int> changeEvent)
    {
        debugColorViewer.Clear();
        foreach (var val in litColors)
            debugColorViewer.Add(val);

        // 
        // don't rebuild visuals here. litColors is updated BEFORE inputtedSequence on the server, rebuilding here would run with a complete litColors array but an empty/stale
    }

    // returns true if a candle sequence is currently being attempted
    public bool IsSequenceLocked() => sequenceActive;

    // server rpc that clears all progress and resets the puzzle state for a new attempt
    [Rpc(SendTo.Server, InvokePermission = RpcInvokePermission.Everyone)]
    public void NullSequenceRpc()
    {
        if (!IsServer) return;

        // don't reset if puzzle is permanently failed
        if (puzzleFailed) return;

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
        if (puzzleFailed)
        {
            Debug.LogWarning("[CandleBehaviour] Input rejected — puzzle already failed.");
            return;
        }

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

            // Instant visual feedback — snaps the light on immediately so the player sees
            // the result without waiting for the NetworkList sync roundtrip.
            // The authoritative rebuild from OnSequenceChanged will reconcile it a frame later.
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
        failureCount++;
        Debug.Log($"[Candles] Failure {failureCount}/{maxFailures}");

        // Visual feedback: flash all active candles red briefly
        FlashAllCandlesClientRpc(false);

        yield return new WaitForSeconds(0.5f);

        if (failureCount >= maxFailures)
        {
            TriggerLoseCondition();
        }
        else
        {
            NullSequenceRpc();
        }
    }

    // handles the lose state when max failures are reached
    private void TriggerLoseCondition()
    {
        puzzleFailed = true;
        canInput = false;
        sequenceActive = false;

        Debug.Log("[Candles] Max failures reached. Puzzle failed!");

        // Unlock cursor so player can navigate menus
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Trigger lose cutscene via SessionCutscenes (same pattern as GraveBehaviour)
        SessionCutscenes cutscenes = FindObjectOfType<SessionCutscenes>();
        if (cutscenes != null)
        {
            cutscenes.TriggerLoseClientRpc();
        }
        else
        {
            Debug.LogWarning("[Candles] No SessionCutscenes found in scene!");
        }

        // Cleanup and despawn (same as GraveBehaviour)
        CleanupPlayersClientRpc();
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
    /// local visual update. Runs on every client via OnSequenceChanged when the NetworkList syncs.
    /// Lights are applied to the EXACT slot indices stored in inputtedSequence, using litColors as
    /// a parallel array. This mirrors GraveBehaviour's UpdateAllRunes, but accounts for the fact
    /// that candles are positional (slot-based) rather than sequential (rune-based).
    /// </summary>
    // updates candle object visibility and light states based on current round progress
    private void UpdateAllCandles()
    {
        int count = _candleController != null ? _candleController.GetStepCount(currentRound) : 0;

        // step 1: Set active state and disable all lights (clean slate)
        for (int i = 0; i < candleObjects.Length; i++)
        {
            bool active = i < count;
            if (candleObjects[i] != null)
                candleObjects[i].SetActive(active);

            if (candleLights[i] != null)
                candleLights[i].enabled = false;

            // stop and clear particles for any slot that isn't active or hasn't been inputted yet.
            // this ensures particles don't linger from a previous round or failed attempt.
            if (i < candleParticles.Length && candleParticles[i] != null)
                candleParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }

        // step 2: Light up only the specific slots that have been inputted.
        // inputtedSequence[i] = the slot index activated at step i
        // litColors[i]         = the color used at step i
        for (int i = 0; i < inputtedSequence.Count; i++)
        {
            int slotIndex = inputtedSequence[i];

            if (slotIndex >= candleLights.Length || candleLights[slotIndex] == null)
                continue;

            if (i < litColors.Count)
            {
                Color c = GetUnityColor((InteractionHandler.Color)litColors[i]);

                candleLights[slotIndex].enabled = true;
                candleLights[slotIndex].color = c;
                candleLights[slotIndex].intensity = 3f;

                // play the particle effect on the matched slot with the same color as the light.
                // this gives the player immediate visual feedback that their input registered.
                if (slotIndex < candleParticles.Length && candleParticles[slotIndex] != null)
                {
                    SetParticleColor(candleParticles[slotIndex], c);
                    candleParticles[slotIndex].Play();
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

    // helper that sets a particle system's start color to match the candle light color.
    // this mirrors GraveBehaviour's absorptionEffect color matching pattern.
    private void SetParticleColor(ParticleSystem ps, Color color)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startColor = color;
    }

    // client rpc that immediately lights up a specific candle with the shot color
    [ClientRpc]
    private void SetCandleColorClientRpc(int slotIndex, InteractionHandler.Color shotColor)
    {
        if (slotIndex >= candleLights.Length || candleLights[slotIndex] == null) return;

        Color c = GetUnityColor(shotColor);

        candleLights[slotIndex].enabled = true;
        candleLights[slotIndex].color = c;
        candleLights[slotIndex].intensity = 3f;

        // snap the particle effect on instantly alongside the light.
        // this avoids the "dark frame" delay from NetworkList sync and gives immediate feedback.
        if (slotIndex < candleParticles.Length && candleParticles[slotIndex] != null)
        {
            SetParticleColor(candleParticles[slotIndex], c);
            candleParticles[slotIndex].Play();
        }
    }

    // client rpc that resets all candles to the correct count for a new round and disables lights
    [ClientRpc]
    private void ResetAllCandlesClientRpc(int stepCount)
    {
        int objCount = candleObjects != null ? candleObjects.Length : 0;
        int lightCount = candleLights != null ? candleLights.Length : 0;
        int particleCount = candleParticles != null ? candleParticles.Length : 0;
        int maxIter = Mathf.Max(objCount, lightCount, particleCount);

        for (int i = 0; i < maxIter; i++)
        {
            bool active = i < stepCount;

            if (i < objCount && candleObjects[i] != null)
                candleObjects[i].SetActive(active);

            if (i < lightCount && candleLights[i] != null)
                candleLights[i].enabled = false;

            // NEW: stop and clear all particles on reset so nothing lingers between rounds.
            if (i < particleCount && candleParticles[i] != null)
                candleParticles[i].Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    // client rpc that brightens all candles in the completed round to show success
    [ClientRpc]
    private void LightUpRoundClientRpc(int roundIndex)
    {
        // light up the exact slots that were used in this round, preserving their step colors
        for (int i = 0; i < inputtedSequence.Count; i++)
        {
            int slotIndex = inputtedSequence[i];
            if (slotIndex >= candleLights.Length || candleLights[slotIndex] == null)
                continue;

            if (i < litColors.Count)
            {
                Color c = GetUnityColor((InteractionHandler.Color)litColors[i]);

                candleLights[slotIndex].enabled = true;
                candleLights[slotIndex].color = c;
                candleLights[slotIndex].intensity = 4f;

                // replay particles on round complete to emphasize the "locked in" success state.
                if (slotIndex < candleParticles.Length && candleParticles[slotIndex] != null)
                {
                    SetParticleColor(candleParticles[slotIndex], c);
                    candleParticles[slotIndex].Play();
                }
            }
        }

        Debug.Log($"[Candles] Round {roundIndex + 1} row locked in.");
    }

    // client rpc that fires when all rounds are successfully completed
    [ClientRpc]
    private void PuzzleCompleteClientRpc()
    {
        Debug.Log("[Candles] All 3 rounds complete. Puzzle solved!");

        // Unlock cursor so player can navigate menus
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Trigger win cutscene via SessionCutscenes (same pattern as GraveBehaviour)
        SessionCutscenes cutscenes = FindObjectOfType<SessionCutscenes>();
        if (cutscenes != null)
        {
            cutscenes.TriggerWinClientRpc();
        }
        else
        {
            Debug.LogWarning("[Candles] No SessionCutscenes found in scene!");
        }

        // Optional: Despawn player objects to clean up before scene transition
        // (Only if you want the same cleanup flow as GraveBehaviour)
        if (NetworkManager.Singleton?.LocalClient?.PlayerObject != null)
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.Despawn(true);
        }
    }

    // client rpc that flashes all active candles with success/fail color
    [ClientRpc]
    private void FlashAllCandlesClientRpc(bool success)
    {
        UnityEngine.Color targetColor = success ? Color.green : Color.red;

        int activeCount = _candleController != null ? _candleController.GetStepCount(currentRound) : candleObjects.Length;

        for (int i = 0; i < activeCount && i < candleLights.Length; i++)
        {
            if (candleLights[i] != null)
            {
                candleLights[i].enabled = true;
                candleLights[i].color = targetColor;
                candleLights[i].intensity = 5f;
            }

            if (i < candleParticles.Length && candleParticles[i] != null)
            {
                SetParticleColor(candleParticles[i], targetColor);
                candleParticles[i].Play();
            }
        }
    }

    // client rpc: cleanup pattern from GraveBehaviour
    [ClientRpc]
    public void CleanupPlayersClientRpc()
    {
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (NetworkManager.Singleton?.LocalClient?.PlayerObject != null)
        {
            NetworkManager.Singleton.LocalClient.PlayerObject.Despawn(true);
        }
    }
}