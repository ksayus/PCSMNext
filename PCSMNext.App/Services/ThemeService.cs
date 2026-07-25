using Serilog;
using PCSMNext.Services;
using System.Windows;
using System.Windows.Media;
using LAE;
using Microsoft.Windows.Themes;
using System.Collections;

namespace PCSMNext.App.Services;

public class ThemeService
{
    private readonly ConfigService _configService;

    public enum ThemeName
    {
        DefaultTheme,
        DarkTheme,
        LightTheme,
    }
    // 支持的主题列表
    public static readonly string[] AvailableTheme = { ThemeName.DefaultTheme.ToString(), ThemeName.DarkTheme.ToString(),
                                                        ThemeName.LightTheme.ToString()};

    public ThemeService(ConfigService configService)
    {
        _configService = configService;
    }

    /// <summary>
    /// 获取当前主题名称
    /// </summary>
    /// <returns></returns>
    public ThemeName GetCurrentTheme()
    {
        var configTheme = _configService.GetAppConfig().App.Theme;
        ThemeName theme = ThemeName.DefaultTheme;

        theme = GetTheme(configTheme);

        return theme;
    }

    public void ApplyTheme(ThemeName themeName)
    {
        var appResources = Application.Current.Resources;

        // 找到并移除当前的主题字典
        var currentTheme = appResources.MergedDictionaries
            .FirstOrDefault(d => d.Source?.OriginalString.Contains("Theme") == true);

        if (currentTheme != null)
            appResources.MergedDictionaries.Remove(currentTheme);

        // 加载新主题
        var newTheme = GetThemeResource(themeName);

        appResources.MergedDictionaries.Add(newTheme);

        Log.Information("Now is change theme to {Theme}", themeName);
    }

    /// <summary>
    /// 应用启动时加载已保存的主题
    /// </summary>
    public void LoadSavedTheme()
    {
        var theme = GetCurrentTheme();
        ApplyTheme(theme);
    }

    /// <summary>
    /// 获取主题名的枚举类型
    /// </summary>
    /// <param name="themeName">主题名(字符串)</param>
    /// <returns></returns>
    public static ThemeName GetTheme(string themeName)
    {
        ThemeName theme = ThemeName.DefaultTheme;

        if (Enum.TryParse<ThemeName>(themeName, true, out ThemeName result))
            theme = result;

        return theme;
    }
    /// <summary>
    /// 获取主题文件
    /// </summary>
    /// <param name="themeName">主题名(枚举)</param>
    /// <returns></returns>
    private static ResourceDictionary GetThemeResource(ThemeName themeName)
    {
        var newTheme = new ResourceDictionary
        {
            Source = new Uri($"Themes/{themeName.ToString()}.xaml", UriKind.Relative)
        };
        return newTheme;
    }
}
