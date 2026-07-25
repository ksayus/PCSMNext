using Microsoft.Extensions.DependencyInjection;
using PCSMNext.App.ViewModels;
using System.Windows.Controls;

namespace PCSMNext.App.Views;

public partial class ServerListPage : UserControl
{
    public ServerListPage()
    {
        InitializeComponent();

        // 从 DI 容器获取 ViewModel
        // 注意：页面在 MainWindow 的缓存中创建，所以这里不用
        // GetRequiredService，而是让 MainWindow 来注入
        DataContext = App.Services.GetRequiredService<ServerListViewModel>();

        // 加载时自动刷新列表
        Loaded += (s, e) =>
        {
            if (DataContext is ServerListViewModel vm)
            {
                vm.LoadServersCommand.Execute(null);
            }
        };
    }
}