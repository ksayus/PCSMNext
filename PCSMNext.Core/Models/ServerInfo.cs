namespace PCSMNext.Core.Models;

public class ServerInfo
{
    // basic infomation
    public string Name { get; set; } = "";
    public string Version { get; set; } = "";
    public string Core { get; set; } = "";
    public string CoreType { get; set; } = "N/A";
    public string Path { get; set; } = "";
    public string StartBatchPath { get; set; } = "";

    // Java config
    public string JavaVersion { get; set; } = "";
    public string JavaPath { get; set; } = "";
    public int MinMemory { get; set; } = 1024;
    public int MaxMemory { get; set; } = 2048;

    // server port
    public int ServerPort { get; set; } = 25565;
    public int RCONPort { get; set; } = 25575;
    public string RCONPassword { get; set; } = "";

    // run status
    public bool AutoStart { get; set; } = false;
    public int Counts { get; set; } = 0;
    public long Size { get; set; } = 0;
    public DateTime? CreatedTime { get; set; } = DateTime.Now;
    public DateTime? LatestStartedTime { get; set; } = DateTime.Now;

    // SSH connect config
    public bool IsRemote { get; set; } = false;
    public string RemoteHost { get; set; } = "";
    public int RemotePort { get; set; } = 22;
    public string RemoteUsername { get; set; } = "root";
    public string RemoteAuthMode { get; set; } = "password";
    public string RemotePassword { get; set; } = "";
    public string RemoteKeyPath { get; set; } = "";
    // the path in remote server
    public string RemoteJavaPath { get; set; } = "";
    public string RemoteServerPath { get; set; } = "";

    // Computed properties, runtime calculation
    public string LaunchCmd =>
        $"\"{JavaPath}\" -Xms{MinMemory}M -Xmx{MaxMemory}M -jar {Core} nogui";

    // Compatible with old field names (migrated from PCSMT-2)
    // These properties will not be serialized, only mapped during migration
    [System.Text.Json.Serialization.JsonIgnore]
    public Dictionary<string, string> LegacyFieldMap => new()
    {
        { "server_name", "Name" },
        { "start_count", "Counts" },
        { "server_core", "Core" },
        { "server_path", "Path" },
        { "server_start_batch_path", "StartBatchPath" },
        { "server_version", "Version" },
        { "server_size", "Size" },
    };
}