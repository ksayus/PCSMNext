using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using PCSMNext.App.Services;
//using Microsoft.Extensions.DependencyInjection;
using PCSMNext.Core.Models;
using PCSMNext.Services;
using Serilog;
using System.Collections.ObjectModel;

namespace PCSMNext.App.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ConfigService _configService;
    private readonly ThemeService _themeService;

    private AppConfig _currentConfig = new();

    private string _selectedTheme;
    private string _selectedUpdateSources;
    private bool _checkAutoUpdate;
    private bool _checkStartup;

    public string SelectedTheme
    {
        get => _selectedTheme;
        set
        {
            if (SetProperty(ref _selectedTheme, value))
            {
                _currentConfig.App.Theme = value;

                var themeEnum = ThemeService.GetTheme(value);
                _themeService.ApplyTheme(themeEnum);

                SaveSettings();

                StatusMessage = $"主题已切换为{value}";
                Log.Information("Theme changed to {Theme}", value);
            }
        }
    }

    public string SelectedUpdateSources
    {
        get => _selectedUpdateSources;
        set
        {
            if(SetProperty(ref _selectedUpdateSources, value))
            {
                _currentConfig.App.AutoUpdateSource = value;

                SaveSettings();

                StatusMessage = $"更新源已切换为{value}";
                Log.Information($"Updated update sources: {value}");
            }
        }
    }

    public bool CheckAutoUpdate
    {
        get => _checkAutoUpdate;
        set
        {
            if (SetProperty(ref _checkAutoUpdate, value))
            {
                _currentConfig.App.AutoUpdate = value;

                SaveSettings() ;

                StatusMessage = $"自动更新{value}";
                Log.Information($"Check auto update: {value}");
            }
        }
    }

    public bool CheckStartup
    {
        get => _checkStartup;
        set
        {
            if (SetProperty(ref _checkStartup, value))
            {
                _currentConfig.App.AutoStart = value;

                SaveSettings() ;

                StatusMessage = $"自启动{value}";
                Log.Information($"Check startup: {value}");
            }
        }
    }


    public AppConfig CurrentConfig => _currentConfig;

    // 可选主题列表
    public ObservableCollection<string> Themes { get; } = new(ThemeService.AvailableTheme);

    // 可选更新源
    public ObservableCollection<string> UpdateSources { get; } = new() { "Github", "Gitee" };

    // 状态消息
    [ObservableProperty]
    private string _statusMessage = "";

    public SettingsViewModel(ConfigService configService, ThemeService themeService)
    {
        _configService = configService;
        _themeService = themeService;

        _currentConfig = _configService.GetAppConfig();
        _selectedTheme = _currentConfig.App.Theme;
        _selectedUpdateSources = _currentConfig.App.AutoUpdateSource;
    }

    private void SaveSettings()
    {
        _configService.SaveAppConfig(_currentConfig);
        StatusMessage = "The settings is saved";
        Log.Information("Settings saved maually.");
    }

    [RelayCommand]
    private void ResetSettings()
    {
        _currentConfig = new AppConfig();
        _configService.SaveAppConfig(_currentConfig);

        SelectedTheme = _currentConfig.App.Theme;

        StatusMessage = "设置已重置为默认值";

        Log.Information("The user reset the setting to default value.");
    }
}