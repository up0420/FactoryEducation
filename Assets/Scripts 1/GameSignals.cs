using System;

/// <summary>
/// NetSession에서 사용되는 전역 이벤트 신호 컨테이너.
/// 실제 게임 로직 모듈은 이 이벤트를 구독합니다.
/// </summary>
public static class GameSignals
{
    // NetSession에서 /session/start 메시지 수신 시 호출됨
    public static event Action SessionStart;

    // NetSession에서 /emergency/stop 메시지 수신 시 호출됨
    public static event Action<string, string> EmergencyStop; // ID, Reason
}

// 이 파일은 NetSession.cs와 다른 모듈 간의 연결고리 역할을 합니다.