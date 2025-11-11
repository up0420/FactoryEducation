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

    // NetSession 인스턴스를 저장 (클라이언트 모드에서 호스트에게 상태 전송용)
    private static NetSession netSession;

    public static string GetLastHandIntrusionPlayer() => lastIntrusionPlayer;

    public class PlayerData
    {
        public int Gate = 1; // 1: Closed, 0: Open
        public int Hand = 1; // 1: Safe, 0: Intrusion
        public int Dual = 0; // 1: Dual-Hand Engaged, 0: Not Engaged
    }

    void Start()
    {
        // NetSession 찾기 (클라이언트 모드에서 상태 전송용)
        netSession = FindObjectOfType<NetSession>();
    }

    /// <summary>
    /// 지정된 플레이어의 인터록 관련 상태를 업데이트합니다.
    /// NetSession의 /interlock/status 또는 /ppe/select 메시지 수신 시 호출됩니다.
    /// </summary>
    /// <param name="id">플레이어 ID</param>
    /// <param name="gate">게이트 상태 (1: Closed, 0: Open)</param>
    /// <param name="hand">손 안전 상태 (1: Safe, 0: Intrusion)</param>
    /// <param name="dual">양손 작동 상태 (1: Engaged, 0: Not Engaged)</param>
    public static void UpdatePlayer(string id, int gate, int hand, int dual)
    {
        if (!players.ContainsKey(id)) players[id] = new PlayerData();
        var p = players[id];

        // 손 침입 감지 시 침입 플레이어 ID 업데이트
        if (p.Hand == 1 && hand == 0) lastIntrusionPlayer = id;

        p.Gate = gate; p.Hand = hand; p.Dual = dual;
        UpdateConditions();

        // 클라이언트 모드인 경우, 호스트에게 자신의 상태를 즉시 전송 (선택적)
        if (netSession != null && netSession.role == NodeRole.Client)
        {
            // NetSession.SendInterlockStatus(gate, hand, dual); 
            // 이 로직은 PressFSM.cs의 CheckSafetyOnInterlockChange에서 처리하는 것이 좋습니다.
        }
    }

    static void UpdateConditions()
    {
        // 모든 플레이어의 게이트가 닫혀야 함
        ConditionGateClosed = players.Values.All(p => p.Gate == 1);
        // 모든 플레이어의 손이 안전해야 함
        ConditionHandsSafe = players.Values.All(p => p.Hand == 1);
        // 한 명이라도 양손 작동을 해야 함 (사이클 시작 조건)
        ConditionDualHand = players.Values.Any(p => p.Dual == 1);

        OnInterlockStatusChanged?.Invoke();
    }

    // 테스트용
    [ContextMenu("모든 조건 안전")]
    void TestSafe() => UpdatePlayer("test", 1, 1, 1);
}