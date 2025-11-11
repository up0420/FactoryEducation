using UnityEngine;
using System;

public class EmergencyStop : MonoBehaviour
{
    // NetSession -> PressFSM으로 전달되는 이벤트 (네트워크/로컬 모두 포함)
    public static event Action<string> OnEmergencyStopActivated;
    // 로컬 발동 시 NetSession에 전송을 알리는 이벤트 (NetSession이 구독)
    public static event Action<string> OnEmergencyStopActivatedLocally;

    public static EmergencyStop Instance { get; private set; }

    [Header("플레이어 ID (NetSession 없어도 됨)")]
    public string playerId = "Player1";

    private bool isActive = false;

    void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    /// <summary>
    /// 로컬 UI/버튼 등으로 비상 정지 발동 시 호출됨
    /// </summary>
    public void ActivateEmergencyStopLocally()
    {
        if (isActive) return;
        isActive = true;
        Debug.LogError($"로컬 비상정지 발동! ID: {playerId}");

        // 1. 네트워크에 전송하도록 알림 (NetSession이 구독)
        OnEmergencyStopActivatedLocally?.Invoke(playerId);

        // 2. FSM에 로컬 비상 정지 신호 전달
        OnEmergencyStopActivated?.Invoke(playerId);
    }

    /// <summary>
    /// 네트워크 메시지 (/emergency/stop) 수신 시 NetSession에서 호출됨
    /// </summary>
    /// <param name="sourceId">비상 정지를 건 플레이어 ID</param>
    public static void HandleNetworkStop(string sourceId)
    {
        // static 메서드이므로 Instance를 통해 로컬 상태를 업데이트
        if (Instance == null || Instance.isActive) return;

        Instance.isActive = true;
        Debug.LogError($"네트워크 비상정지 발동! 출처 ID: {sourceId}");

        // 3. FSM에 네트워크 비상 정지 신호 전달
        OnEmergencyStopActivated?.Invoke(sourceId);
    }

    public void ResetEmergency()
    {
        isActive = false;
        Debug.Log("비상정지 해제");
    }

    [ContextMenu("테스트: 비상정지 발동")]
    void TestActivate() => ActivateEmergencyStopLocally();
}