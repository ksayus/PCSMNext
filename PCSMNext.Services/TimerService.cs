using PCSMNext.Core.Models;
using Serilog;

namespace PCSMNext.Services;

public class TimerService
{
    private readonly ConfigService _configService;
    private readonly RconService _rconService;

    // 取消令牌，对标 PCSMT-2 的 threading.Event
    private CancellationTokenSource? _storageCts;
    private CancellationTokenSource? _heartbeatCts;

    // 心跳缓存，对标 PCSMT-2 timer.py 的 Heartbeat.List
    public Dictionary<string, PlayerList> HeartbeatCache { get; } = new();

    public TimerService(ConfigService configService, RconService rconService)
    {
        _configService = configService;
        _rconService = rconService;
    }

    /// <summary>
    /// 启动存储监控定时器
    /// </summary>
    public void StartStorageMonitor(List<ServerInfo> servers)
    {
        _storageCts = new CancellationTokenSource();
        var config = _configService.GetAppConfig();

        Task.Run(async () =>
        {
            while (!_storageCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(
                        config.Timer.StorageCheckIntervalSeconds * 1000,
                        _storageCts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                foreach (var server in servers)
                {
                    if (Directory.Exists(server.Path))
                    {
                        var size = GetDirectorySize(server.Path);
                        server.Size = size;
                        Log.Debug("存储扫描: {Name} = {Size}MB",
                            server.Name, size / 1024 / 1024);
                    }
                }
            }
        }, _storageCts.Token);

        Log.Information("存储监控已启动，间隔 {Interval} 秒",
            config.Timer.StorageCheckIntervalSeconds);
    }

    /// <summary>
    /// 启动心跳轮询
    /// 对标 PCSMT-2 的 Heartbeat 类
    /// </summary>
    public void StartHeartbeat(List<ServerInfo> servers)
    {
        _heartbeatCts = new CancellationTokenSource();
        var config = _configService.GetAppConfig();

        Task.Run(async () =>
        {
            while (!_heartbeatCts.Token.IsCancellationRequested)
            {
                try
                {
                    await Task.Delay(
                        config.Timer.HeartbeatIntervalSeconds * 1000,
                        _heartbeatCts.Token);
                }
                catch (TaskCanceledException)
                {
                    break;
                }

                foreach (var server in servers)
                {
                    var players = await _rconService.GetOnlinePlayersAsync(
                        "127.0.0.1", server.RCONPort, server.RCONPassword);

                    // 原子更新缓存
                    lock (HeartbeatCache)
                    {
                        HeartbeatCache[server.Name] = players;
                    }
                }
            }
        }, _heartbeatCts.Token);

        Log.Information("心跳轮询已启动，间隔 {Interval} 秒",
            config.Timer.HeartbeatIntervalSeconds);
    }

    /// <summary>
    /// 停止所有定时器
    /// </summary>
    public void StopAll()
    {
        _storageCts?.Cancel();
        _heartbeatCts?.Cancel();
        Log.Information("所有定时器已停止");
    }

    /// <summary>
    /// 计算目录大小
    /// </summary>
    private static long GetDirectorySize(string path)
    {
        try
        {
            return new DirectoryInfo(path)
                .EnumerateFiles("*", SearchOption.AllDirectories)
                .Sum(f => { try { return f.Length; } catch { return 0; } });
        }
        catch (Exception ex)
        {
            Log.Warning(ex, "计算目录大小失败: {Path}", path);
            return 0;
        }
    }
}