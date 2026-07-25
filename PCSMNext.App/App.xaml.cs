using PCSMNext.Services;
using PCSMNext.App.Services;
using PCSMNext.Core;
using Serilog;
using System.IO;
using System.Windows;
using Microsoft.Extensions.DependencyInjection;
using PCSMNext.App.ViewModels;

namespace PCSMNext.App;

/// <summary>
/// Interaction logic for App.xaml
/// </summary>
public partial class App : Application
{
    // 全局 DI 容器
    public static IServiceProvider Services { get; private set; } = null!;

    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        // 检查目录
        Directory.CreateDirectory(Constants.ConfigFolder);
        Directory.CreateDirectory(Constants.LogsFolder);
        Directory.CreateDirectory(Constants.ServersFolder);

        // 配置Serilog
        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .WriteTo.Console()
            .WriteTo.File(
                Path.Combine(Constants.LogsFolder, Constants.LogFile),
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7)
            .CreateLogger();

        Log.Information("PCSM Next 启动, 版本{Version}", Constants.AppVersion);

        // 初始化配置
        var configService = new ConfigService();
        var configPath = Path.Combine(Constants.ConfigFolder, Constants.AppSettingsFile);

        // 配置依赖注入
        var services = new ServiceCollection();

        // 注册服务
        services.AddSingleton<ConfigService>();
        services.AddSingleton<JavaService>();
        services.AddSingleton<RconService>();
        services.AddSingleton<SshService>();
        services.AddSingleton<TimerService>();
        services.AddSingleton<ThemeService>();

        // 注册 ViewModel (每次请求创建新实例)
        services.AddTransient<SettingsViewModel>();
        services.AddTransient<ServerListViewModel>();

        Services = services.BuildServiceProvider();

        // 加载主题
        var themeService = new ThemeService(configService);
        themeService.LoadSavedTheme();

        // 启动主窗口
        //var mainWindow = new MainWindow();
        //mainWindow.Show();
    }

    protected override void OnExit(ExitEventArgs e)
    {
        Log.Information("PCSM Next exit.");
        Log.CloseAndFlush();
        base.OnExit(e);
    }
}
