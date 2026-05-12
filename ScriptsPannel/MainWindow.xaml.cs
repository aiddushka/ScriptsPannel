using System.IO;
using System.Windows;
using System.Windows.Input;
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
        if (s.UseShelfBackgroundImage &&
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
                Opacity = 0.38
            };
            SurfaceTint.Opacity = 0.90;
        }
        else
        {
            ShelfBackdrop.Background = System.Windows.Media.Brushes.Transparent;
            SurfaceTint.Opacity = 1.0;
        }
    }

    private void DrawerDismissOverlay_OnMouseDown(object sender, MouseButtonEventArgs e)
    {
        if (DataContext is MainViewModel vm && vm.MenuOpen)
            vm.MenuOpen = false;
        e.Handled = true;
    }
}
