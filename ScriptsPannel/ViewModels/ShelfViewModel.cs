using System.Collections.ObjectModel;
using System.Windows;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using ScriptsPannel.Models;
using ScriptsPannel.Services;
using ScriptsPannel.Views;

namespace ScriptsPannel.ViewModels;

public partial class ShelfViewModel : ObservableObject
{
    private readonly IAutostartTaskService _autostart;

    public Guid Id { get; }

    [ObservableProperty] private string _name;

    public ObservableCollection<ScriptRowViewModel> Scripts { get; } = new();

    public ShelfViewModel(Guid id, string name, IScriptDataStore store, IAutostartTaskService autostart)
    {
        _autostart = autostart;
        Id = id;
        _name = name;
        Store = store;
        RefreshScripts();
    }

    public IScriptDataStore Store { get; }

    public void RefreshScripts()
    {
        Scripts.Clear();
        foreach (var e in Store.LoadScripts(Id))
            Scripts.Add(new ScriptRowViewModel(this, e, Store, _autostart));
    }

    [RelayCommand]
    private void AddScript()
    {
        var vm = new AddEditScriptViewModel(Store, _autostart, Id, null);
        var w = new AddEditScriptWindow(vm) { Owner = System.Windows.Application.Current.MainWindow };
        if (w.ShowDialog() == true)
            RefreshScripts();
    }
}
