using System.Diagnostics;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptsPannel.Models;
using ScriptsPannel.Services;
using ScriptsPannel.Views;

namespace ScriptsPannel.ViewModels;

public partial class ScriptRowViewModel : ObservableObject
{
    private readonly ShelfViewModel _shelf;
    private readonly IAutostartTaskService _autostart;

    public ScriptEntry Model { get; }

    public ScriptRowViewModel(ShelfViewModel shelf, ScriptEntry model, IScriptDataStore store,
        IAutostartTaskService autostart)
    {
        _shelf = shelf;
        Model = model;
        Store = store;
        _autostart = autostart;
    }

    public IScriptDataStore Store { get; }

    public string Name => Model.Name;
    public string RuntimeLabel => Model.Runtime switch
    {
        ScriptRuntimeKind.PowerShell => "PowerShell",
        ScriptRuntimeKind.Cmd => "CMD",
        _ => "Custom"
    };

    public bool Autostart => Model.Autostart;

    [RelayCommand]
    private void Run()
    {
        try
        {
            var psi = ScriptCommandBuilder.BuildStartInfo(Store, _shelf.Id, Model);
            Process.Start(psi);
        }
        catch (Exception ex)
        {
            System.Windows.MessageBox.Show(ex.Message, (string)System.Windows.Application.Current.FindResource("Str.Error")!,
                System.Windows.MessageBoxButton.OK,
                System.Windows.MessageBoxImage.Warning);
        }
    }

    [RelayCommand]
    private void Edit()
    {
        var vm = new AddEditScriptViewModel(Store, _autostart, _shelf.Id, Model);
        var w = new AddEditScriptWindow(vm) { Owner = System.Windows.Application.Current.MainWindow };
        if (w.ShowDialog() == true)
            _shelf.RefreshScripts();
    }

    [RelayCommand]
    private void Delete()
    {
        var msg = (string)System.Windows.Application.Current.FindResource("Str.ConfirmDelete")!;
        var r = System.Windows.MessageBox.Show(msg + "\n\n" + Model.Name,
            (string)System.Windows.Application.Current.FindResource("Str.AppTitle")!,
            System.Windows.MessageBoxButton.OKCancel, System.Windows.MessageBoxImage.Question);
        if (r != System.Windows.MessageBoxResult.OK)
            return;
        if (Model.Autostart)
        {
            var tn = WindowsAutostartTaskService.FullTaskName(_shelf.Id.ToString("N"), Model.Id.ToString("N"));
            _autostart.TryUnregister(tn);
        }
        Store.DeleteScript(_shelf.Id, Model.Id);
        _shelf.RefreshScripts();
    }
}
