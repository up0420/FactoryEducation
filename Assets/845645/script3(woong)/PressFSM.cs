using UnityEngine;
using System;

public class PressFSM : MonoBehaviour
{
    public enum PressState { IDLE, WAITING_START, CLOSING_PRE_CHECK, CLOSING, OPENING, EMERGENCY_STOP, CYCLE_COMPLETE, SESSION_END }
    public PressState currentState = PressState.IDLE;

    private const int TotalCycles = 5;
    private int currentCycle = 0;
    private float cycleStartTime;
    private bool cycleViolationOccurred = false;

    public static event Action<int> OnCycleStart;
    public static event Action<string, int, string, float> OnViolationLogged;
    public static event Action OnAllCyclesComplete;
    // ����: public static event Action OnSessionStarted; // PPE���� ȣ���
    // ����: GameSignals.SessionStart�� �����Ͽ� NetSession���κ��� ���� ��ȣ�� ����

    public static PressFSM Instance { get; private set; }

    [Header("�÷��̾� ���� (NetSession ��� ��)")]
    public string playerId = "Player1";
    public bool isHost = true;

    private ScoringEngine scoringEngine;
    private NetSession1 netSession; // NetSession ���� �߰�

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        scoringEngine = FindObjectOfType<ScoringEngine>();
        netSession = FindObjectOfType<NetSession1>();
    }

    void Start()
    {
        Interlocks.OnInterlockStatusChanged += CheckSafetyOnInterlockChange;
        EmergencyStop.OnEmergencyStopActivated += HandleEmergencyStop;

        // NetSession ����: GameSignals�� ���� ���� ��ȣ�� ����
        GameSignals.SessionStart += StartPressSequence;

        // NetSession�� ���� ��� �ʱ� ���´� NetSession�� ���ҿ� ���� �޶��� �� ����
        if (netSession == null || netSession.role == NodeRole.Host)
        {
            if (currentState == PressState.IDLE)
            {
                // ȣ��Ʈ/���� ���: �ʱ� ���� ��� ���·� ����
                currentState = PressState.WAITING_START;
                Debug.Log("ȣ��Ʈ/���� ���: ���� ���� ��� ��");
            }
        }
    }

    void OnDestroy()
    {
        Interlocks.OnInterlockStatusChanged -= CheckSafetyOnInterlockChange;
        EmergencyStop.OnEmergencyStopActivated -= HandleEmergencyStop;
        GameSignals.SessionStart -= StartPressSequence; // ���� ����
    }

    private void StartPressSequence()
    {
        currentCycle = 0;
        cycleViolationOccurred = false;
        currentState = PressState.WAITING_START;
        Debug.Log("��Ʈ��ũ ���� ���� ��ȣ ����: ������ ������ ����");
    }

    void Update()
    {
        switch (currentState)
        {
            case PressState.WAITING_START:
                if (currentCycle >= TotalCycles)
                {
                    currentState = PressState.CYCLE_COMPLETE;
                    OnAllCyclesComplete?.Invoke();
                }
                break;

            case PressState.CLOSING_PRE_CHECK:
                if (Interlocks.IsSafeToStartCycle) StartClosingMotion();
                else if (!Interlocks.ConditionHandsSafe)
                {
                    TriggerViolation(playerId, "HandIntrusionStart");
                    currentState = PressState.IDLE;
                }
                break;

            case PressState.CLOSING:
                CheckInterlocksDuringClosing();
                break;

            case PressState.CYCLE_COMPLETE:
                if (isHost && scoringEngine != null)
                    scoringEngine.CalculateFinalScore(currentCycle);
                currentState = PressState.SESSION_END;
                Debug.Log("���� �Ϸ�. ���� ���� ��� �Ϸ�.");
                break;
        }
    }

    public void RequestStartCycle()
    {
        if (currentState != PressState.WAITING_START || currentCycle >= TotalCycles) return;
        if (Interlocks.ConditionDualHand)
        {
            currentCycle++;
            cycleViolationOccurred = false;
            cycleStartTime = Time.time;
            OnCycleStart?.Invoke(currentCycle);
            currentState = PressState.CLOSING_PRE_CHECK;
            Debug.Log($"����Ŭ {currentCycle} ����");
        }
    }

    private void StartClosingMotion()
    {
        currentState = PressState.CLOSING;
        Debug.Log("������ ���� ����");
    }

    private void CheckSafetyOnInterlockChange()
    {
        if (currentState == PressState.CLOSING) CheckInterlocksDuringClosing();
        // Ŭ���̾�Ʈ ��忡�� ���ͷ� ���°� ����Ǹ� ȣ��Ʈ���� ���� (�ɼ�)
        if (netSession != null && netSession.role == NodeRole.Client)
        {
            // Interlocks ����(gate, hand, dual)�� NetSession�� ���� ȣ��Ʈ���� ����
            // �� ������ Interlocks.cs�� �߰��� �� �ֽ��ϴ�.
            // netSession.SendInterlockStatus(Interlocks.ConditionGateClosed ? 1 : 0, Interlocks.ConditionHandsSafe ? 1 : 0, Interlocks.ConditionDualHand ? 1 : 0);
        }
    }

    private void CheckInterlocksDuringClosing()
    {
        if (cycleViolationOccurred || currentState != PressState.CLOSING) return;
        if (!Interlocks.IsSafeToContinueClosing)
        {
            string type = !Interlocks.ConditionGateClosed ? "GateOpen" : "HandIntrusion";
            string violator = Interlocks.GetLastHandIntrusionPlayer() ?? playerId;
            TriggerViolation(violator, type);
        }
    }

    private void TriggerViolation(string playerId, string type)
    {
        if (cycleViolationOccurred) return;
        cycleViolationOccurred = true;
        currentState = PressState.IDLE;
        float t = Time.time - cycleStartTime;
        Debug.LogWarning($"[����] {playerId} - {type} ({t:F2}s)");
        scoringEngine?.LogViolation(playerId, currentCycle, type, Time.time);
        OnViolationLogged?.Invoke(playerId, currentCycle, type, t);
    }

    public void HandleEmergencyStop(string sourceId)
    {
        if (currentState == PressState.EMERGENCY_STOP) return;
        PressState prevState = currentState;
        currentState = PressState.EMERGENCY_STOP;
        Debug.LogError($"������� �ߵ�! ��ó: {sourceId}");

        // ��� ������ '����'�ߴ��� �Ǵ��ϴ� ���� ����
        // ���� ���¿���, ������ ��Ȳ(���ͷ� ����)�� �־��� �� ������
        bool appropriate = (prevState == PressState.CLOSING) && !Interlocks.IsSafeToContinueClosing;
        string type = appropriate ? "EmergencyStopCorrect" : "EmergencyStopMisuse";

        // ���� ����Ŭ�� ���� �α� ���
        scoringEngine?.LogViolation(sourceId, currentCycle, type, Time.time);

        // (�߰� ����: ������� �� �������� �ٽ� Open ���·� ��ȯ�ϴ� ������ �ʿ���)
    }

    [ContextMenu("�׽�Ʈ: ���� ����Ŭ ���� ����")]
    public void ForceNextCycle() => RequestStartCycle();
}