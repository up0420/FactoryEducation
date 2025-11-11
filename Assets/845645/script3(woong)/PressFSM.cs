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
    // 이전: public static event Action OnSessionStarted; // PPE에서 호출용
    // 변경: GameSignals.SessionStart를 구독하여 NetSession으로부터 시작 신호를 받음

    public static PressFSM Instance { get; private set; }

    [Header("플레이어 정보 (NetSession 없어도 됨)")]
    public string playerId = "Player1";
    public bool isHost = true;

    private ScoringEngine scoringEngine;
    private NetSession netSession; // NetSession 참조 추가

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        scoringEngine = FindObjectOfType<ScoringEngine>();
        netSession = FindObjectOfType<NetSession>();
    }

    void Start()
    {
        Interlocks.OnInterlockStatusChanged += CheckSafetyOnInterlockChange;
        EmergencyStop.OnEmergencyStopActivated += HandleEmergencyStop;

        // NetSession 통합: GameSignals를 통해 시작 신호를 받음
        GameSignals.SessionStart += StartPressSequence;

        // NetSession이 있을 경우 초기 상태는 NetSession의 역할에 따라 달라질 수 있음
        if (netSession == null || netSession.role == NodeRole.Host)
        {
            if (currentState == PressState.IDLE)
            {
                // 호스트/단일 모드: 초기 시작 대기 상태로 진입
                currentState = PressState.WAITING_START;
                Debug.Log("호스트/단일 모드: 세션 시작 대기 중");
            }
        }
    }

    void OnDestroy()
    {
        Interlocks.OnInterlockStatusChanged -= CheckSafetyOnInterlockChange;
        EmergencyStop.OnEmergencyStopActivated -= HandleEmergencyStop;
        GameSignals.SessionStart -= StartPressSequence; // 구독 해제
    }

    private void StartPressSequence()
    {
        currentCycle = 0;
        cycleViolationOccurred = false;
        currentState = PressState.WAITING_START;
        Debug.Log("네트워크 세션 시작 신호 수신: 프레스 시퀀스 시작");
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
                Debug.Log("세션 완료. 최종 점수 계산 완료.");
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
            Debug.Log($"사이클 {currentCycle} 시작");
        }
    }

    private void StartClosingMotion()
    {
        currentState = PressState.CLOSING;
        Debug.Log("프레스 닫힘 시작");
    }

    private void CheckSafetyOnInterlockChange()
    {
        if (currentState == PressState.CLOSING) CheckInterlocksDuringClosing();
        // 클라이언트 모드에서 인터록 상태가 변경되면 호스트에게 전송 (옵션)
        if (netSession != null && netSession.role == NodeRole.Client)
        {
            // Interlocks 상태(gate, hand, dual)를 NetSession을 통해 호스트에게 전송
            // 이 로직은 Interlocks.cs에 추가될 수 있습니다.
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
        Debug.LogWarning($"[위반] {playerId} - {type} ({t:F2}s)");
        scoringEngine?.LogViolation(playerId, currentCycle, type, Time.time);
        OnViolationLogged?.Invoke(playerId, currentCycle, type, t);
    }

    public void HandleEmergencyStop(string sourceId)
    {
        if (currentState == PressState.EMERGENCY_STOP) return;
        PressState prevState = currentState;
        currentState = PressState.EMERGENCY_STOP;
        Debug.LogError($"비상정지 발동! 출처: {sourceId}");

        // 비상 정지가 '적절'했는지 판단하는 로직 개선
        // 닫힘 상태였고, 위험한 상황(인터록 위반)이 있었을 때 적절함
        bool appropriate = (prevState == PressState.CLOSING) && !Interlocks.IsSafeToContinueClosing;
        string type = appropriate ? "EmergencyStopCorrect" : "EmergencyStopMisuse";

        // 현재 사이클에 위반 로그 기록
        scoringEngine?.LogViolation(sourceId, currentCycle, type, Time.time);

        // (추가 로직: 비상정지 후 프레스를 다시 Open 상태로 전환하는 로직이 필요함)
    }

    [ContextMenu("테스트: 다음 사이클 강제 시작")]
    public void ForceNextCycle() => RequestStartCycle();
}