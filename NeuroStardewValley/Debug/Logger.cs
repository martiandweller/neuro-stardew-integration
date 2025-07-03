namespace NeuroStardewValley.Debug;

using StardewModdingAPI;

public class Logger
{
    private static IMonitor? Monitor;

    public static void SetMonitor(IMonitor monitor)
    {
        Monitor ??= monitor;
    }

    public static void Log(string message, LogLevel level = LogLevel.Info)
    {
        Monitor?.Log(message, level);
    }
    
    public static void Trace(string message)
    {
        Monitor?.Log(message, LogLevel.Trace);
    }

    public static void Debug(string message)
    {
        Monitor?.Log(message, LogLevel.Debug);
    }

    public static void Info(string message)
    {
        Monitor?.Log(message, LogLevel.Info);
    }

    public static void Warning(string message)
    {
        Monitor?.Log(message,LogLevel.Warn);
    }

    public static void Error(string message)
    {
        Monitor?.Log(message, LogLevel.Error);
    }
}