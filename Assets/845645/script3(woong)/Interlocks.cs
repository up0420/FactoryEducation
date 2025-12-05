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

    // NetSession �ν��Ͻ��� ���� (Ŭ���̾�Ʈ ��忡�� ȣ��Ʈ���� ���� ���ۿ�)
    private static NetSession1 netSession;

    public static string GetLastHandIntrusionPlayer() => lastIntrusionPlayer;

    public class PlayerData
    {
        public int Gate = 1; // 1: Closed, 0: Open
        public int Hand = 1; // 1: Safe, 0: Intrusion
        public int Dual = 0; // 1: Dual-Hand Engaged, 0: Not Engaged
    }

    void Start()
    {
        // NetSession ã�� (Ŭ���̾�Ʈ ��忡�� ���� ���ۿ�)
        netSession = FindObjectOfType<NetSession1>();
    }

    /// <summary>
    /// ������ �÷��̾��� ���ͷ� ���� ���¸� ������Ʈ�մϴ�.
    /// NetSession�� /interlock/status �Ǵ� /ppe/select �޽��� ���� �� ȣ��˴ϴ�.
    /// </summary>
    /// <param name="id">�÷��̾� ID</param>
    /// <param name="gate">����Ʈ ���� (1: Closed, 0: Open)</param>
    /// <param name="hand">�� ���� ���� (1: Safe, 0: Intrusion)</param>
    /// <param name="dual">��� �۵� ���� (1: Engaged, 0: Not Engaged)</param>
    public static void UpdatePlayer(string id, int gate, int hand, int dual)
    {
        if (!players.ContainsKey(id)) players[id] = new PlayerData();
        var p = players[id];

        // �� ħ�� ���� �� ħ�� �÷��̾� ID ������Ʈ
        if (p.Hand == 1 && hand == 0) lastIntrusionPlayer = id;

        p.Gate = gate; p.Hand = hand; p.Dual = dual;
        UpdateConditions();

        // Ŭ���̾�Ʈ ����� ���, ȣ��Ʈ���� �ڽ��� ���¸� ��� ���� (������)
        if (netSession != null && netSession.role == NodeRole.Client)
        {
            // NetSession.SendInterlockStatus(gate, hand, dual); 
            // �� ������ PressFSM.cs�� CheckSafetyOnInterlockChange���� ó���ϴ� ���� �����ϴ�.
        }
    }

    static void UpdateConditions()
    {
        // ��� �÷��̾��� ����Ʈ�� ������ ��
        ConditionGateClosed = players.Values.All(p => p.Gate == 1);
        // ��� �÷��̾��� ���� �����ؾ� ��
        ConditionHandsSafe = players.Values.All(p => p.Hand == 1);
        // �� ���̶� ��� �۵��� �ؾ� �� (����Ŭ ���� ����)
        ConditionDualHand = players.Values.Any(p => p.Dual == 1);

        OnInterlockStatusChanged?.Invoke();
    }

    // �׽�Ʈ��
    [ContextMenu("��� ���� ����")]
    void TestSafe() => UpdatePlayer("test", 1, 1, 1);
}