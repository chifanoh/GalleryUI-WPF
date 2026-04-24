using System.Globalization;
using System.Windows.Data;
using System.Windows.Media;
using GalleryUI.Services;

namespace GalleryUI.Converters;

/// <summary>
/// 下载状态转换为背景颜色画刷
/// </summary>
public class StatusToBackgroundConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is DownloadStatus status)
        {
            return status switch
            {
                DownloadStatus.Pending => new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                DownloadStatus.Downloading => new SolidColorBrush(Color.FromRgb(230, 243, 255)),
                DownloadStatus.Completed => new SolidColorBrush(Color.FromRgb(230, 255, 230)),
                DownloadStatus.Error => new SolidColorBrush(Color.FromRgb(255, 235, 235)),
                DownloadStatus.Cancelled => new SolidColorBrush(Color.FromRgb(240, 240, 240)),
                _ => new SolidColorBrush(Color.FromRgb(240, 240, 240))
            };
        }
        return new SolidColorBrush(Color.FromRgb(240, 240, 240));
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}
