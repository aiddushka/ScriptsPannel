using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptsPannel.Models;
using ScriptsPannel.Services;
using ScriptsPannel.Views;

using ScriptsPannel;

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
    private void OpenSettings()
    {
        MenuOpen = false;
        var svm = new SettingsViewModel(_settings, () =>
        {
            ReloadStoreFromDisk();
            var s = _settings.Load();
            ThemeApplier.ApplyLanguage(s.Language);
            ThemeApplier.ApplyThemeResources(s.Theme);
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
}
