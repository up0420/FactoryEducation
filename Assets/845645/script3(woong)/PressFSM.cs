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

    // 필수 이벤트 정의 (ScoringEngine이 이걸 구독함!)
    public static event Action<int> OnCycleStart;
    public static event Action<string, int, string, float> OnViolationLogged;  // 이 줄이 없어서 에러!
    public static event Action OnAllCyclesComplete;

    private NetSession netSession;
    private ScoringEngine scoringEngine;

    private string PlayerId => netSession?.myId ?? "Player1";

    void Awake()
    {
        netSession = FindObjectOfType<NetSession>();
        scoringEngine = FindObjectOfType<ScoringEngine>();
    }

    private void OnEnable()
    {
        GameSignals.SessionStart += StartPressSequence;
        GameSignals.EmergencyStop += (id, reason) => HandleEmergencyStop(id);
        Interlocks.OnInterlockStatusChanged += CheckSafetyOnInterlockChange;
    }

    private void OnDisable()
    {
        GameSignals.SessionStart -= StartPressSequence;
        GameSignals.EmergencyStop -= (id, reason) => HandleEmergencyStop(id);
        Interlocks.OnInterlockStatusChanged -= CheckSafetyOnInterlockChange;
    }

    private void StartPressSequence()
    {
        currentCycle = 0;
        cycleViolationOccurred = false;
        currentState = PressState.WAITING_START;
        Debug.Log($"[PressFSM] 세션 시작! T0 = {netSession?.SessionTime ?? 0f:F2}s (인천 2025-11-11 14:54 KST)");
    }

    void Update()
    {
        if (currentState == PressState.WAITING_START && currentCycle >= TotalCycles)
        {
            currentState = PressState.CYCLE_COMPLETE;
            OnAllCyclesComplete?.Invoke();
            if (netSession?.role == NodeRole.Host == true)
                scoringEngine?.CalculateFinalScore(currentCycle);
            return;
        }

        if (currentState == PressState.CLOSING_PRE_CHECK)
        {
            if (Interlocks.IsSafeToStartCycle)
                StartClosingMotion();
            else if (!Interlocks.ConditionHandsSafe)
            {
                TriggerViolation(PlayerId, "HandIntrusionStart");
                currentState = PressState.IDLE;
            }
        }

        if (currentState == PressState.CLOSING)
            CheckInterlocksDuringClosing();
    }

    public void RequestStartCycle()
    {
        if (currentState != PressState.WAITING_START || currentCycle >= TotalCycles) return;
        if (!Interlocks.ConditionDualHand) return;

        currentCycle++;
        cycleViolationOccurred = false;
        cycleStartTime = Time.time;
        OnCycleStart?.Invoke(currentCycle);
        currentState = PressState.CLOSING_PRE_CHECK;
        Debug.Log($"[PressFSM] 사이클 {currentCycle} 시작");
    }

    private void StartClosingMotion()
    {
        currentState = PressState.CLOSING;
        Debug.Log("[PressFSM] 프레스 닫힘 동작 시작");
    }

    private void CheckSafetyOnInterlockChange() => CheckInterlocksDuringClosing();

    private void CheckInterlocksDuringClosing()
    {
        if (cycleViolationOccurred || currentState != PressState.CLOSING) return;
        if (Interlocks.IsSafeToContinueClosing) return;

        string type = !Interlocks.ConditionGateClosed ? "GateOpen" : "HandIntrusion";
        string violator = Interlocks.GetLastHandIntrusionPlayer() ?? PlayerId;
        TriggerViolation(violator, type);
    }

    private void TriggerViolation(string playerId, string type)
    {
        if (cycleViolationOccurred) return;
        cycleViolationOccurred = true;
        currentState = PressState.IDLE;
        float violationTime = Time.time - cycleStartTime;

        Debug.LogWarning($"[위반] {playerId} → {type} ({violationTime:F2}s 후)");

        // 이 줄이 ScoringEngine에 전달됨!
        OnViolationLogged?.Invoke(playerId, currentCycle, type, Time.time);
    }

    public void HandleEmergencyStop(string sourcePlayerId)
    {
        if (currentState == PressState.EMERGENCY_STOP) return;

        currentState = PressState.EMERGENCY_STOP;
        Debug.LogError($"비상 정지 발동! 출처: {sourcePlayerId} (인천 2025-11-11 14:54 KST)");

        bool isAppropriate = (currentState == PressState.CLOSING || currentState == PressState.CLOSING_PRE_CHECK)
                             && !Interlocks.IsSafeToContinueClosing;

        string type = isAppropriate ? "EmergencyStopCorrect" : "EmergencyStopMisuse";

        // 이 줄도 ScoringEngine에 전달됨!
        OnViolationLogged?.Invoke(sourcePlayerId, currentCycle, type, Time.time);
    }

    [ContextMenu("테스트: 다음 사이클 강제 시작")]
    public void ForceNextCycle() => RequestStartCycle();

    [ContextMenu("테스트: 세션 강제 종료")]
    public void ForceSessionEnd()
    {
        currentCycle = TotalCycles;
        currentState = PressState.CYCLE_COMPLETE;
        OnAllCyclesComplete?.Invoke();
    }
}