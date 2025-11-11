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
    public static event Action OnSessionStarted; // PPE에서 호출용

    public static PressFSM Instance { get; private set; }

    [Header("플레이어 정보 (NetSession 없어도 됨)")]
    public string playerId = "Player1";
    public bool isHost = true;

    private ScoringEngine scoringEngine;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        scoringEngine = FindObjectOfType<ScoringEngine>();
    }

    void Start()
    {
        Interlocks.OnInterlockStatusChanged += CheckSafetyOnInterlockChange;
        EmergencyStop.OnEmergencyStopActivated += HandleEmergencyStop;
        OnSessionStarted += StartPressSequence;

        // 테스트용 자동 시작
        if (currentState == PressState.IDLE)
            StartPressSequence();
    }

    void OnDestroy()
    {
        Interlocks.OnInterlockStatusChanged -= CheckSafetyOnInterlockChange;
        EmergencyStop.OnEmergencyStopActivated -= HandleEmergencyStop;
        OnSessionStarted -= StartPressSequence;
    }

    private void StartPressSequence()
    {
        currentCycle = 0;
        cycleViolationOccurred = false;
        currentState = PressState.WAITING_START;
        Debug.Log("프레스 시퀀스 시작");
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
        currentState = PressState.EMERGENCY_STOP;
        Debug.LogError($"비상정지 발동! 출처: {sourceId}");

        bool appropriate = (currentState == PressState.CLOSING) && !Interlocks.IsSafeToContinueClosing;
        string type = appropriate ? "EmergencyStopCorrect" : "EmergencyStopMisuse";
        scoringEngine?.LogViolation(sourceId, currentCycle, type, Time.time);
    }

    [ContextMenu("테스트: 다음 사이클 강제 시작")]
    public void ForceNextCycle() => RequestStartCycle();
}