using System.ComponentModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using GalleryUI.Services;
using Microsoft.Win32;
using Wpf.Ui.Appearance;
using Wpf.Ui.Controls;

namespace GalleryUI.Views.Pages;

/// <summary>
/// 设置页面
/// </summary>
public partial class SettingsPage
{
    private readonly GalleryDlService _galleryDlService;
    private readonly DownloadConfigService _configService;

    public SettingsPage()
    {
        _configService = DownloadConfigService.Instance;
        DataContext = _configService;
        
        // 初始化下载服务
        _galleryDlService = new GalleryDlService(_configService.DownloadPath);
        
        InitializeComponent();

        // 根据当前主题设置 ComboBox 选中项
        var currentTheme = ApplicationThemeManager.GetAppTheme();
        ThemeComboBox.SelectedIndex = currentTheme switch
        {
            ApplicationTheme.Light => 0,
            ApplicationTheme.Dark => 1,
            ApplicationTheme.HighContrast => 2,
            _ => 1 // 默认深色
        };

        // 检查 gallery-dl 环境（延迟执行，确保资源已加载）
        Loaded += (_, _) => CheckGalleryDlEnvironment();
    }

    private async void CheckGalleryDlEnvironment()
    {
        var galleryDlPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "gallery-dl.exe");
        GalleryDlPathText.Text = galleryDlPath;

        if (_galleryDlService.IsAvailable())
        {
            // 环境正常 - 浅绿色样式
            GalleryDlStatusBorder.Background = (System.Windows.Media.Brush)FindResource("SystemFillColorSuccessBackgroundBrush");
            GalleryDlStatusBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("SystemFillColorSuccessBrush");
            GalleryDlStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("SystemFillColorSuccessBrush");
            GalleryDlStatusTextBlock.Text = "环境完整";
            GalleryDlStatusBorder.Visibility = Visibility.Visible;
            
            GalleryDlStatusText.Text = "已安装";
            GalleryDlInfoBar.IsOpen = false;

            // 获取版本
            var version = await _galleryDlService.GetVersionAsync();
            GalleryDlVersionText.Text = version ?? "未知";
        }
        else
        {
            // 环境缺失 - 浅红色样式
            GalleryDlStatusBorder.Background = (System.Windows.Media.Brush)FindResource("SystemFillColorCriticalBackgroundBrush");
            GalleryDlStatusBorder.BorderBrush = (System.Windows.Media.Brush)FindResource("SystemFillColorCriticalBrush");
            GalleryDlStatusTextBlock.Foreground = (System.Windows.Media.Brush)FindResource("SystemFillColorCriticalBrush");
            GalleryDlStatusTextBlock.Text = "环境缺失";
            GalleryDlStatusBorder.Visibility = Visibility.Visible;
            
            GalleryDlStatusText.Text = "未找到";
            GalleryDlVersionText.Text = "-";
            GalleryDlInfoBar.IsOpen = true;
        }
    }

    private void OnThemeComboBoxSelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (ThemeComboBox.SelectedIndex == -1) return;

        var theme = ThemeComboBox.SelectedIndex switch
        {
            0 => ApplicationTheme.Light,
            1 => ApplicationTheme.Dark,
            2 => ApplicationTheme.HighContrast,
            _ => ApplicationTheme.Dark
        };

        ApplicationThemeManager.Apply(theme);
    }

    public string AppVersion => $"版本 {GetAssemblyVersion()}";

    private static string GetAssemblyVersion()
    {
        return System.Reflection.Assembly.GetExecutingAssembly().GetName().Version?.ToString()
            ?? string.Empty;
    }

    /// <summary>
    /// 浏览下载路径
    /// </summary>
    private void OnBrowseDownloadPathClick(object sender, RoutedEventArgs e)
    {
        var dialog = new OpenFolderDialog
        {
            Title = "选择下载目录",
            InitialDirectory = Directory.Exists(_configService.DownloadPath) 
                ? _configService.DownloadPath 
                : Environment.GetFolderPath(Environment.SpecialFolder.UserProfile)
        };

        if (dialog.ShowDialog() == true)
        {
            _configService.DownloadPath = dialog.FolderName;
        }
    }
}
