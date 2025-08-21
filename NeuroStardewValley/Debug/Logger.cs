namespace NeuroStardewValley.Debug;

using StardewModdingAPI;

public static class Logger
{
    private static IMonitor? _monitor;

    public static void SetMonitor(IMonitor monitor)
    {
        _monitor ??= monitor;
    }

    public static void Log(string message, LogLevel level = LogLevel.Info)
    {
        _monitor?.Log(message, level);
    }
    
    public static void Trace(string message)
    {
        _monitor?.Log(message, LogLevel.Trace);
    }

    public static void Debug(string message)
    {
        _monitor?.Log(message, LogLevel.Debug);
    }

    public static void Info(string message)
    {
        _monitor?.Log(message, LogLevel.Info);
    }

    public static void Warning(string message)
    {
        _monitor?.Log(message,LogLevel.Warn);
    }

    public static void Error(string message)
    {
        _monitor?.Log(message, LogLevel.Error);
    }
}