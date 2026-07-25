namespace PCSMNext.Core.Models;


/// <summary>
/// program config
/// </summary>
public class AppConfig
{
    public AppSection App { get; set; } = new();
    public LoggingSection Logging { get; set; } = new();
    public RconSection RCON { get; set; } = new();
    public TimerSection Timer { get; set; } = new();
}

public class AppSection
{
    public bool AutoUpdate { get; set; } = true;
    public string AutoUpdateSource { get; set; } = "Github";
    public bool AutoStart { get; set; } = true;
    public string Theme { get; set; } = "Default";
}

public class LoggingSection
{
    public string Level { get; set; } = "Information";
    public int RetainDays { get; set; } = 7;
}

public class RconSection
{
    public string DefaultPassword { get; set; } = "";
    public int DefaultPort { get; set; } = 25565;
    public int TimeoutSeconds { get; set; } = 5;
}

public class TimerSection
{
    public int StorageCheckIntervalSeconds { get; set; } = 3600;
    public int HeartbeatIntervalSeconds { get; set; } = 4;
}