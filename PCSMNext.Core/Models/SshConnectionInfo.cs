namespace PCSMNext.Core.Models;

/// <summary>
/// SSH connect config
/// </summary>
public class SshConnectionInfo
{
    public string Host { get; set; } = "127.0.0.1";
    public int Port { get; set; } = 22;
    public string Username { get; set; } = "root";
    // password or key. to set the authentication-mode for authentication scheme.
    public string AuthMode { get; set; } = "password";
    public string Password { get; set; } = "";
    public string PrivateKeyPath { get; set; } = ""; // private key path
    public int TimeoutSeconds { get; set; } = 10;
}

/// <summary>
/// SSH cmd execute result
/// </summary>
public class SshResult
{
    public bool Success { get; set; }
    public string Output { get; set; } = "";
    public string Error { get; set; } = "";
    public int? ExitCode { get; set; }
}