using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCSMNext.Core.Models;
using PCSMNext.Services;
using Serilog;
using System.Collections.ObjectModel;

namespace PCSMNext.App.ViewModels;

public partial class ServerListViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly RconService _rconService;

    [ObservableProperty]
    private ObservableCollection<ServerInfo> _servers = new();

    [ObservableProperty]
    private ServerInfo? _selectedServer;

    [ObservableProperty]
    private string _statusMessage = "就绪";

    [ObservableProperty]
    private bool _isBusy;

    public ServerListViewModel(ConfigService configService, RconService rconService)
    {
        _configService = configService;
        _rconService = rconService;
    }

    [RelayCommand]
    private void LoadServers()
    {
        IsBusy = true;
        StatusMessage = "正在加载服务器列表...";

        var servers = _configService.LoadAllServers();
        Servers = new ObservableCollection<ServerInfo>(servers);

        IsBusy = false;
        StatusMessage = $"已加载 {servers.Count} 个服务器";
        Log.Information("加载了 {Count} 个服务器", servers.Count);
    }

    [RelayCommand]
    private async Task StartServer(ServerInfo server)
    {
        StatusMessage = $"正在启动 {server.Name}...";
        IsBusy = true;

        try
        {
            var process = new System.Diagnostics.Process
            {
                StartInfo = new System.Diagnostics.ProcessStartInfo
                {
                    FileName = server.JavaPath,
                    Arguments = $"-Xms{server.MinMemory}M -Xmx{server.MaxMemory}M -jar \"{server.Core}\" nogui",
                    WorkingDirectory = server.Path,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            StatusMessage = $"{server.Name} 已启动 (PID: {process.Id})";
            Log.Information("启动服务器: {Name} PID={Pid}", server.Name, process.Id);
        }
        catch (Exception ex)
        {
            StatusMessage = $"启动失败: {ex.Message}";
            Log.Error(ex, "启动服务器失败: {Name}", server.Name);
        }
        finally
        {
            IsBusy = false;
        }
    }

    [RelayCommand]
    private async Task DeleteServer(ServerInfo server)
    {
        // 实际项目中这里应该弹出确认对话框
        //_configService.DeleteServer(server.Name);
        Servers.Remove(server);
        StatusMessage = $"{server.Name} 已删除";
    }
}