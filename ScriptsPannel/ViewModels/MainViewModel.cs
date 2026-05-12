using System.Collections.ObjectModel;
using System.IO;
using System.Linq;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptsPannel.Models;
using ScriptsPannel.Services;
using ScriptsPannel.Views;

namespace ScriptsPannel.ViewModels;

public partial class MainViewModel : ObservableObject
{
    private readonly ISettingsService _settings;
    private readonly IAutostartTaskService _autostart;
    private IScriptDataStore _store;

    public ObservableCollection<ShelfViewModel> Shelves { get; } = new();

    [ObservableProperty] private ShelfViewModel? _selectedShelf;
    [ObservableProperty] private bool _welcomeDismissed;
    [ObservableProperty] private string _welcomeShelfName = "";
    [ObservableProperty] private bool _menuOpen;

    public MainViewModel(ISettingsService settings, IAutostartTaskService autostart)
    {
        _settings = settings;
        _autostart = autostart;
        _store = new FileScriptDataStore(_settings.GetEffectiveDataRoot());
        Shelves.CollectionChanged += (_, _) => RefreshDerived();
        LoadShelves();
    }

    public bool HasShelves => Shelves.Count > 0;
    public bool ShowWelcome => !HasShelves && !WelcomeDismissed;
    public bool ShowEmptyHint => !HasShelves && WelcomeDismissed;

    private void RefreshDerived()
    {
        OnPropertyChanged(nameof(HasShelves));
        OnPropertyChanged(nameof(ShowWelcome));
        OnPropertyChanged(nameof(ShowEmptyHint));
    }

    partial void OnWelcomeDismissedChanged(bool value) => RefreshDerived();

    partial void OnSelectedShelfChanged(ShelfViewModel? value) => MenuOpen = false;

    [RelayCommand]
    private void DismissWelcome() => WelcomeDismissed = true;

    [RelayCommand(CanExecute = nameof(CanCreateWelcome))]
    private void CreateWelcomeShelf()
    {
        CreateShelf(WelcomeShelfName.Trim());
        WelcomeShelfName = "";
    }

    private bool CanCreateWelcome() => !string.IsNullOrWhiteSpace(WelcomeShelfName);

    partial void OnWelcomeShelfNameChanged(string value) => CreateWelcomeShelfCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void NewShelfFromMenu()
    {
        MenuOpen = false;
        var dlg = new PromptWindow((string)System.Windows.Application.Current.FindResource("Str.NewShelfTitle")!,
            (string)System.Windows.Application.Current.FindResource("Str.ShelfName")!)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dlg.ShowDialog() == true && !string.IsNullOrWhiteSpace(dlg.ResultText))
            CreateShelf(dlg.ResultText.Trim());
    }

    [RelayCommand]
    private void ImportShelfFromMenu()
    {
        MenuOpen = false;
        var dlg = new Microsoft.Win32.OpenFileDialog
        {
            Filter = (string)System.Windows.Application.Current.FindResource("Str.FileFilterZip")!,
            Title = (string)System.Windows.Application.Current.FindResource("Str.ImportShelf")!
        };
        if (dlg.ShowDialog() != true)
            return;

        if (!ShelfArchiveService.TryImportShelfFromZip(_store, _autostart, dlg.FileName, out var info, out var err))
        {
            var msg = err switch
            {
                "invalid_archive" => (string)System.Windows.Application.Current.FindResource("Str.ImportInvalidArchive")!,
                "no_scripts_in_archive" => (string)System.Windows.Application.Current.FindResource("Str.ImportNoScripts")!,
                _ => string.IsNullOrWhiteSpace(err)
                    ? (string)System.Windows.Application.Current.FindResource("Str.ImportFailed")!
                    : (string)System.Windows.Application.Current.FindResource("Str.ImportFailed")! + "\n" + err
            };
            System.Windows.MessageBox.Show(msg,
                (string)System.Windows.Application.Current.FindResource("Str.Error")!,
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
            return;
        }

        ReloadStoreFromDisk();
        if (info != null)
            SelectedShelf = Shelves.FirstOrDefault(s => s.Id == info.Id) ?? Shelves.FirstOrDefault();
    }

    [RelayCommand]
    private void ExportShelf(ShelfViewModel? shelf)
    {
        shelf ??= SelectedShelf;
        if (shelf == null)
            return;
        MenuOpen = false;
        var dlg = new Microsoft.Win32.SaveFileDialog
        {
            Filter = (string)System.Windows.Application.Current.FindResource("Str.FileFilterZip")!,
            FileName = SanitizeFileName(shelf.Name) + ".zip",
            Title = (string)System.Windows.Application.Current.FindResource("Str.ExportShelf")!
        };
        if (dlg.ShowDialog() != true)
            return;
        try
        {
            ShelfArchiveService.ExportShelfToZip(_store, shelf.Id, shelf.Name, dlg.FileName);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(
                (string)System.Windows.Application.Current.FindResource("Str.ExportFailed")! + "\n" + ex.Message,
                (string)System.Windows.Application.Current.FindResource("Str.Error")!,
                System.Windows.MessageBoxButton.OK, System.Windows.MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void RenameShelf(ShelfViewModel? shelf)
    {
        shelf ??= SelectedShelf;
        if (shelf == null)
            return;
        MenuOpen = false;
        var dlg = new PromptWindow(
            (string)System.Windows.Application.Current.FindResource("Str.RenameShelfTitle")!,
            (string)System.Windows.Application.Current.FindResource("Str.ShelfName")!,
            shelf.Name,
            okButtonIsSave: true)
        {
            Owner = System.Windows.Application.Current.MainWindow
        };
        if (dlg.ShowDialog() != true)
            return;
        var newName = dlg.ResultText.Trim();
        if (string.IsNullOrWhiteSpace(newName) || newName == shelf.Name)
            return;

        var idx = _store.LoadIndex();
        var si = idx.Shelves.FirstOrDefault(s => s.Id == shelf.Id);
        if (si == null)
            return;
        si.Name = newName;
        _store.SaveIndex(idx);
        shelf.Name = newName;
    }

    [RelayCommand]
    private void DeleteShelf(ShelfViewModel? shelf)
    {
        shelf ??= SelectedShelf;
        if (shelf == null)
            return;
        MenuOpen = false;
        var confirm = (string)System.Windows.Application.Current.FindResource("Str.ConfirmDeleteShelf")!;
        var r = System.Windows.MessageBox.Show(confirm + "\n\n" + shelf.Name,
            (string)System.Windows.Application.Current.FindResource("Str.AppTitle")!,
            System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Warning);
        if (r != System.Windows.MessageBoxResult.OK)
            return;

        foreach (var e in _store.LoadScripts(shelf.Id))
        {
            var tn = WindowsAutostartTaskService.FullTaskName(shelf.Id.ToString("N"), e.Id.ToString("N"));
            _autostart.TryUnregister(tn);
        }

        var dir = _store.ShelfDirectory(shelf.Id);
        if (Directory.Exists(dir))
            Directory.Delete(dir, recursive: true);

        var idx = _store.LoadIndex();
        idx.Shelves.RemoveAll(s => s.Id == shelf.Id);
        _store.SaveIndex(idx);
        Shelves.Remove(shelf);
        if (SelectedShelf == shelf)
            SelectedShelf = Shelves.FirstOrDefault();
    }

    [RelayCommand]
    private void RefreshApplication()
    {
        MenuOpen = false;
        ReloadStoreFromDisk();
        var s = _settings.Load();
        ThemeApplier.ApplyLanguage(s.Language);
        ThemeApplier.ApplyThemeResources(ThemeApplier.ToLightDark(s.Theme));
        if (System.Windows.Application.Current.MainWindow is MainWindow mw)
            mw.ApplyShelfBackdrop(s);
    }

    [RelayCommand]
    private void OpenSettings()
    {
        MenuOpen = false;
        var svm = new SettingsViewModel(_settings, () =>
        {
            ReloadStoreFromDisk();
            var s = _settings.Load();
            ThemeApplier.ApplyLanguage(s.Language);
            ThemeApplier.ApplyThemeResources(ThemeApplier.ToLightDark(s.Theme));
            if (System.Windows.Application.Current.MainWindow is MainWindow mw)
                mw.ApplyShelfBackdrop(s);
        });
        var w = new SettingsWindow(svm) { Owner = System.Windows.Application.Current.MainWindow };
        w.ShowDialog();
    }

    public void ReloadStoreFromDisk()
    {
        var sel = SelectedShelf?.Id;
        _store = new FileScriptDataStore(_settings.GetEffectiveDataRoot());
        Shelves.Clear();
        LoadShelves();
        SelectedShelf = Shelves.FirstOrDefault(s => s.Id == sel) ?? Shelves.FirstOrDefault();
    }

    private void LoadShelves()
    {
        var idx = _store.LoadIndex();
        foreach (var s in idx.Shelves.OrderBy(x => x.Name, StringComparer.CurrentCultureIgnoreCase))
            Shelves.Add(new ShelfViewModel(s.Id, s.Name, _store, _autostart));
        SelectedShelf ??= Shelves.FirstOrDefault();
    }

    private void CreateShelf(string name)
    {
        var info = new ShelfInfo { Id = Guid.NewGuid(), Name = name, CreatedUtc = DateTimeOffset.UtcNow };
        var idx = _store.LoadIndex();
        idx.Shelves.Add(info);
        _store.SaveIndex(idx);
        var vm = new ShelfViewModel(info.Id, info.Name, _store, _autostart);
        Shelves.Add(vm);
        SelectedShelf = vm;
        WelcomeDismissed = true;
    }

    private static string SanitizeFileName(string name)
    {
        var invalid = Path.GetInvalidFileNameChars();
        var chars = name.Select(c => invalid.Contains(c) ? '_' : c).ToArray();
        var s = new string(chars).Trim();
        return string.IsNullOrWhiteSpace(s) ? "shelf" : s;
    }
}
