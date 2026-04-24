using System.Collections.ObjectModel;
using System.ComponentModel;
using System.Runtime.CompilerServices;
using GalleryUI.Services;

namespace GalleryUI.Models;

/// <summary>
/// 下载任务模型
/// </summary>
public class DownloadTask : INotifyPropertyChanged
{
    private string _url = string.Empty;
    private string? _fileName;
    private double _progressPercentage;
    private string? _speed;
    private string? _eta;
    private DownloadStatus _status;
    private string? _errorMessage;
    private CancellationTokenSource? _cancellationTokenSource;

    public string Url
    {
        get => _url;
        set { _url = value; OnPropertyChanged(); }
    }

    public string? FileName
    {
        get => _fileName;
        set { _fileName = value; OnPropertyChanged(); }
    }

    public double ProgressPercentage
    {
        get => _progressPercentage;
        set { _progressPercentage = value; OnPropertyChanged(); }
    }

    public string? Speed
    {
        get => _speed;
        set { _speed = value; OnPropertyChanged(); }
    }

    public string? ETA
    {
        get => _eta;
        set { _eta = value; OnPropertyChanged(); }
    }

    public DownloadStatus Status
    {
        get => _status;
        set { _status = value; OnPropertyChanged(); }
    }

    public string? ErrorMessage
    {
        get => _errorMessage;
        set { _errorMessage = value; OnPropertyChanged(); }
    }

    public CancellationTokenSource? CancellationTokenSource
    {
        get => _cancellationTokenSource;
        set { _cancellationTokenSource = value; }
    }

    public event PropertyChangedEventHandler? PropertyChanged;

    protected virtual void OnPropertyChanged([CallerMemberName] string? propertyName = null)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}

/// <summary>
/// 下载任务管理器
/// </summary>
public class DownloadTaskManager
{
    private static readonly Lazy<DownloadTaskManager> _instance = new(() => new DownloadTaskManager());
    public static DownloadTaskManager Instance => _instance.Value;

    public ObservableCollection<DownloadTask> Tasks { get; } = new();

    private DownloadTaskManager() { }

    public DownloadTask AddTask(string url)
    {
        var task = new DownloadTask
        {
            Url = url,
            Status = DownloadStatus.Pending,
            CancellationTokenSource = new CancellationTokenSource()
        };
        Tasks.Add(task);
        return task;
    }

    public void RemoveTask(DownloadTask task)
    {
        task.CancellationTokenSource?.Cancel();
        Tasks.Remove(task);
    }

    public void ClearCompleted()
    {
        var completed = Tasks.Where(t => t.Status == DownloadStatus.Completed || t.Status == DownloadStatus.Cancelled).ToList();
        foreach (var task in completed)
        {
            Tasks.Remove(task);
        }
    }
}
