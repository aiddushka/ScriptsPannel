using System.IO;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using ScriptsPannel.Models;
using ScriptsPannel.ViewModels;

namespace ScriptsPannel;

public partial class MainWindow : Window
{
    public MainWindow()
    {
        InitializeComponent();
    }

    public void ApplyShelfBackdrop(AppSettings s)
    {
        if (s.Theme == UiThemeKind.CustomBackground &&
            !string.IsNullOrWhiteSpace(s.CustomShelfBackgroundPath) &&
            File.Exists(s.CustomShelfBackgroundPath))
        {
            var img = new BitmapImage();
            img.BeginInit();
            img.UriSource = new Uri(System.IO.Path.GetFullPath(s.CustomShelfBackgroundPath), UriKind.Absolute);
            img.CacheOption = BitmapCacheOption.OnLoad;
            img.EndInit();
            ShelfBackdrop.Background = new ImageBrush(img)
            {
                Stretch = Stretch.UniformToFill,
                Opacity = 0.42
            };
            SurfaceTint.Opacity = 0.86;
        }
        else
        {
            ShelfBackdrop.Background = System.Windows.Media.Brushes.Transparent;
            SurfaceTint.Opacity = 1.0;
        }
    }

    private void MenuPopup_OnClosed(object sender, EventArgs e)
    {
        if (DataContext is MainViewModel m)
            m.MenuOpen = false;
    }
}
