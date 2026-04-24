using System.Diagnostics;
using System.IO;
using System.Text.RegularExpressions;

namespace GalleryUI.Services;

/// <summary>
/// gallery-dl 下载服务
/// </summary>
public partial class GalleryDlService
{
    private readonly string _galleryDlPath;
    private readonly DownloadConfigService _configService;

    // 预编译的正则表达式，提高性能
    private static readonly Regex ProgressRegex = new(
        @"\[download\]\s+(\d+\.?\d*)%\s+of\s+(\S+)\s+at\s+(\S+)\s+ETA\s+(\S+)",
        RegexOptions.Compiled);

    private static readonly Regex SimpleProgressRegex = new(
        @"\[download\]\s+(\d+\.?\d*)%",
        RegexOptions.Compiled);

    private static readonly Regex FileNameRegex = new(
        @"\[download\]\s+Downloading\s+(.+)",
        RegexOptions.Compiled);

    private static readonly Regex VerboseRegex = new(
        @"\[(\w+)\]\[debug\]\s+(.+)",
        RegexOptions.Compiled);

    private static readonly Regex PercentRegex = new(
        @"(\d+\.?\d*)%",
        RegexOptions.Compiled);

    public GalleryDlService(string downloadDirectory)
    {
        _galleryDlPath = System.IO.Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gallery-dl.exe");
        _configService = DownloadConfigService.Instance;

        // 确保下载目录存在
        if (!Directory.Exists(downloadDirectory))
        {
            Directory.CreateDirectory(downloadDirectory);
        }
    }

    /// <summary>
    /// 检查 gallery-dl 是否可用
    /// </summary>
    public bool IsAvailable()
    {
        return File.Exists(_galleryDlPath);
    }

    /// <summary>
    /// 获取 gallery-dl 版本
    /// </summary>
    public async Task<string?> GetVersionAsync()
    {
        if (!IsAvailable()) return null;

        try
        {
            var process = new Process
            {
                StartInfo = new ProcessStartInfo
                {
                    FileName = _galleryDlPath,
                    Arguments = "--version",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true
                }
            };

            process.Start();
            string output = await process.StandardOutput.ReadToEndAsync();
            await process.WaitForExitAsync();

            return output.Trim();
        }
        catch
        {
            return null;
        }
    }

    /// <summary>
    /// 开始下载任务
    /// </summary>
    public async Task DownloadAsync(
        string url,
        IProgress<DownloadProgress>? progress = null,
        CancellationToken cancellationToken = default)
    {
        if (!IsAvailable())
        {
            throw new InvalidOperationException("gallery-dl.exe 未找到");
        }

        // 获取当前配置的下载路径
        var downloadDirectory = _configService.DownloadPath;
        if (!Directory.Exists(downloadDirectory))
        {
            Directory.CreateDirectory(downloadDirectory);
        }

        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = _galleryDlPath,
                Arguments = $"--verbose -d \"{downloadDirectory}\" \"{url}\"",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            },
            EnableRaisingEvents = true
        };

        var progressReporter = new DownloadProgress { Url = url, Status = DownloadStatus.Downloading };

        process.OutputDataReceived += (sender, e) =>
        {
            if (string.IsNullOrEmpty(e.Data)) return;

            // 解析 gallery-dl 输出
            ParseOutput(e.Data, progressReporter);
            // 创建副本报告进度，避免引用问题
            var report = new DownloadProgress
            {
                Url = progressReporter.Url,
                FileName = progressReporter.FileName,
                ProgressPercentage = progressReporter.ProgressPercentage,
                TotalSize = progressReporter.TotalSize,
                Speed = progressReporter.Speed,
                ETA = progressReporter.ETA,
                Status = progressReporter.Status,
                ErrorMessage = progressReporter.ErrorMessage
            };
            progress?.Report(report);
        };

        process.ErrorDataReceived += (sender, e) =>
        {
            if (!string.IsNullOrEmpty(e.Data))
            {
                // 只记录错误输出，不立即设置错误状态
                // 因为 verbose 输出也可能通过 stderr
                progressReporter.ErrorMessage = e.Data;
                var report = new DownloadProgress
                {
                    Url = progressReporter.Url,
                    FileName = progressReporter.FileName,
                    ProgressPercentage = progressReporter.ProgressPercentage,
                    TotalSize = progressReporter.TotalSize,
                    Speed = progressReporter.Speed,
                    ETA = progressReporter.ETA,
                    Status = progressReporter.Status,
                    ErrorMessage = e.Data
                };
                progress?.Report(report);
            }
        };

        process.Start();
        process.BeginOutputReadLine();
        process.BeginErrorReadLine();

        // 等待进程完成或取消
        await Task.Run(() =>
        {
            process.WaitForExit();
        }, cancellationToken);

        if (process.ExitCode == 0)
        {
            progressReporter.Status = DownloadStatus.Completed;
        }
        else if (progressReporter.Status != DownloadStatus.Error)
        {
            progressReporter.Status = DownloadStatus.Error;
            progressReporter.ErrorMessage = $"Exit code: {process.ExitCode}";
        }

        progress?.Report(progressReporter);
    }

    /// <summary>
    /// 解析 gallery-dl 输出
    /// </summary>
    private void ParseOutput(string line, DownloadProgress progress)
    {
        // 调试输出
        System.Diagnostics.Debug.WriteLine($"[gallery-dl] {line}");

        // 匹配下载进度，例如: [download]  45.0% of 10.5MiB at 2.5MiB/s ETA 00:05
        var match = ProgressRegex.Match(line);
        if (match.Success)
        {
            progress.ProgressPercentage = double.Parse(match.Groups[1].Value);
            progress.TotalSize = match.Groups[2].Value;
            progress.Speed = match.Groups[3].Value;
            progress.ETA = match.Groups[4].Value;
            progress.Status = DownloadStatus.Downloading;
            return;
        }

        // 匹配简化的进度格式: [download] 45.0%
        var simpleMatch = SimpleProgressRegex.Match(line);
        if (simpleMatch.Success)
        {
            progress.ProgressPercentage = double.Parse(simpleMatch.Groups[1].Value);
            progress.Status = DownloadStatus.Downloading;
            return;
        }

        // 匹配文件名 - Destination 格式
        if (line.StartsWith("[download] Destination:"))
        {
            progress.FileName = line.Replace("[download] Destination:", "").Trim();
            return;
        }

        // 匹配文件名 - 正在下载的文件
        var fileMatch = FileNameRegex.Match(line);
        if (fileMatch.Success)
        {
            progress.FileName = fileMatch.Groups[1].Value.Trim();
            return;
        }

        // 匹配 gallery-dl 的 verbose 输出中的下载信息
        // 例如: [nhentai][debug] ... 或者 [gallery-dl][debug] ...
        var verboseMatch = VerboseRegex.Match(line);
        if (verboseMatch.Success)
        {
            var content = verboseMatch.Groups[2].Value;

            // 检查是否包含下载进度信息
            var percentMatch = PercentRegex.Match(content);
            if (percentMatch.Success)
            {
                progress.ProgressPercentage = double.Parse(percentMatch.Groups[1].Value);
                progress.Status = DownloadStatus.Downloading;
            }

            // 检查是否包含文件信息
            if (content.Contains("Downloading") || content.Contains("download"))
            {
                progress.FileName = ExtractFileNameFromUrl(progress.Url);
            }

            return;
        }

        // 匹配开始下载信息
        if (line.Contains("Downloading") && line.Contains("/"))
        {
            var parts = line.Split(new[] { "Downloading" }, StringSplitOptions.None);
            if (parts.Length > 1)
            {
                progress.FileName = parts[1].Trim();
                return;
            }
        }

        // 匹配完成信息
        if (line.Contains("100%") || line.Contains("[download] 100%"))
        {
            progress.ProgressPercentage = 100;
        }
    }

    /// <summary>
    /// 从 URL 提取文件名
    /// </summary>
    private static string ExtractFileNameFromUrl(string url)
    {
        try
        {
            var uri = new Uri(url);
            var segments = uri.Segments;
            if (segments.Length > 0)
            {
                var lastSegment = segments[^1].TrimEnd('/');
                if (!string.IsNullOrEmpty(lastSegment))
                {
                    return lastSegment;
                }
            }
            return uri.Host;
        }
        catch
        {
            return url;
        }
    }
}

/// <summary>
/// 下载进度信息
/// </summary>
public class DownloadProgress
{
    public string Url { get; set; } = string.Empty;
    public string? FileName { get; set; }
    public double ProgressPercentage { get; set; }
    public string? TotalSize { get; set; }
    public string? Speed { get; set; }
    public string? ETA { get; set; }
    public DownloadStatus Status { get; set; }
    public string? ErrorMessage { get; set; }
}

/// <summary>
/// 下载状态
/// </summary>
public enum DownloadStatus
{
    Pending,
    Downloading,
    Completed,
    Error,
    Cancelled
}
