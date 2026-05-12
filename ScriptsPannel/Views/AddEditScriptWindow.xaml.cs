using System.Windows;
using ScriptsPannel.ViewModels;

namespace ScriptsPannel.Views;

public partial class AddEditScriptWindow : Window
{
    public AddEditScriptWindow(AddEditScriptViewModel vm)
    {
        InitializeComponent();
        DataContext = vm;
        Loaded += (_, _) =>
        {
            Title = vm.IsEdit
                ? (string)FindResource("Str.EditScriptTitle")!
                : (string)FindResource("Str.NewScriptTitle")!;
        };
    }
}
