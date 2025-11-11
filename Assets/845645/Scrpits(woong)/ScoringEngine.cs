using UnityEngine;
using System.Collections.Generic;

public class ScoringEngine : MonoBehaviour
{
    private const int ScoreNoViolation = 100;
    private const int PenaltyIntrusion = -30;
    private const int PenaltyGateOpen = -20;
    private const int BonusCallout = 10;
    private const int ScoreEmergencyStopAppropriate = 5;
    private const int ScoreEmergencyStopMisuse = -50;

    public int teamScore { get; private set; } = 0;
    public Dictionary<string, int> ppeBonusScores { get; private set; } = new();

    public struct ViolationLog
    {
        public float SessionTime;
        public int CycleNumber;
        public string ViolationType;
        public string PlayerId;
    }

    public List<ViolationLog> violationLogs { get; private set; } = new();

    void Awake()
    {
        PressFSM.OnViolationLogged += LogViolationFromFSM;
    }

    void OnDestroy()
    {
        PressFSM.OnViolationLogged -= LogViolationFromFSM;
    }

    private void LogViolationFromFSM(string playerId, int cycle, string violationType, float time)
        => LogViolation(playerId, cycle, violationType, time);

    public void LogViolation(string playerId, int cycle, string violationType, float sessionTime)
    {
        violationLogs.Add(new ViolationLog
        {
            PlayerId = playerId,
            CycleNumber = cycle,
            ViolationType = violationType,
            SessionTime = sessionTime
        });
        Debug.Log($"[Scoring] 위반 기록: {playerId} | 사이클 {cycle} | {violationType}");
    }

    public void AddPpeBonus(string playerId, int bonusScore)
    {
        if (ppeBonusScores.ContainsKey(playerId))
            ppeBonusScores[playerId] += bonusScore;
        else
            ppeBonusScores[playerId] = bonusScore;

        teamScore += bonusScore;
    }

    public void CalculateFinalScore(int totalCyclesCompleted)
    {
        teamScore = 0;

        // 1. 무위반 사이클 점수
        HashSet<int> violatedCycles = new();
        foreach (var log in violationLogs)
            violatedCycles.Add(log.CycleNumber);

        for (int i = 1; i <= totalCyclesCompleted; i++)
        {
            if (!violatedCycles.Contains(i))
                teamScore += ScoreNoViolation;
        }

        // 2. PPE 보너스
        foreach (int bonus in ppeBonusScores.Values)
            teamScore += bonus;

        // 3. 위반 페널티
        foreach (var log in violationLogs)
        {
            switch (log.ViolationType)
            {
                case "HandIntrusion":
                case "HandIntrusionStart":
                    teamScore += PenaltyIntrusion;
                    break;
                case "GateOpen":
                    teamScore += PenaltyGateOpen;
                    break;
                case "EmergencyStopCorrect":
                    teamScore += ScoreEmergencyStopAppropriate;
                    break;
                case "EmergencyStopMisuse":
                    teamScore += ScoreEmergencyStopMisuse;
                    break;
            }
        }

        Debug.Log($"[Scoring] 최종 팀 점수: {teamScore}점 (완료 사이클: {totalCyclesCompleted})");
    }
}