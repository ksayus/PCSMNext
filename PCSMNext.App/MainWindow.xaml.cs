using PCSMNext.App.ViewModels;
using PCSMNext.App.Views;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Microsoft.Extensions.DependencyInjection;

namespace PCSMNext.App;

public partial class MainWindow : Window
{
    // 当前激活的导航按钮
    private Button? _activeNavButton;

    // 页面缓存（避免重复创建）
    private readonly Dictionary<string, UserControl> _pageCache = new();

    public MainWindow()
    {
        InitializeComponent();

        // 默认显示服务器页面
        NavigateTo("Servers");

        ShowHint("PCSM Next Start!");
    }

    /// <summary>
    /// 导航按钮点击事件
    /// </summary>
    private void NavButton_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button btn)
        {
            NavigateTo((btn.Tag?.ToString()) ?? "");
            SetActiveButton(btn);
        }
    }

    /// <summary>
    /// 切换到指定页面
    /// </summary>
    private void NavigateTo(string pageName)
    {
        UserControl? page = pageName switch
        {
            "Servers" => GetOrCreatePage("Servers", () => new ServerListPage()),
            //"Java" => GetOrCreatePage("Java", () => new JavaPage()),
            //"Console" => GetOrCreatePage("Console", () => new ConsolePage()),
            "Settings" => GetOrCreatePage("Settings", () => new SettingsPage()),
            //"About" => GetOrCreatePage("About", () => new AboutPage()),
            _ => null
        };

        if (page != null)
        {
            UpdatePageDataContext(page, pageName);
            ContentArea.Child = page;
        }
    }

    private void UpdatePageDataContext(UserControl page, string pageName)
    {
        switch (pageName)
        {
            case "Settings":
                var vm1 = App.Services.GetRequiredService<SettingsViewModel>();
                page.DataContext = vm1;
                break;
            case "Servers":
                var vm2 = App.Services.GetRequiredService<ServerListViewModel>();
                page.DataContext = vm2;
                break;
        }
    }

    /// <summary>
    /// 获取或创建页面（带缓存）
    /// </summary>
    private UserControl GetOrCreatePage(string key, Func<UserControl> factory)
    {
        if (!_pageCache.TryGetValue(key, out var page))
        {
            page = factory();
            _pageCache[key] = page;
        }
        return page;
    }

    /// <summary>
    /// 设置当前激活的导航按钮样式
    /// </summary>
    private void SetActiveButton(Button activeBtn)
    {
        if (_activeNavButton != null)
            _activeNavButton.Style = (Style)FindResource("NavButton");

        activeBtn.Style = (Style)FindResource("ActiveNavButton");
        _activeNavButton = activeBtn;
    }

    private void TitleBar_MouseLeftButtonDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ClickCount == 1)
            this.DragMove();
    }

    // 标题栏按钮
    private void Minimize_Click(object sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;
    private void Maximize_Click(object sender, RoutedEventArgs e)
    {
        WindowState = WindowState == WindowState.Maximized
            ? WindowState.Normal : WindowState.Maximized;
    }
    private void Close_Click(object sender, RoutedEventArgs e) => Close();

    // 允许拖动窗口（标题栏区域）
    private void TitleBar_MouseDown(object sender, MouseButtonEventArgs e)
    {
        if (e.ChangedButton == MouseButton.Left)
            DragMove();
    }

    /// <summary>
    /// 显示底部通知
    /// </summary>
    public void ShowHint(string message, string type = "info")
    {
        HintText.Text = message;
        HintBar.Visibility = Visibility.Visible;

        // 根据类型设置颜色
        HintText.Foreground = type switch
        {
            "info" => System.Windows.Media.Brushes.WhiteSmoke,
            "success" => System.Windows.Media.Brushes.LightGreen,
            "error" => System.Windows.Media.Brushes.Red,
            _ => new System.Windows.Media.SolidColorBrush(
                System.Windows.Media.Color.FromRgb(0x5e, 0xea, 0xd4))
        };

        // 5 秒后自动隐藏
        var timer = new System.Windows.Threading.DispatcherTimer
        {
            Interval = TimeSpan.FromSeconds(5)
        };
        timer.Tick += (s, e) =>
        {
            HintBar.Visibility = Visibility.Collapsed;
            timer.Stop();
        };
        timer.Start();
    }
}