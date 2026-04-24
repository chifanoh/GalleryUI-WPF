using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using GalleryUI.Models;
using GalleryUI.Services;

namespace GalleryUI.Views.Pages;

/// <summary>
/// 下载页面
/// </summary>
public partial class HomePage
{
    private readonly GalleryDlService _galleryDlService;
    private readonly DownloadTaskManager _taskManager;

    public HomePage()
    {
        _taskManager = DownloadTaskManager.Instance;
        DataContext = _taskManager;

        // 初始化下载服务，使用单例配置服务的路径
        var configService = DownloadConfigService.Instance;
        _galleryDlService = new GalleryDlService(configService.DownloadPath);

        InitializeComponent();

        // 检查 gallery-dl 环境
        CheckEnvironment();
    }

    private void CheckEnvironment()
    {
        if (!_galleryDlService.IsAvailable())
        {
            EnvironmentInfoBar.IsOpen = true;
            DownloadButton.IsEnabled = false;
        }
        else
        {
            EnvironmentInfoBar.IsOpen = false;
            DownloadButton.IsEnabled = true;
        }
    }

    private void OnDownloadButtonClick(object sender, RoutedEventArgs e)
    {
        // 再次检查环境
        if (!_galleryDlService.IsAvailable())
        {
            ShowMessage("未找到 gallery-dl.exe，无法下载");
            return;
        }

        var url = UrlTextBox.Text.Trim();
        if (string.IsNullOrEmpty(url))
        {
            ShowMessage("请输入漫画链接");
            return;
        }

        StartDownloadTask(url);
    }

    private void OnClearButtonClick(object sender, RoutedEventArgs e)
    {
        UrlTextBox.Clear();
    }

    private async void StartDownloadTask(string url)
    {
        // 显示加载动画并禁用按钮
        DownloadButton.IsEnabled = false;
        DownloadButtonContent.Visibility = Visibility.Collapsed;
        DownloadProgressRing.Visibility = Visibility.Visible;

        var task = _taskManager.AddTask(url);

        var progress = new Progress<DownloadProgress>(p =>
        {
            task.ProgressPercentage = p.ProgressPercentage;
            task.Speed = p.Speed;
            task.ETA = p.ETA;
            task.FileName = p.FileName;
            task.Status = p.Status;
            task.ErrorMessage = p.ErrorMessage;
        });

        try
        {
            await _galleryDlService.DownloadAsync(url, progress, task.CancellationTokenSource?.Token ?? CancellationToken.None);
        }
        catch (OperationCanceledException)
        {
            task.Status = DownloadStatus.Cancelled;
        }
        catch (Exception ex)
        {
            task.Status = DownloadStatus.Error;
            task.ErrorMessage = ex.Message;
        }
        finally
        {
            // 隐藏加载动画并启用按钮
            DownloadProgressRing.Visibility = Visibility.Collapsed;
            DownloadButtonContent.Visibility = Visibility.Visible;
            DownloadButton.IsEnabled = true;
        }
    }

    private void ShowMessage(string message)
    {
        Debug.WriteLine(message);
        MessageBox.Show(message, "提示", MessageBoxButton.OK, MessageBoxImage.Information);
    }
}
