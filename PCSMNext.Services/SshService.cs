using PCSMNext.Core.Models;
using PCSMNext.Core.Extensions;
using Renci.SshNet;
using Serilog;

namespace PCSMNext.Services;

public class SshService
{
    // Connect cache (avoid each cmd shake hands again).
    private readonly Dictionary<string, SshClient> _clients = new();

    /// <summary>
    /// Get or create SSH connection.
    /// </summary>
    /// <param name="info"></param>
    /// <returns>Connection obj</returns>
    private SshClient GetClient(SshConnectionInfo info)
    {
        var key = $"{info.Username}@{info.Host}:{info.Port}";

        if (_clients.TryGetValue(key, out var client) && client.IsConnected)
            return client;

        // Acording to AuthMode create connect.
        var connectionInfo = info.AuthMode == "key" && !string.IsNullOrEmpty(info.PrivateKeyPath)
            ? new PrivateKeyConnectionInfo(info.Host, info.Port, info.Username,
                new PrivateKeyFile(info.PrivateKeyPath))
            : new ConnectionInfo(info.Host, info.Port, info.Password,
                new PasswordAuthenticationMethod(info.Username, info.Password));

        client = new SshClient(connectionInfo);
        client.Connect();
        _clients[key] = client;

        Log.Information("SSH connected: {Host}:{Port}", info.Host, info.Port);
        return client;
    }

    /// <summary>
    /// Test SSH connection whether can use.
    /// </summary>
    /// <param name="info"></param>
    /// <returns>bool value</returns>
    public async Task<bool> TestConnectionAsync(SshConnectionInfo info)
    {
        try
        {
            using var client = new SshClient(info.Host, info.Port, info.Username, info.Password);
            await Task.Run(() => client.Connect());
            var result = client.IsConnected;
            return result;
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed SSH connect: {Host}:{Port}", info.Host, info.Port);
            return false;
        }
    }

    /// <summary>
    /// Execute remote command.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="command"></param>
    /// <param name="timeoutMs"></param>
    /// <returns>SSH result obj</returns>
    public async Task<SshResult> ExecuteCommandAsync(
        SshConnectionInfo info, string command, int timeoutMs = 3000)
    {
        try
        {
            using var client = new SshClient(info.Host, info.Port, info.Username, info.Password);
            await Task.Run(() => client.Connect());

            using var cmd = client.CreateCommand(command);
            cmd.CommandTimeout = TimeSpan.FromMilliseconds(timeoutMs);

            var output = await Task.Run(() => cmd.Execute());

            Log.Debug("SSH command: {Cmd} -> {Result}",
                command.Truncate(100), output.Truncate(100));

            return new SshResult
            {
                Success = cmd.ExitStatus == 0,
                Output = output,
                Error = cmd.Error,
                ExitCode = cmd.ExitStatus
            };
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "Failed execute SSH command: {Host} {Cmd}", info.Host, command);
            return new SshResult { Success = false, Error = ex.Message };
        }
    }

    /// <summary>
    /// Get remote system infomation.
    /// </summary>
    /// <param name="info"></param>
    /// <returns></returns>
    public async Task<RemoteSystemInfo> GetSystemInfoAsync(SshConnectionInfo info)
    {
        var sysInfo = new RemoteSystemInfo();

        // Parallel execute mutiple inquiries command.
        var tasks = new Dictionary<string, Task<SshResult>>
        {
            ["os"] = ExecuteCommandAsync(info, "cat /etc/os-release | grep PRETTY_NAME | cut -d= -f2 | tr -d '\"'"),
            ["kernel"] = ExecuteCommandAsync(info, "uname -r"),
            ["cpu"] = ExecuteCommandAsync(info, "top -bn1 | grep 'Cpu(s)' | awk '{print $2+$4\"%\"}'"),
            ["mem"] = ExecuteCommandAsync(info, "free -h | grep Mem | awk '{print $3\"/\"$2}'"),
            ["disk"] = ExecuteCommandAsync(info, "df -h / | tail -1 | awk '{print $3\"/\"$2}'"),
            ["java"] = ExecuteCommandAsync(info, "java -version 2>&1 | head -1"),
            ["uptime"] = ExecuteCommandAsync(info, "uptime -p | cut -d' ' -f2-")
        };

        await Task.WhenAll(tasks.Values);

        sysInfo.OsName = tasks["os"].Result.Output.Trim();
        sysInfo.KernelVersion = tasks["kernel"].Result.Output.Trim();
        sysInfo.CpuUsage = tasks["cpu"].Result.Output.Trim();
        sysInfo.MemoryUsage = tasks["mem"].Result.Output.Trim();
        sysInfo.DiskUsage = tasks["disk"].Result.Output.Trim();
        sysInfo.JavaVersion = tasks["java"].Result.Output.Trim();
        sysInfo.Uptime = tasks["uptime"].Result.Output.Trim();

        return sysInfo;
    }

    /// <summary>
    /// Start remote Minecraft server.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="server"></param>
    /// <returns></returns>
    public async Task<SshResult> StartMinecraftServerAsync(
        SshConnectionInfo info, ServerInfo server)
    {
        // Build start command.
        var command = $"""
            cd "{server.RemoteServerPath}"
            screen -dmS mcserver java -Xms{server.MinMemory}M -Xmx{server.MaxMemory}M -jar "{server.Core}" nogui
            echo "Server started in screen session 'mcserver'"
            """;

        var result = await ExecuteCommandAsync(info, command);

        if (result.Success)
        {
            Log.Information("The remote server started: {Name} @ {Host}",
                server.Name, info.Host);
        }

        return result;
    }

    /// <summary>
    /// Read remote log file.
    /// </summary>
    /// <param name="info"></param>
    /// <param name="logPath"></param>
    /// <param name="lines"></param>
    /// <returns></returns>
    public async Task<SshResult> ReadLogAsync(
        SshConnectionInfo info, string logPath, int lines = 100)
    {
        return await ExecuteCommandAsync(info, $"tail -n {lines} \"{logPath}\"");
    }

    /// <summary>
    /// Upload file to remote server.
    /// </summary>
    public async Task<bool> UploadFileAsync(
    SshConnectionInfo info, string localPath, string remotePath)
    {
        try
        {
            using var sftp = new SftpClient(info.Host, info.Port, info.Username, info.Password);
            await Task.Run(() =>
            {
                sftp.Connect();
                using var fileStream = File.OpenRead(localPath);
                sftp.UploadFile(fileStream, remotePath, true);
            });

            Log.Information("Files uploaded: {Local} -> {Remote}@{Host}",
                localPath, remotePath, info.Host);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to upload files: {Local} -> {Host}", localPath, info.Host);
            return false;
        }
    }

    // <summary>
    /// Domnload file from remote server.
    /// </summary>
    public async Task<bool> DownloadFileAsync(SshConnectionInfo info, string remotePath, string localPath)
    {
        try
        {
            // 确保本地目录存在
            var dir = Path.GetDirectoryName(localPath);
            if (!string.IsNullOrEmpty(dir))
                Directory.CreateDirectory(dir);

            // 直接使用 SftpClient，避免依赖 SshClient
            await Task.Run(() =>
            {
                using var sftp = new SftpClient(info.Host, info.Port, info.Username, info.Password);
                sftp.Connect();
                using var fileStream = File.Create(localPath);
                sftp.DownloadFile(remotePath, fileStream);
            });

            Log.Information("Files downloaded: {Remote}@{Host} -> {Local}", remotePath, info.Host, localPath);
            return true;
        }
        catch (Exception ex)
        {
            Log.Error(ex, "Failed to download files: {Remote}@{Host}", remotePath, info.Host);
            return false;
        }
    }

    /// <summary>
    /// Release all connection
    /// </summary>
    public void DisposeAll()
    {
        foreach (var (key, client) in _clients)
        {
            if (client.IsConnected)
                client.Disconnect();
            client.Dispose();
        }
        _clients.Clear();
        Log.Information("All SSH connection released.");
    }
}
