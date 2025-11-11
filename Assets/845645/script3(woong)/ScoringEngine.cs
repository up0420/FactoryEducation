using UnityEngine;
using System.Collections.Generic;

public class ScoringEngine : MonoBehaviour
{
    private const int ScoreNoViolation = 100;
    private const int PenaltyIntrusion = -30;
    private const int PenaltyGateOpen = -20;
    private const int ScoreEmergencyStopAppropriate = 5;
    private const int ScoreEmergencyStopMisuse = -50;

    public int teamScore { get; private set; }
    public List<ViolationLog> violationLogs = new();

    public struct ViolationLog
    {
        public float SessionTime;
        public int CycleNumber;
        public string ViolationType;
        public string PlayerId;
    }

    void Awake()
    {
        PressFSM.OnViolationLogged += (p, c, t, time) => LogViolation(p, c, t, time);
    }

    public void LogViolation(string playerId, int cycle, string type, float time)
    {
        violationLogs.Add(new ViolationLog
        {
            PlayerId = playerId,
            CycleNumber = cycle,
            ViolationType = type,
            SessionTime = time
        });
    }

    public void CalculateFinalScore(int completedCycles)
    {
        teamScore = 0;
        HashSet<int> violated = new();
        foreach (var log in violationLogs) violated.Add(log.CycleNumber);

        for (int i = 1; i <= completedCycles; i++)
            if (!violated.Contains(i)) teamScore += ScoreNoViolation;

        foreach (var log in violationLogs)
        {
            switch (log.ViolationType)
            {
                case "HandIntrusion":
                case "HandIntrusionStart":
                    teamScore += PenaltyIntrusion; break;
                case "GateOpen":
                    teamScore += PenaltyGateOpen; break;
                case "EmergencyStopCorrect":
                    teamScore += ScoreEmergencyStopAppropriate; break;
                case "EmergencyStopMisuse":
                    teamScore += ScoreEmergencyStopMisuse; break;
            }
        }

        Debug.Log($"최종 점수: {teamScore}점");
    }
}