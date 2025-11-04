using UnityEngine;
using System;
using System.Collections;
using System.Collections.Generic;

/// <summary>
/// B. 인터랙션·프레스 FSM 담당 모듈 (Owner: Gameplay)
/// 프레스 기계의 상태 기계(FSM)를 관리하고 안전 인터락 및 채점 엔진을 통합합니다.
/// </summary>
public class PressFSM : MonoBehaviour
{
    // === 상태 정의 ===
    public enum PressState { IDLE, WAITING_START, CLOSING_PRE_CHECK, CLOSING, OPENING, EMERGENCY_STOP, CYCLE_COMPLETE, SESSION_END }
    public PressState currentState = PressState.IDLE;

    // === 사이클 변수 ===
    private const int TotalCycles = 5;
    private int currentCycle = 0;
    private float cycleStartTime;
    private bool cycleViolationOccurred = false;

    // === 이벤트 ===
    public static event Action<int> OnCycleStart;
    public static event Action<string, int, string, float> OnViolationLogged; // (PlayerId, Cycle, Type, Time)
    public static event Action OnAllCyclesComplete;

    // === 컴포넌트 참조 ===
    private NetSession netSession;
    private ScoringEngine scoringEngine;

    void Start()
    {
        // 필수 컴포넌트 참조
        netSession = FindObjectOfType<NetSession>();
        scoringEngine = FindObjectOfType<ScoringEngine>();

        // 게임 시작 이벤트 구독
        NetSession.OnSessionStarted += StartPressSequence;
        // 인터락 상태 변경 이벤트 구독
        Interlocks.OnInterlockStatusChanged += CheckSafetyOnInterlockChange;
        // 비상 정지 이벤트 구독 (최우선)
        EmergencyStop.OnEmergencyStopActivated += HandleEmergencyStop;
    }

    /// <summary>
    /// 게임이 시작되면 프레스 시퀀스를 초기화하고 시작합니다.
    /// </summary>
    private void StartPressSequence()
    {
        currentCycle = 0;
        currentState = PressState.WAITING_START;
        Debug.Log("프레스 시퀀스 시작. 사이클 시작 대기 중.");
    }

    void Update()
    {
        // FSM 업데이트
        switch (currentState)
        {
            case PressState.WAITING_START:
                if (currentCycle < TotalCycles)
                {
                    TryStartNextCycle();
                }
                else
                {
                    // 5회 사이클 완료
                    currentState = PressState.CYCLE_COMPLETE;
                    OnAllCyclesComplete?.Invoke();
                }
                break;

            case PressState.CLOSING_PRE_CHECK:
                // 양손 동시 조건이 충족되었는지 확인 (4 조건)
                if (Interlocks.IsSafeToStartCycle)
                {
                    StartClosingMotion();
                }
                else if (Interlocks.ConditionHandsSafe == false)
                {
                    // 손이 안전하지 않으면 시작할 수 없음 (위반 로깅)
                    TriggerViolation(netSession.userId, "HandIntrusionStart");
                    currentState = PressState.IDLE;
                }
                break;

            case PressState.CLOSING:
                CheckInterlocksDuringClosing();
                // 프레스 헤드 움직임 및 완료 로직 (Placeholder)
                // if (pressHeadAtBottom) TransitionToOpening();
                break;

            case PressState.OPENING:
                // 프레스 헤드 움직임 및 완료 로직 (Placeholder)
                // if (pressHeadAtTop) TransitionToWaitingStart();
                break;

            case PressState.CYCLE_COMPLETE:
                // 최종 점수 산정 및 세션 종료 준비
                if (netSession.isHost)
                {
                    scoringEngine.CalculateFinalScore(TotalCycles);
                    // netSession.SendWsMessage("/session/end", ...);
                }
                currentState = PressState.SESSION_END;
                break;
        }
    }

    private void TryStartNextCycle()
    {
        // 양손 버튼 입력 등 외부 신호가 들어왔을 때 호출
        if (Interlocks.ConditionDualHand) // 양손 동시 조건 충족 시
        {
            currentCycle++;
            cycleViolationOccurred = false;
            cycleStartTime = Time.time;
            OnCycleStart?.Invoke(currentCycle);
            currentState = PressState.CLOSING_PRE_CHECK; // 닫힘 전 최종 체크 단계로 전환
            Debug.Log($"--- 사이클 {currentCycle} 시작 대기 (프리 체크) ---");
        }
    }

    private void StartClosingMotion()
    {
        currentState = PressState.CLOSING;
        Debug.Log("프레스 닫힘 동작 시작.");
    }

    private void CheckSafetyOnInterlockChange()
    {
        // CLOSING 상태일 때만 인터락 변경에 반응
        if (currentState == PressState.CLOSING)
        {
            CheckInterlocksDuringClosing();
        }
    }

    /// <summary>
    /// 프레스 작동 중 (CLOSING 상태) 인터락 조건을 실시간으로 검사합니다.
    /// </summary>
    private void CheckInterlocksDuringClosing()
    {
        if (cycleViolationOccurred || currentState != PressState.CLOSING) return;

        // 사이클 중단 위반 검사: 게이트가 열리거나 손이 침범했을 경우
        if (!Interlocks.IsSafeToContinueClosing)
        {
            string violationType = "";
            string violatingPlayer = netSession.userId; // 위반한 플레이어를 찾아야 함 (현재는 Local로 가정)

            if (!Interlocks.ConditionGateClosed) violationType = "GateOpen";
            else if (!Interlocks.ConditionHandsSafe) violationType = "HandIntrusion";

            if (!string.IsNullOrEmpty(violationType))
            {
                TriggerViolation(violatingPlayer, violationType);
            }
        }
    }

    /// <summary>
    /// 안전 위반 발생 시 처리 로직.
    /// </summary>
    private void TriggerViolation(string playerId, string violationType)
    {
        float violationTime = Time.time - cycleStartTime;
        cycleViolationOccurred = true;
        currentState = PressState.IDLE; // 사이클 즉시 중단

        Debug.LogWarning($"[위반 발생] 타입: {violationType}, 플레이어: {playerId}, 시간: {violationTime:F2}초");

        // ScoringEngine에 로그 기록
        scoringEngine.LogViolation(playerId, currentCycle, violationType, Time.time);

        // 네트워크에 위반 로그 전송 (호스트 전용)
        if (netSession.isHost)
        {
            // netSession.SendWsMessage("/violation/log", ...);
        }
    }

    /// <summary>
    /// 비상정지 버튼이 눌렸을 때 호출됩니다. (최우선 처리)
    /// </summary>
    public void HandleEmergencyStop(string sourcePlayerId)
    {
        if (currentState == PressState.EMERGENCY_STOP) return;

        // 현재 사이클 중단 및 상태 전환
        currentState = PressState.EMERGENCY_STOP;
        Debug.LogError($"!!! 비상 정지 발동 !!! 출처: {sourcePlayerId}");

        // 비상 정지 적절성 채점 로직 추가 필요 (ScoringEngine 통합)
        scoringEngine.LogViolation(sourcePlayerId, currentCycle, "EmergencyStop", Time.time);

        // 모든 움직임 즉시 중단 로직 추가
    }
}
