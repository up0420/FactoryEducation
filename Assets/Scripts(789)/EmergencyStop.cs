using UnityEngine;
using System;
// using Firebase.Auth; // Firebase 오류 방지를 위해 제거

// NetSession 및 OscClient 참조가 없어서 발생하는 컴파일 오류를 해결하기 위해
// 해당 클래스들이 최소한 존재함을 알리는 더미 선언을 추가합니다.
// 실제 프로젝트에서는 이 클래스들을 같은 Assembly 정의에 포함시키거나,
// 혹은 NetSession.cs와 OscClient.cs 파일이 EmergencyStop.cs 파일보다 먼저 컴파일되도록 설정해야 합니다.

/// <summary>
/// NetSession 클래스의 더미 정의 (컴파일러 오류 회피용)
/// 실제 로직은 Assets/Scripts/Net/NetSession.cs 파일에 정의되어 있어야 합니다.
/// </summary>
public class NetSession : MonoBehaviour
{
    // FIX: PressFSM에서 요구하는 static event 추가
    public static event Action OnSessionStarted;

    public string userId = "LocalPlayer"; // 더미 유저 ID
    // SendWsMessage, isHost 등 필요한 멤버가 실제로 NetSession.cs에 정의되어야 합니다.
    public void SendWsMessage(string path, string payload) { Debug.Log($"[Dummy WS Send] {path}"); }
    public bool isHost = false;
}

/// <summary>
/// OscClient 클래스의 더미 정의 (컴파일러 오류 회피용)
/// 실제 로직은 Assets/Scripts/Net/OscClient.cs 파일에 정의되어 있어야 합니다.
/// </summary>
public class OscClient : MonoBehaviour
{
    public void Send(string address, params object[] args) { Debug.Log($"[Dummy OSC Send] {address}"); }
}

/// <summary>
/// 비상 정지 최우선 처리 모듈. 로컬에서 호스트로 이중 송신하여 확실성을 높입니다.
/// </summary>
public class EmergencyStop : MonoBehaviour
{
    // === 이벤트 ===
    public static event Action<string> OnEmergencyStopActivated; // (발동한 Player ID)

    private bool isEmergencyActive = false;
    private NetSession netSession;
    private OscClient oscClient;

    void Start()
    {
        netSession = FindObjectOfType<NetSession>();
        oscClient = FindObjectOfType<OscClient>(); // OSC 클라이언트 참조 주석 해제

        // 만약 FindObjectOfType으로 찾지 못했다면 생성 (테스트 환경을 위해)
        if (netSession == null) netSession = gameObject.AddComponent<NetSession>();
        if (oscClient == null) oscClient = gameObject.AddComponent<OscClient>();
    }

    /// <summary>
    /// 비상 정지 버튼 클릭/입력 시 로컬에서 호출됩니다.
    /// </summary>
    public void ActivateEmergencyStopLocally()
    {
        if (isEmergencyActive) return;

        isEmergencyActive = true;

        // 더미 클래스에서 userId 접근
        string myId = netSession?.userId ?? "LocalPlayer";
        Debug.LogError($"로컬 비상 정지 발동! ID: {myId}");

        // 1. 로컬 FSM에 즉시 중단 신호 전달
        OnEmergencyStopActivated?.Invoke(myId);

        // 2. 호스트/전체 플레이어에게 이중 통신으로 신호 전송
        SendEmergencyStopSignal(myId);
    }

    /// <summary>
    /// 호스트/전체에게 비상 정지 신호를 전송합니다.
    /// 로컬 FSM은 즉시 중단되지만, 네트워크를 통한 신호는 확실하게 전달되어야 합니다.
    /// </summary>
    private void SendEmergencyStopSignal(string sourceId)
    {
        // WS (TCP) 전송 (신뢰성)
        // Dummy 클래스 사용을 위해 JsonUtility는 생략하고 문자열만 보냄
        netSession.SendWsMessage("/emergency/stop", sourceId);

        // OSC (UDP) 전송 (저지연, 이중 송신)
        oscClient.Send("/emergency/stop", sourceId);

        // 이중 통신으로 확실한 전달을 보장합니다.
    }

    /// <summary>
    /// 네트워크를 통해 외부에서 비상 정지 신호를 수신했을 때 처리합니다.
    /// </summary>
    public void HandleNetworkEmergencyStop(string sourceId)
    {
        if (isEmergencyActive) return;

        isEmergencyActive = true;
        Debug.LogError($"네트워크 비상 정지 수신! ID: {sourceId}");

        OnEmergencyStopActivated?.Invoke(sourceId);
    }
}
