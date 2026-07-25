namespace PCSMNext.Core.Models;

public class JavaInfo
{
    public string Version { get; set; } = "";
    public string MajorVersion { get; set; } = "";
    public string Path { get; set; } = "";
    public string InstallPath { get; set; } = "";
    public bool IsValid { get; set; }

    public Version? ParsedVersion { get; set; }
    public int MajorVersionNumber { get; set; }
}
