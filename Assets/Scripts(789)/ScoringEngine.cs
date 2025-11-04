using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 점수 엔진 및 위반 로그 관리 모듈 (Owner: Gameplay).
/// 모든 위반 및 점수 관련 이벤트를 기록하고 점수를 계산합니다.
/// </summary>
public class ScoringEngine : MonoBehaviour
{
    // === 점수 상수 ===
    private const int ScoreNoViolation = 100;
    private const int PenaltyIntrusion = -30;
    private const int PenaltyGateUnconfirmed = -20;
    private const int BonusCallout = 10; // 콜아웃 옵션
    private const int ScoreEmergencyStopAppropriate = 5;
    private const int ScoreEmergencyStopMisuse = -5;

    // === 상태 변수 ===
    public int teamScore { get; private set; } = 0;
    public Dictionary<string, int> ppeBonusScores { get; private set; } = new Dictionary<string, int>();

    // 위반 로그 구조체
    public struct ViolationLog
    {
        public float SessionTime; // T0 기준 시간 (초)
        public int CycleNumber;
        public string ViolationType; // HandIntrusion, GateOpen, EmergencyStop
        public string PlayerId;
    }

    public List<ViolationLog> violationLogs { get; private set; } = new List<ViolationLog>();

    void Awake()
    {
        // 위반 및 점수 이벤트 구독
        PressFSM.OnViolationLogged += LogViolation;
        // NetSession.OnPpeScoreReceived += AddPpeBonus; // 네트워크 모듈과 연결 필요
    }

    /// <summary>
    /// 위반 발생 시 로그를 기록합니다. (PressFSM에서 호출)
    /// </summary>
    public void LogViolation(string playerId, int cycle, string violationType, float sessionTime)
    {
        violationLogs.Add(new ViolationLog
        {
            PlayerId = playerId,
            CycleNumber = cycle,
            ViolationType = violationType,
            SessionTime = sessionTime
        });
        Debug.Log($"[Scoring] 위반 기록됨: {playerId}, 사이클 {cycle}, 타입 {violationType}");
    }

    /// <summary>
    /// PPE 시스템으로부터 보너스 점수를 수신하여 기록합니다.
    /// </summary>
    public void AddPpeBonus(string playerId, int bonusScore)
    {
        ppeBonusScores[playerId] = bonusScore;
        teamScore += bonusScore;
    }

    /// <summary>
    /// 세션 종료 시 최종 점수를 계산합니다. (호스트 전용)
    /// </summary>
    public void CalculateFinalScore(int totalCyclesCompleted)
    {
        teamScore = 0; // 점수 초기화 후 재계산

        // 1. 기본 점수 (무위반 사이클)
        int cleanCycles = totalCyclesCompleted; // 위반이 발생한 사이클은 제외되어야 함

        // 모든 위반 로그를 검토하여 사이클당 기본 점수 계산
        Dictionary<int, bool> cycleViolated = new Dictionary<int, bool>();
        foreach (var log in violationLogs)
        {
            cycleViolated[log.CycleNumber] = true;
        }

        // 위반이 없는 사이클마다 +100점 부여
        for (int i = 1; i <= totalCyclesCompleted; i++)
        {
            if (!cycleViolated.ContainsKey(i))
            {
                teamScore += ScoreNoViolation;
            }
        }

        // 2. PPE 보너스 합산
        foreach (int score in ppeBonusScores.Values)
        {
            teamScore += score;
        }

        // 3. 로그 기반 위반 페널티 합산
        foreach (var log in violationLogs)
        {
            switch (log.ViolationType)
            {
                case "HandIntrusion":
                    teamScore += PenaltyIntrusion;
                    break;
                case "GateUnconfirmed":
                    teamScore += PenaltyGateUnconfirmed;
                    break;
                case "EmergencyStopCorrect":
                    teamScore += ScoreEmergencyStopAppropriate;
                    break;
                case "EmergencyStopMisuse":
                    teamScore += ScoreEmergencyStopMisuse;
                    break;
                    // 기타 위반 로그 처리...
            }
        }

        Debug.Log($"[Scoring] 최종 팀 점수 계산 완료: {teamScore}점");
        // 리포트 빌더에 데이터를 전달합니다.
        // ReportBuilder.GenerateReport(this);
    }
}
