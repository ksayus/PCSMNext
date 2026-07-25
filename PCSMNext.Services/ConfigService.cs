using Microsoft.Extensions.Configuration;
using System.Text.Json;
using PCSMNext.Core;
using PCSMNext.Core.Models;
using Serilog;

namespace PCSMNext.Services;

public class ConfigService
{
    private readonly IConfiguration _configuration;
    private readonly string _configFilePath;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        WriteIndented = true, // 格式化 JSON (缩进)
        PropertyNameCaseInsensitive = true, // 不区分大小写匹配属性
        Encoder = System.Text.Encodings.Web.JavaScriptEncoder.UnsafeRelaxedJsonEscaping
        //允许中文不被转义成 \uXXXX
    };

    public ConfigService()
    {
        // 确保配置目录存在
        Directory.CreateDirectory(Constants.ConfigFolder);

        _configFilePath = Path.Combine(Constants.ConfigFolder, Constants.AppSettingsFile);

        // 如果配置文件不存在, 自动生成默认配置
        if (!File.Exists(_configFilePath))
        {
            var defaultConfig = new AppConfig();
            var json = JsonSerializer.Serialize(defaultConfig);
            File.WriteAllText(_configFilePath, json);
        }

        // 构建 .NET 配置框架
        _configuration = new ConfigurationBuilder()
            .AddJsonFile(_configFilePath, optional: false, reloadOnChange: true)
            .Build();
    }

    /// <summary>
    /// 读取配置（强类型返回）
    /// </summary>
    public AppConfig GetAppConfig()
    {
        var config = new AppConfig();
        _configuration.Bind(config);
        return config;
    }

    /// <summary>
    /// 更新并保存配置
    /// </summary>
    public void SaveAppConfig(AppConfig config)
    {
        var json = JsonSerializer.Serialize(config, JsonOptions);
        File.WriteAllText(_configFilePath, json);

        // reloadOnChange: true 会自动检测文件变化
        // 因此不需要手动刷新
    }

    /// <summary>
    /// 读取单个配置值
    /// </summary>
    public string GetValue(string key, string defaultValue = "")
    {
        return _configuration[key] ?? defaultValue;
    }

    /// <summary>
    /// 配置迁移：检查并修复配置文件的完整性
    /// 对标 PCSMT-2 Init.py 的 Infomation.Config() 方法
    /// </summary>
    public void MigrateConfig()
    {
        var config = GetAppConfig();
        var needsSave = false;

        // 检测每个字段, 缺失或非法则自动修复

        // 更新源
        var validSources = new[] { "Github", "Gitee" };
        if (!validSources.Contains(config.App.AutoUpdateSource))
        {
            Log.Warning("AutoUpdateSource 值非法: {Value}, 重置为 Github", config.App.AutoUpdateSource);
            config.App.AutoUpdateSource = "Github";
            needsSave = true;
        }

        // RCON端口
        if (config.RCON.DefaultPort < 1 || config.RCON.DefaultPort > 65536)
        {
            Log.Warning("RCON 端口非法: {Port}, 重置为 25575", config.RCON.DefaultPort);
            config.RCON.DefaultPort = 25575;
            needsSave = true;
        }

        // 检测是否要保存
        if (needsSave)
        {
            SaveAppConfig(config);
            Log.Information("配置已自动修复");
        }
    }

    /// <summary>
    /// 加载所有服务器配置
    /// </summary>
    public List<ServerInfo> LoadAllServers()
    {
        var servers = new List<ServerInfo>();
        if (!Directory.Exists(Constants.ServersFolder))
            return servers;

        foreach (var dir in Directory.GetDirectories(Constants.ServersFolder))
        {
            var serverFile = Path.Combine(dir, Constants.ServerInfoFile);
            if (!File.Exists(serverFile)) continue;

            try
            {
                var json = File.ReadAllText(serverFile);
                var server = JsonSerializer.Deserialize<ServerInfo>(json, JsonOptions);
                if (server != null)
                {
                    servers.Add(server);
                }
            }
            catch(Exception ex)
            {
                Log.Warning(ex, "加载服务器配置失败: {Path}", serverFile);
            }
        }

        return servers;
    }

    /// <summary>
    /// 保存单个服务器配置
    /// </summary>
    public void SaveServer(ServerInfo server)
    {
        var serverDir = Path.Combine(Constants.ServersFolder, server.Name);
        Directory.CreateDirectory(serverDir);

        var json = JsonSerializer.Serialize(server, JsonOptions);
        var path = Path.Combine(serverDir, Constants.ServerInfoFile);
        File.WriteAllText(path, json);
        Log.Information("服务器配置已保存: {Name} -> {Path}", server.Name, path);
    }
}