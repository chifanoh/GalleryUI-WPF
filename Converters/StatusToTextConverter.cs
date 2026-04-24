using System.Globalization;
using System.Windows.Data;
using GalleryUI.Services;

namespace GalleryUI.Converters;

/// <summary>
/// 将下载状态转换为简洁文本
/// </summary>
public class StatusToTextConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadStatus status)
        {
            return status switch
            {
                DownloadStatus.Pending => "等待中",
                DownloadStatus.Downloading => "下载中",
                DownloadStatus.Completed => "已完成",
                DownloadStatus.Error => "下载出错",
                DownloadStatus.Cancelled => "已取消",
                _ => "未知"
            };
        }
        return "未知";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
