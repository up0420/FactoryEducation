using UnityEngine;
using System;
using System.Collections.Generic;

/// <summary>
/// 인터락 조건 체크 및 실시간 상태를 제공하는 모듈 (Owner: Gameplay).
/// 모든 플레이어의 상태를 취합하여 최종 인터락 안전 상태를 판단합니다.
/// </summary>
public class Interlocks : MonoBehaviour
{
    // === 인터락 상태 (각 조건별 0 또는 1) ===
    // 1. 게이트 닫힘, 2. 손/도구 안전구역 이탈, 3. OK 확인(옵션), 4. 양손 동시
    public static bool ConditionGateClosed { get; private set; } = false;
    public static bool ConditionHandsSafe { get; private set; } = false;
    public static bool ConditionOkConfirmed { get; private set; } = true; // 옵션, 기본적으로 안전
    public static bool ConditionDualHand { get; private set; } = false;

    // === 실시간 안전 상태 ===
    public static bool IsSafeToStartCycle { get; private set; } = false;
    public static bool IsSafeToContinueClosing { get; private set; } = false;

    // === 플레이어별 상태 (네트워크 입력 수신용) ===
    private Dictionary<string, PlayerSafetyData> playerStates = new Dictionary<string, PlayerSafetyData>();

    // 인터락 업데이트 이벤트 (FSM에 신호를 보냄)
    public static event Action OnInterlockStatusChanged;

    private class PlayerSafetyData
    {
        // 0: 위반, 1: 안전 (프로토콜 정의에 따름)
        public int GateStatus = 0;      // (1)
        public int HandStatus = 0;      // (2)
        public int OkStatus = 1;        // (3)
        public int DualHandStatus = 0;  // (4)
    }

    void Update()
    {
        // 실시간으로 안전 상태를 업데이트합니다.
        CheckSafetyConditions();
    }

    /// <summary>
    /// 네트워크(OSC/UDP)를 통해 수신된 플레이어의 실시간 안전 상태를 업데이트합니다.
    /// </summary>
    /// <param name="playerId">플레이어 ID</param>
    /// <param name="gateStatus">게이트 상태 (0: open, 1: closed)</param>
    /// <param name="handStatus">손/도구 안전구역 (0: intrusion, 1: safe)</param>
    /// <param name="okStatus">OK 버튼 확인 (0: unconfirmed, 1: confirmed)</param>
    /// <param name="dualHandStatus">양손 동시 버튼 (0: not active, 1: active)</param>
    public void UpdatePlayerState(string playerId, int gateStatus, int handStatus, int okStatus, int dualHandStatus)
    {
        if (!playerStates.ContainsKey(playerId))
        {
            playerStates.Add(playerId, new PlayerSafetyData());
        }

        var data = playerStates[playerId];
        data.GateStatus = gateStatus;
        data.HandStatus = handStatus;
        data.OkStatus = okStatus;
        data.DualHandStatus = dualHandStatus;

        // 데이터 수신 시 인터락 조건을 재검사합니다.
        CheckSafetyConditions();
    }

    /// <summary>
    /// 모든 플레이어의 상태를 종합하여 최종 인터락 상태를 결정합니다.
    /// </summary>
    private void CheckSafetyConditions()
    {
        // 1. 게이트 닫힘: 모든 플레이어의 게이트가 닫혀야 함 (VR 환경의 메인 게이트 상태를 따름)
        // (가정: 메인 게이트는 하나이며, GateStatus는 그 물리적 상태를 반영함)
        ConditionGateClosed = CheckAllPlayersStatus(p => p.GateStatus == 1);

        // 2. 손/도구 안전구역 이탈: 모든 플레이어가 안전구역을 이탈하지 않았어야 함 (한 명이라도 침범하면 FALSE)
        ConditionHandsSafe = CheckAllPlayersStatus(p => p.HandStatus == 1);

        // 3. OK 확인: 모든 플레이어가 최근 3초 내 OK 확인을 했어야 함 (옵션, 현재는 켜져 있는 것으로 간주)
        ConditionOkConfirmed = CheckAllPlayersStatus(p => p.OkStatus == 1);

        // 4. 양손 동시: 사이클 시작을 위해, 현재 플레이어(Local Player)가 양손을 1초 이상 눌렀어야 함
        // (네트워크를 통해 Local Player의 ID가 포함된 DualHandStatus를 수신해야 함)
        // 이 로직은 Local Player의 DualHandInput 컴포넌트에서 직접 가져오는 것이 더 정확할 수 있습니다.
        // 현재는 네트워크에서 받은 상태가 1인 플레이어가 하나라도 있으면 true로 간주합니다.
        ConditionDualHand = CheckAllPlayersStatus(p => p.DualHandStatus == 1);

        // 최종 안전 상태 결정
        // 사이클 시작 조건: 게이트 닫힘 & 손 안전 & 양손 동시
        IsSafeToStartCycle = ConditionGateClosed && ConditionHandsSafe && ConditionDualHand;

        // 사이클 진행 조건 (CLOSING 상태에서): 게이트 닫힘 & 손 안전
        IsSafeToContinueClosing = ConditionGateClosed && ConditionHandsSafe;

        OnInterlockStatusChanged?.Invoke();
    }

    private bool CheckAllPlayersStatus(System.Predicate<PlayerSafetyData> predicate)
    {
        if (playerStates.Count == 0) return true; // 플레이어 데이터가 없으면 안전으로 간주 (초기 상태)

        bool allSafe = true;
        foreach (var state in playerStates.Values)
        {
            if (!predicate(state))
            {
                allSafe = false;
                break;
            }
        }
        return allSafe;
    }
}
