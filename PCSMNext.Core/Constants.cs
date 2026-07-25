namespace PCSMNext.Core;

public static class Constants
{
    // program info
    public const string AppName = "PCSM Next";
    public const string AppVersion = "1.0.0";

    // file path
    public static readonly string AppDataFolder = Path.Combine(Environment.CurrentDirectory, "PCSMNext");
    public static readonly string ConfigFolder = Path.Combine(AppDataFolder, "Config");
    public static readonly string LogsFolder = Path.Combine(AppDataFolder, "Logs");
    public static readonly string ServersFolder = Path.Combine(AppDataFolder, "Servers");

    // file name
    public static string AppSettingsFile = "appsettings.json";
    public static string ServerInfoFile = "server.json";
    public static string LogFile = "pcsm-.log";

    // Mapping table from Minecraft versions to JDK versions
    public static readonly Dictionary<string, string> MinecraftJavaMapping = new()
    {
        { "1.16.5", "8" },
        { "1.17.0", "16" },
        { "1.18.2", "16" },
        { "1.18.3", "17" },
        { "1.20.4", "17" },
        { "1.20.5", "21" },
        { "1.21.0", "21" },
    };
    // Range of Minecraft versions compatible with the same JDK version
    public static readonly Dictionary<string, (string Min, string Max)> JavaVersionRanges = new()
    {
        { "8",  ("1.0.0",  "1.16.5") },
        { "16", ("1.17.0", "1.18.2") },
        { "17", ("1.18.3", "1.20.4") },
        { "21", ("1.20.5", "1.21.8") },
    };
}
