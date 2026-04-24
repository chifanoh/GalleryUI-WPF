using System.ComponentModel;
using System.IO;
using System.Runtime.CompilerServices;

namespace GalleryUI.Services;

/// <summary>
/// 下载配置服务（单例）
/// </summary>
public class DownloadConfigService : INotifyPropertyChanged
{
    private static readonly Lazy<DownloadConfigService> _instance = new(() => new DownloadConfigService());
    public static DownloadConfigService Instance => _instance.Value;

    private string _downloadPath = string.Empty;

    private DownloadConfigService()
    {
        _downloadPath = LoadDownloadPath();
    }

    /// <summary>
    /// 下载路径
    /// </summary>
    public string DownloadPath
    {
        get => _downloadPath;
        set
        {
            if (_downloadPath != value)
            {
                _downloadPath = value;
                SaveDownloadPath(value);
                OnPropertyChanged();
            }
        }
    }

    /// <summary>
    /// 加载下载路径设置
    /// </summary>
    private static string LoadDownloadPath()
    {
        try
        {
            var configPath = GetConfigPath();
            if (File.Exists(configPath))
            {
                var json = File.ReadAllText(configPath);
                var config = System.Text.Json.JsonSerializer.Deserialize<DownloadConfig>(json);
                if (!string.IsNullOrEmpty(config?.DownloadPath))
                {
                    // 确保目录存在
                    if (!Directory.Exists(config.DownloadPath))
                    {
                        Directory.CreateDirectory(config.DownloadPath);
                    }
                    return config.DownloadPath;
                }
            }
        }
        catch
        {
            // 读取失败使用默认路径
        }

        // 默认路径
        var defaultPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
            "Downloads",
            "GalleryDownloads");
        
        if (!Directory.Exists(defaultPath))
        {
            Directory.CreateDirectory(defaultPath);
        }
        
        return defaultPath;
    }

    /// <summary>
    /// 保存下载路径设置
    /// </summary>
    private static void SaveDownloadPath(string path)
    {
        try
        {
            var configPath = GetConfigPath();
            var config = new DownloadConfig { DownloadPath = path };
            var json = System.Text.Json.JsonSerializer.Serialize(config);
            File.WriteAllText(configPath, json);
        }
        catch
        {
            // 保存失败忽略
        }
    }

    /// <summary>
    /// 获取配置文件路径
    /// </summary>
    private static string GetConfigPath()
    {
        var appData = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "GalleryUI");
        if (!Directory.Exists(appData))
        {
            Directory.CreateDirectory(appData);
        }
        return Path.Combine(appData, "config.json");
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 下载配置
/// </summary>
public class DownloadConfig
{
    public string? DownloadPath { get; set; }
}
