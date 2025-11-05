// 이 스크립트는 다른 스크립트들이 공통으로 사용할 이벤트를 정의합니다.

using System;

public static class GameSignals
{
    // NetSession.cs (59번째 줄)에서 호출됨
    // 게임 세션이 시작될 때 이벤트를 알림
    public static Action SessionStart;

    // NetSession.cs (80번째 줄)에서 호출됨
    // 비상 정지(Emergency Stop)가 발생했을 때, 
    // 정지를 요청한 ID와 이유(reason)를 함께 전달
    public static Action<string, string> EmergencyStop;

    // 필요하다면 여기에 다른 게임 이벤트를 추가할 수 있습니다.
    // 예시: public static Action<int> ScoreUpdated;
}