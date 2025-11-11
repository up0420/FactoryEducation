using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class Interlocks : MonoBehaviour
{
    public static bool ConditionGateClosed { get; private set; }
    public static bool ConditionHandsSafe { get; private set; } = true;
    public static bool ConditionDualHand { get; private set; }
    public static bool IsSafeToStartCycle => ConditionGateClosed && ConditionHandsSafe && ConditionDualHand;
    public static bool IsSafeToContinueClosing => ConditionGateClosed && ConditionHandsSafe;

    public static event System.Action OnInterlockStatusChanged;

    private static Dictionary<string, PlayerData> players = new();
    private static string lastIntrusionPlayer;

    public static string GetLastHandIntrusionPlayer() => lastIntrusionPlayer;

    public class PlayerData
    {
        public int Gate = 1;
        public int Hand = 1;
        public int Dual = 0;
    }

    public static void UpdatePlayer(string id, int gate, int hand, int dual)
    {
        if (!players.ContainsKey(id)) players[id] = new PlayerData();
        var p = players[id];
        if (p.Hand == 1 && hand == 0) lastIntrusionPlayer = id;
        p.Gate = gate; p.Hand = hand; p.Dual = dual;
        UpdateConditions();
    }

    static void UpdateConditions()
    {
        ConditionGateClosed = players.Values.All(p => p.Gate == 1);
        ConditionHandsSafe = players.Values.All(p => p.Hand == 1);
        ConditionDualHand = players.Values.Any(p => p.Dual == 1);
        OnInterlockStatusChanged?.Invoke();
    }

    // 테스트용
    [ContextMenu("모든 조건 안전")]
    void TestSafe() => UpdatePlayer("test", 1, 1, 1);
}