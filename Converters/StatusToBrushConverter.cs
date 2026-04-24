using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GalleryUI.Services;

namespace GalleryUI.Converters;

/// <summary>
/// 下载状态转换为颜色画刷
/// </summary>
public class StatusToBrushConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadStatus status)
        {
            return status switch
            {
                DownloadStatus.Pending => new SolidColorBrush(Colors.Gray),
                DownloadStatus.Downloading => new SolidColorBrush(Color.FromRgb(0, 120, 212)),
                DownloadStatus.Completed => new SolidColorBrush(Color.FromRgb(16, 124, 16)),
                DownloadStatus.Error => new SolidColorBrush(Color.FromRgb(196, 43, 28)),
                DownloadStatus.Cancelled => new SolidColorBrush(Colors.Gray),
                _ => new SolidColorBrush(Colors.Gray)
            };
        }
        return new SolidColorBrush(Colors.Gray);
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
