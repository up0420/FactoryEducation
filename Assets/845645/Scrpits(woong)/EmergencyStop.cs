using UnityEngine;
using System;

public class EmergencyStop : MonoBehaviour
{
    public static event Action<string> OnEmergencyStopActivated;
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

    public void ActivateEmergencyStopLocally()
    {
        if (isActive) return;
        isActive = true;
        Debug.LogError($"비상정지 발동! ID: {playerId}");
        OnEmergencyStopActivated?.Invoke(playerId);
    }

    public void ResetEmergency()
    {
        isActive = false;
        Debug.Log("비상정지 해제");
    }

    [ContextMenu("테스트: 비상정지 발동")]
    void TestActivate() => ActivateEmergencyStopLocally();
}