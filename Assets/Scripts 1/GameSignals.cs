using System;

public static class GameSignals
{
    public static Action SessionStart;
    public static Action<string, string> EmergencyStop;
}