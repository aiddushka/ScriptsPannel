using System.Windows;
using ScriptsPannel.Models;
using ScriptsPannel.Services;
using ScriptsPannel.ViewModels;

namespace ScriptsPannel;

public partial class App : System.Windows.Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);
        var settings = new SettingsService();
        var s = settings.Load();
        ThemeApplier.ApplyLanguage(s.Language);
        ThemeApplier.ApplyThemeResources(s.Theme);
        var main = new MainWindow
        {
            DataContext = new MainViewModel(settings, new WindowsAutostartTaskService())
        };
        MainWindow = main;
        main.ApplyShelfBackdrop(s);
        main.Show();
    }
}
