using System.Collections.Generic;
using Unity.Netcode;
using UnityEngine;

// network behaviour that acts as the puzzle master, defining candle sequences and managing round progression
public class CandleController : NetworkBehaviour
{
    // data class representing a single step in a candle sequence (which slot and what color)
    [System.Serializable]
    public class CandleStep
    {
        public int requiredSlotIndex;
        public InteractionHandler.Color requiredColor;
    }

    // data class holding the full ordered sequence for one round
    [System.Serializable]
    public class RoundSolution
    {
        public List<CandleStep> sequence = new List<CandleStep>();
    }

    // inspector-configured solutions for all rounds of the puzzle
    [Header("Solutions (set in Inspector)")]
    [SerializeField] private RoundSolution[] rounds = new RoundSolution[0];

    // references to the wound and candle behaviours for coordination
    [Header("References")]
    [SerializeField] private CandleBehaviour _candleBehaviour;
    [SerializeField] private WoundBehaviour _woundBehaviour;

    // current round index tracked at runtime
    [Header("Runtime")]
    [SerializeField] private int currentRound = 0;

    // public getters for total rounds and current round index
    public int TotalRounds => rounds.Length;
    public int CurrentRound => currentRound;

    // returns true when the wound is ready to show a new sequence (or if no wound is linked)
    public bool IsRoundReady => _woundBehaviour == null || _woundBehaviour.IsSequenceReady;

    // finds the wound and candle behaviours on spawn if they weren't assigned in the inspector
    public override void OnNetworkSpawn()
    {
        if (_candleBehaviour == null)
            _candleBehaviour = FindObjectOfType<CandleBehaviour>();
        if (_woundBehaviour == null)
            _woundBehaviour = FindObjectOfType<WoundBehaviour>();
    }

    // server starts the first round automatically when the scene begins
    private void Start()
    {
        if (!IsServer) return;
        StartRound(0);
    }

    // returns a specific step from a specific round, or null if out of bounds
    public CandleStep GetStep(int round, int index)
    {
        if (round >= rounds.Length) return null;
        if (index >= rounds[round].sequence.Count) return null;
        return rounds[round].sequence[index];
    }

    // returns how many steps are in a given round
    public int GetStepCount(int round)
    {
        if (round >= rounds.Length) return 0;
        return rounds[round].sequence.Count;
    }

    // convenience overload that returns the step count for the current round
    public int GetStepCount() => GetStepCount(currentRound);

    // returns the full solution list for the current round
    public List<CandleStep> GetCurrentSolution()
    {
        if (currentRound >= rounds.Length)
        {
            Debug.LogWarning("[CandleController] GetCurrentSolution called past final round.");
            return new List<CandleStep>();
        }
        return rounds[currentRound].sequence;
    }

    // initializes a new round: updates the wound display, signals the new sequence, and sets the ready flag
    public void StartRound(int roundIndex)
    {
        if (roundIndex >= rounds.Length)
        {
            Debug.LogWarning("[CandleController] StartRound called past final round.");
            return;
        }

        currentRound = roundIndex;

        _woundBehaviour?.SetSequenceReady(false);

        if (_woundBehaviour != null)
        {
            _woundBehaviour.SignalNewSequenceClientRpc();

            // build parallel arrays of slot indices and colors to send to player two
            var slotSequence = new int[rounds[roundIndex].sequence.Count];
            var colorSequence = new int[rounds[roundIndex].sequence.Count];

            for (int i = 0; i < rounds[roundIndex].sequence.Count; i++)
            {
                slotSequence[i] = rounds[roundIndex].sequence[i].requiredSlotIndex;
                colorSequence[i] = (int)rounds[roundIndex].sequence[i].requiredColor;
            }

            _woundBehaviour.DisplaySequenceClientRpc(slotSequence, colorSequence);
        }

        _woundBehaviour?.SetSequenceReady(true);

        Debug.Log($"[CandleController] Round {roundIndex + 1} started. " +
                  $"Sequence length: {rounds[roundIndex].sequence.Count}");
    }

#if UNITY_EDITOR
    // validates that each round gets progressively longer
    private void OnValidate()
    {
        if (rounds == null)
        {
            rounds = new RoundSolution[0];
            return;
        }

        // ensure no null entries
        for (int i = 0; i < rounds.Length; i++)
        {
            if (rounds[i] == null)
                rounds[i] = new RoundSolution();
        }

        // warn if sequences don't get progressively longer
        for (int i = 1; i < rounds.Length; i++)
        {
            if (rounds[i].sequence.Count <= rounds[i - 1].sequence.Count)
            {
                Debug.LogWarning($"[CandleController] Round {i + 1} sequence should be longer than round {i}.");
            }
        }
    }
#endif
}