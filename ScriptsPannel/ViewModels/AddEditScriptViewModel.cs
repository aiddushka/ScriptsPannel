using System.IO;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptsPannel.Models;
using ScriptsPannel.Services;

namespace ScriptsPannel.ViewModels;

public partial class AddEditScriptViewModel : ObservableObject
{
    private readonly IScriptDataStore _store;
    private readonly IAutostartTaskService _autostartTaskService;
    private readonly Guid _shelfId;
    private readonly Guid _scriptId;

    [ObservableProperty] private string _name = "";
    [ObservableProperty] private int _runtimeIndex;
    [ObservableProperty] private string? _customRunnerPath;
    [ObservableProperty] private string _body = "";
    [ObservableProperty] private bool _runAsAdministrator;
    [ObservableProperty] private bool _autostart;

    public bool IsEdit { get; }

    public AddEditScriptViewModel(IScriptDataStore store, IAutostartTaskService autostart, Guid shelfId,
        ScriptEntry? existing)
    {
        _store = store;
        _autostartTaskService = autostart;
        _shelfId = shelfId;
        _scriptId = existing?.Id ?? Guid.NewGuid();
        IsEdit = existing != null;
        if (existing != null)
        {
            Name = existing.Name;
            Body = existing.Body;
            RunAsAdministrator = existing.RunAsAdministrator;
            Autostart = existing.Autostart;
            CustomRunnerPath = existing.CustomRunnerPath;
            RuntimeIndex = existing.Runtime switch
            {
                ScriptRuntimeKind.PowerShell => 0,
                ScriptRuntimeKind.Cmd => 1,
                _ => 2
            };
        }
    }

    partial void OnNameChanged(string value) => SaveCommand.NotifyCanExecuteChanged();

    [RelayCommand]
    private void BrowseRunner()
    {
        var d = new Microsoft.Win32.OpenFileDialog { Filter = "Programs|*.exe|All|*.*" };
        if (d.ShowDialog() == true)
            CustomRunnerPath = d.FileName;
    }

    private bool CanSave() => !string.IsNullOrWhiteSpace(Name);

    [RelayCommand(CanExecute = nameof(CanSave))]
    private void Save(Window window)
    {
        var runtime = RuntimeIndex switch
        {
            1 => ScriptRuntimeKind.Cmd,
            2 => ScriptRuntimeKind.Custom,
            _ => ScriptRuntimeKind.PowerShell
        };
        if (runtime == ScriptRuntimeKind.Custom &&
            (string.IsNullOrWhiteSpace(CustomRunnerPath) || !System.IO.File.Exists(CustomRunnerPath)))
        {
            System.Windows.MessageBox.Show((string)System.Windows.Application.Current.FindResource("Str.Error")! + ": runner",
                (string)System.Windows.Application.Current.FindResource("Str.AppTitle")!, System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
            return;
        }

        var entry = new ScriptEntry
        {
            Id = _scriptId,
            Name = Name.Trim(),
            Runtime = runtime,
            CustomRunnerPath = string.IsNullOrWhiteSpace(CustomRunnerPath) ? null : CustomRunnerPath,
            Body = Body ?? "",
            RunAsAdministrator = RunAsAdministrator,
            Autostart = Autostart
        };

        _store.SaveScript(_shelfId, entry);

        var taskName = WindowsAutostartTaskService.FullTaskName(_shelfId.ToString("N"), _scriptId.ToString("N"));
        if (entry.Autostart)
        {
            var tr = ScriptCommandBuilder.BuildRunCommandLine(_store, _shelfId, entry);
            if (!_autostartTaskService.TryRegister(taskName, tr, entry.RunAsAdministrator))
            {
                System.Windows.MessageBox.Show((string)System.Windows.Application.Current.FindResource("Str.AutostartWarn")!,
                    (string)System.Windows.Application.Current.FindResource("Str.AppTitle")!, System.Windows.MessageBoxButton.OK,
                    System.Windows.MessageBoxImage.Warning);
            }
        }
        else
        {
            _autostartTaskService.TryUnregister(taskName);
        }

        window.DialogResult = true;
        window.Close();
    }

    [RelayCommand]
    private static void Cancel(Window window)
    {
        window.DialogResult = false;
        window.Close();
    }
}
