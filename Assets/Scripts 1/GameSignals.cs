
using System;

/// <summary>
/// NetSession���� ���Ǵ� ���� �̺�Ʈ ��ȣ �����̳�.
/// ���� ���� ���� ����� �� �̺�Ʈ�� �����մϴ�.
/// </summary>
public static class GameSignals
{

    // Called from NetSession.cs when game session starts
    public static Action SessionStart;

    // Called from NetSession.cs when Emergency Stop occurs
    // Parameters: playerId, reason
    public static Action<string, string> EmergencyStop;

    // Called from NetSession.cs when PPE selection is confirmed
    // Parameters: playerId, correctCount, bonus
    public static Action<string, int, int> PpeConfirmed;

    // Add more game events here if needed
    // Example: public static Action<int> ScoreUpdated;
}

