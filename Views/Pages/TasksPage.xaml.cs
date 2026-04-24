using System.Windows;
using System.Windows.Controls;
using GalleryUI.Models;
using GalleryUI.Services;

namespace GalleryUI.Views.Pages;

/// <summary>
/// 任务页面
/// </summary>
public partial class TasksPage
{
    private readonly DownloadTaskManager _taskManager;

    public TasksPage()
    {
        _taskManager = DownloadTaskManager.Instance;
        DataContext = _taskManager;
        InitializeComponent();
    }

    private void OnClearCompletedClick(object sender, RoutedEventArgs e)
    {
        _taskManager.ClearCompleted();
    }

    private void OnCancelTaskClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DownloadTask task)
        {
            task.CancellationTokenSource?.Cancel();
            task.Status = DownloadStatus.Cancelled;
        }
    }

    private void OnRemoveTaskClick(object sender, RoutedEventArgs e)
    {
        if (sender is Button button && button.Tag is DownloadTask task)
        {
            _taskManager.RemoveTask(task);
        }
    }
}
