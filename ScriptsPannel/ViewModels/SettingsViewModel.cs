using System.IO;
using System.Windows.Forms;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptsPannel.Models;
using ScriptsPannel.Services;
using WinMessageBox = System.Windows.MessageBox;

namespace ScriptsPannel.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly Action _onSaved;

    [ObservableProperty] private string _language = "ru";
    [ObservableProperty] private int _themeIndex;
    [ObservableProperty] private string _scriptsPath = "";
    [ObservableProperty] private string? _customBackgroundPath;

    public SettingsViewModel(ISettingsService settings, Action onSaved)
    {
        _settings = settings;
        _onSaved = onSaved;
        var s = settings.Load();
        Language = string.IsNullOrWhiteSpace(s.Language) ? "ru" : s.Language!;
        ThemeIndex = s.Theme switch
        {
            UiThemeKind.Dark => 1,
            UiThemeKind.CustomBackground => 2,
            _ => 0
        };
        ScriptsPath = string.IsNullOrWhiteSpace(s.ScriptsDataRoot)
            ? AppPaths.DefaultDataRoot
            : s.ScriptsDataRoot!;
        CustomBackgroundPath = s.CustomShelfBackgroundPath;
    }

    [RelayCommand]
    private void BrowseScriptsFolder()
    {
        using var d = new FolderBrowserDialog();
        d.InitialDirectory = Directory.Exists(ScriptsPath) ? ScriptsPath : AppPaths.ExecutableDirectory;
        if (d.ShowDialog() == DialogResult.OK)
            ScriptsPath = d.SelectedPath;
    }

    [RelayCommand]
    private void BrowseBackground()
    {
        var dlg = new Microsoft.Win32.OpenFileDialog { Filter = "Images|*.png;*.jpg;*.jpeg;*.bmp|All|*.*" };
        if (dlg.ShowDialog() == true)
            CustomBackgroundPath = dlg.FileName;
    }

    [RelayCommand]
    private void Save(System.Windows.Window w)
    {
        try
        {
            Directory.CreateDirectory(ScriptsPath.Trim());
        }
        catch (Exception ex)
        {
            WinMessageBox.Show(ex.Message,
                (string)System.Windows.Application.Current.FindResource("Str.Error")!,
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        var theme = ThemeIndex switch
        {
            1 => UiThemeKind.Dark,
            2 => UiThemeKind.CustomBackground,
            _ => UiThemeKind.Light
        };
        var settings = new AppSettings
        {
            Language = Language.Trim().ToLowerInvariant(),
            Theme = theme,
            ScriptsDataRoot = ScriptsPath.Trim(),
            CustomShelfBackgroundPath = string.IsNullOrWhiteSpace(CustomBackgroundPath)
                ? null
                : CustomBackgroundPath
        };
        _settings.Save(settings);
        _onSaved();
        w.DialogResult = true;
        w.Close();
    }

    [RelayCommand]
    private static void Cancel(System.Windows.Window w)
    {
        w.DialogResult = false;
        w.Close();
    }
}
