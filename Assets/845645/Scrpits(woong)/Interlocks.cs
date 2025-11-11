using UnityEngine;
using System;
using System.Collections.Generic;
using System.Linq;

public class Interlocks : MonoBehaviour
{
    public static bool ConditionGateClosed { get; private set; } = false;
    public static bool ConditionHandsSafe { get; private set; } = false;
    public static bool ConditionOkConfirmed { get; private set; } = true;
    public static bool ConditionDualHand { get; private set; } = false;

    public static bool IsSafeToStartCycle { get; private set; } = false;
    public static bool IsSafeToContinueClosing { get; private set; } = false;

    public static event Action OnInterlockStatusChanged;

    private static Dictionary<string, PlayerSafetyData> playerStates = new();
    private static string lastHandIntrusionPlayer = null;

    public static string GetLastHandIntrusionPlayer() => lastHandIntrusionPlayer;

    private class PlayerSafetyData
    {
        public int GateStatus = 0;
        public int HandStatus = 0;
        public int OkStatus = 1;
        public int DualHandStatus = 0;
    }

    void Update() => CheckSafetyConditions();

    public static void UpdatePlayerState(string playerId, int gateStatus, int handStatus, int okStatus, int dualHandStatus)
    {
        if (!playerStates.TryGetValue(playerId, out var data))
        {
            data = new PlayerSafetyData();
            playerStates[playerId] = data;
        }

        bool handChanged = data.HandStatus != handStatus;
        data.GateStatus = gateStatus;
        data.HandStatus = handStatus;
        data.OkStatus = okStatus;
        data.DualHandStatus = dualHandStatus;

        if (handChanged && handStatus == 0)
            lastHandIntrusionPlayer = playerId;

        Instance?.CheckSafetyConditions();
    }

    private static Interlocks Instance;

    void Awake()
    {
        if (Instance == null) Instance = this;
    }

    private void CheckSafetyConditions()
    {
        if (playerStates.Count == 0)
        {
            ConditionGateClosed = true;
            ConditionHandsSafe = true;
            ConditionOkConfirmed = true;
            ConditionDualHand = false;
        }
        else
        {
            ConditionGateClosed = playerStates.Values.All(p => p.GateStatus == 1);
            ConditionHandsSafe = playerStates.Values.All(p => p.HandStatus == 1);
            ConditionOkConfirmed = playerStates.Values.All(p => p.OkStatus == 1);
            ConditionDualHand = playerStates.Values.Any(p => p.DualHandStatus == 1);
        }

        IsSafeToStartCycle = ConditionGateClosed && ConditionHandsSafe && ConditionDualHand;
        IsSafeToContinueClosing = ConditionGateClosed && ConditionHandsSafe;

        OnInterlockStatusChanged?.Invoke();
    }
}